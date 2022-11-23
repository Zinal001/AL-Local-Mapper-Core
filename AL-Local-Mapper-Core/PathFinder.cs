using AdventureLandLibrary.GameObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AL_Local_Mapper_Core
{
    public class PathFinder
    {

        private List<PathCache> _CachedPaths = new List<PathCache>();

        public void Initialize()
        {
            foreach (String mapName in Glob.Settings.ExcludedMaps)
                Maps.ExcludeMaps.Add(mapName);

            Maps.LoadLite();
        }

        public JObject FindPath(JObject arguments)
        {
            GetPathArgs args = arguments.ToObject<GetPathArgs>();

            if (Glob.Settings.CachePaths && !args.NoCache)
            {
                lock(_CachedPaths)
                {
                    var cache = _CachedPaths.FirstOrDefault(cp => cp.Key == args.ToString());
                    if (cache != null)
                    {
                        if (DateTime.Now.Subtract(cache.Created).TotalMinutes >= Glob.Settings.CacheTime)
                            _CachedPaths.Remove(cache);
                        else
                        {
                            return new JObject() {
                                { "path", JArray.FromObject(cache.Path) },
                                { "time", 0 },
                                { "cached", true }
                            };
                        }
                    }
                }
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            List<AdventureLandLibrary.Pathfinding.PathNode> fullPath = new List<AdventureLandLibrary.Pathfinding.PathNode>();

            int fromX = args.FromX;
            int fromY = args.FromY;
            String fromMap = args.FromMap;

            int tries = 0;

            do
            {
                tries++;

                List<AdventureLandLibrary.Pathfinding.PathNode> path = GetMapPath(fromX, fromY, fromMap, args.ToX, args.ToY, args.ToMap, out String err);
                if (!String.IsNullOrEmpty(err))
                {
                    sw.Stop();
                    return new JObject() {
                        { "path", new JArray() },
                        { "time", sw.ElapsedMilliseconds },
                        { "error", err }
                    };
                }

                if (args.CheckIfTownIsFaster || Glob.Settings.CheckTown)
                {
                    double pathTime = GetPathLength(path) / args.RunSpeed;
                    if (args.ConsoleLog)
                        Console.WriteLine($"Path from (\"{fromMap}\", {fromX}, {fromY}) to (\"{path.Last().MapName}\", {path.Last().X}, {path.Last().Y}) takes {pathTime}.");

                    dynamic mapTownLocation = AdventureLandLibrary.Global.Loader.data.maps[fromMap].spawns[0];
                    int mtlX = mapTownLocation[0].ToObject<int>();
                    int mtlY = mapTownLocation[1].ToObject<int>();

                    var pathFromSpawn = GetMapPath(mtlX, mtlY, fromMap, args.ToX, args.ToY, args.ToMap, out String townErr);
                    if (String.IsNullOrEmpty(townErr))
                    {
                        double pathFromSpawnTime = (GetPathLength(pathFromSpawn) / args.RunSpeed) + (10d);

                        if (args.ConsoleLog)
                            Console.WriteLine($"Using town from (\"{fromMap}\", {fromX}, {fromY}) to (\"{path.Last().MapName}\", {path.Last().X}, {path.Last().Y}) takes {pathFromSpawnTime}.");

                        if (pathFromSpawnTime >= 0 && pathFromSpawnTime < pathTime) 
                        {
                            if (args.ConsoleLog)
                                Console.WriteLine("Using town is faster for this route!");

                            path = pathFromSpawn;
                            path.Insert(0, new AdventureLandLibrary.Pathfinding.TownPathNode(mtlX, mtlY, fromMap));
                        }
                    }
                }

                if (args.OptimizePath || Glob.Settings.OptimizePath)
                    path = RemoveDuplicates(path);

                if (path.Any())
                {
                    fullPath.AddRange(path);

                    if (path.Last() is AdventureLandLibrary.Pathfinding.TeleportPathNode tpNode)
                    {
                        dynamic spawn = AdventureLandLibrary.Global.Loader.data.maps[tpNode.MapName].spawns[tpNode.TargetSpawn];
                        fromX = spawn[0].ToObject<int>();
                        fromY = spawn[1].ToObject<int>();
                        fromMap = tpNode.ActionTarget;
                    }
                    else
                        break;
                }
            } while (tries < 20);

            sw.Stop();

            if (Glob.Settings.CachePaths)
            {
                lock(_CachedPaths)
                {
                    _CachedPaths.Add(new PathCache()
                    {
                        Created = DateTime.Now,
                        Key = args.ToString(),
                        Path = fullPath
                    });
                }
            }

            return new JObject() {
                { "path", JArray.FromObject(fullPath) },
                { "time", sw.ElapsedMilliseconds }
            };
        }

        private static List<AdventureLandLibrary.Pathfinding.PathNode> RemoveDuplicates(List<AdventureLandLibrary.Pathfinding.PathNode> fullPath)
        {
            List<AdventureLandLibrary.Pathfinding.PathNode> newPath = new List<AdventureLandLibrary.Pathfinding.PathNode>();

            AdventureLandLibrary.Pathfinding.PathNode lastNode = null;
            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                var curNode = fullPath[i];

                if (lastNode == null || !curNode.IsSame(lastNode))
                    newPath.Add(curNode);

                lastNode = curNode;
            }

            if (fullPath.Any() && !fullPath.Last().IsSame(newPath.Last()))
                newPath.Add(fullPath.Last());

            return newPath;
        }

        private static int GetPathLength(List<AdventureLandLibrary.Pathfinding.PathNode> path)
        {
            int length = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (path[i].Action != "Move")
                    continue;

                int j = i + 1;
                var nextNode = path[i + 1];
                while (nextNode.Action != "Move")
                {
                    if (j < path.Count)
                        return length;
                    nextNode = path[++j];
                }

                length += (int)Distance(path[i].X, path[i].Y, path[j].X, path[j].Y);
            }

            return length;
        }

        private static double Distance(int x1, int y1, int x2, int y2)
        {
            int dX = x2 - x1;
            int dY = y2 - y1;

            return Math.Sqrt(Math.Pow(dX, 2) + Math.Pow(dY, 2)); //dX * dX + dY * dY;
        }

        private static List<AdventureLandLibrary.Pathfinding.PathNode> GetMapPath(int startX, int startY, String startMapName, int stopX, int stopY, String stopMapName, out String error)
        {
            error = null;

            System.Drawing.Point startPoint = new System.Drawing.Point(startX, startY);
            System.Drawing.Point endPoint = new System.Drawing.Point(stopX, stopY);

            var path = Maps.FindPath(new AdventureLandLibrary.Geometry.Point(startPoint.X, startPoint.Y), new AdventureLandLibrary.Geometry.Point(stopX, stopY), startMapName, stopMapName, false);

            if (path.Any())
            {
                if (path.Last() is AdventureLandLibrary.Pathfinding.TeleportPathNode tpNode && path.Length > 1)
                    endPoint = new System.Drawing.Point(path[path.Length - 2].X, path[path.Length - 2].Y);
                else
                    endPoint = new System.Drawing.Point(path.Last().X, path.Last().Y);
            }

            if (Glob.Settings.LocalPaths != null && Glob.Settings.LocalPaths.ContainsKey(startMapName))
            {
                Classes.LocalPath oPath = Glob.Settings.LocalPaths[startMapName].FirstOrDefault(o => (o.Pos1.Rect.Contains(startPoint) && o.Pos2.Rect.Contains(endPoint)) || (o.BiDirectional && o.Pos2.Rect.Contains(startPoint) && o.Pos1.Rect.Contains(endPoint)));

                if (oPath != null)
                {
                    List<AdventureLandLibrary.Pathfinding.PathNode> nodes = new List<AdventureLandLibrary.Pathfinding.PathNode>();
                    path = Maps.FindPath(new AdventureLandLibrary.Geometry.Point(startPoint.X, startPoint.Y), new AdventureLandLibrary.Geometry.Point(oPath.Path.First().X, oPath.Path.First().Y), startMapName, oPath.Path.First().MapName, false);
                    nodes.AddRange(path);

                    foreach (var step in oPath.Path)
                    {
                        if (step is Classes.TeleportPathPart tpStep)
                            nodes.Add(new AdventureLandLibrary.Pathfinding.TeleportPathNode(tpStep.X, tpStep.Y, tpStep.ToMapName) { ActionTarget = tpStep.ToMapName, TargetSpawn = tpStep.SpawnIndex });
                        else if (step is Classes.TownPathPart townStep)
                            nodes.Add(new AdventureLandLibrary.Pathfinding.TownPathNode(townStep.X, townStep.Y, townStep.MapName));
                        else
                            nodes.Add(new AdventureLandLibrary.Pathfinding.MovePathNode(step.X, step.Y, step.MapName));
                    }

                    path = Maps.FindPath(new AdventureLandLibrary.Geometry.Point(nodes.Last().X, nodes.Last().Y), new AdventureLandLibrary.Geometry.Point(stopX, stopY), nodes.Last().MapName, stopMapName, false);
                    nodes.AddRange(path);

                    return nodes;
                }
            }

            return path.ToList();
        }

        private class GetPathArgs
        {
            [JsonProperty("fromMap")]
            [JsonRequired()]
            public String FromMap { get; set; }

            [JsonProperty("fromX")]
            public int FromX { get; set; }

            [JsonProperty("fromY")]
            public int FromY { get; set; }

            [JsonProperty("toMap")]
            [JsonRequired()]
            public String ToMap { get; set; }

            [JsonProperty("toX")]
            public int ToX { get; set; }

            [JsonProperty("toY")]
            public int ToY { get; set; }

            [JsonProperty("useTown")]
            public bool CheckIfTownIsFaster { get; set; } = true;

            [JsonProperty("optimisePath")]
            public bool OptimizePath { get; set; } = true;

            [JsonProperty("runspd")]
            public double RunSpeed { get; set; } = 50;

            [JsonProperty("nocache")]
            public bool NoCache { get; set; } = false;

            [JsonProperty("consoleLog")]
            public bool ConsoleLog { get; set; } = false;

            public override string ToString()
            {
                return $"{FromMap}:{FromX}.{FromY}-{ToMap}:{ToX}.{ToY}-{CheckIfTownIsFaster}-{OptimizePath}";
            }
        }

        private class PathCache
        {
            public String Key { get; set; }
            public DateTime Created { get; set; }
            public List<AdventureLandLibrary.Pathfinding.PathNode> Path { get; set; }
        }
    }
}
