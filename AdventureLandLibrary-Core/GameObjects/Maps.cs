using AdventureLandLibrary.Global;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdventureLandLibrary.Geometry;
using AdventureLandLibrary.Pathfinding;

namespace AdventureLandLibrary.GameObjects
{
    public static class Maps
    {
        public static Dictionary<string, Map> MapDictionary;
        public static WorldGraph world;

        public static List<String> ExcludeMaps { get; set; } = new List<string>() { "original_main" };

        public static void Load()
        {
            MapDictionary = LoadMaps();

            world = new WorldGraph();
        }

        public static void LoadLite()
        {
            MapDictionary = LoadMapsLite();

            world = new WorldGraph();
        }

        public static String SaveToJson()
        {
            return new JObject() {
                { "mapDictionary", JObject.FromObject(MapDictionary) },
                { "world", JObject.FromObject(world) }
            }.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        public static void TryUpdateData()
        {
            var newData = Loader.GetLiveData();

            if(newData != null && newData.version != Loader.data.version)
            {
                Console.WriteLine("New Version Detected, Loading!");
                Loader.UpdateData();
                Load();
            }

            
        }

        public static Dictionary<String, Map> LoadMapsLite()
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            timer.Start();
            var tempMapDictionary = new Dictionary<string, Map>();

            var maps = ((JObject)Loader.data.geometry).Properties().Select(p => p.Name).ToList();

            foreach (var exclude in ExcludeMaps)
            {
                maps.Remove(exclude);
            }

            foreach(String mapName in maps)
            {
                try
                {
                    var mapObj = new Map(mapName);
                    tempMapDictionary.Add(mapName, mapObj);
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to initialize: {0}", mapName);
                }
            }

            timer.Stop();

            return tempMapDictionary;
        }

        public static Dictionary<string, Map> LoadMaps()
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            timer.Start();
            var tempMapDictionary = new Dictionary<string, Map>();

            //var test = new Map("level2");

            var maps = ((JObject)Loader.data.geometry).Properties().Select(p => p.Name).ToList();

            foreach (var exclude in ExcludeMaps)
            {
                maps.Remove(exclude);
            }

            Parallel.ForEach(maps, mapName =>
            {
                var map = (JObject)Loader.data.maps[mapName];

                try
                {
                    var mapObj = new Map(mapName);
                    tempMapDictionary.Add(mapName, mapObj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to initialize: {0}", mapName);
                }
            });
            timer.Stop();

            return tempMapDictionary;
        }

        public static PathNode[] FindPath(Point Start, Point End, string StartMap, string EndMap, bool FullPath)
        {
            List<PathNode> path = new List<PathNode>();

            if (MapDictionary.ContainsKey(StartMap) && MapDictionary.ContainsKey(EndMap))
            {
                var curStart = Start;
                var curEnd = End;

                if (StartMap != EndMap)
                {
                    var mapPath = world.GetPath(StartMap, EndMap);

                    //The path might be empty for a broken path, return an empty path in that case. --Zinal
                    if (!mapPath.Any())
                        return new PathNode[0];

                    var maxIndex = 1;

                    if(FullPath)
                    {
                        maxIndex = mapPath.Count;
                    }

                    for (var i = 0; i < maxIndex; i++)
                    {
                        var mapConnection = mapPath[i];

                        if(i > 0)
                        {
                            var prevConnection = mapPath[i-1];

                            if(mapConnection.ConnectedMap == prevConnection.MapName)
                            {
                                break;
                            }

                            curStart = new Point(prevConnection.SpawnPoint);

                        }

                        var map = MapDictionary[mapConnection.MapName];

                        curEnd = new Point(mapConnection.SpawnPoint);

                        var curPath = map.FindPath(curStart, curEnd);

                        var smoothedPath = map.SmoothPath(curPath);

                        for(var x = 0; x < curPath.Length; x++)
                        {
                            var point = curPath[x];

                            var pathNode = new MovePathNode(point, map.MapID);

                            path.Add(pathNode);
                        }

                        //Add transport point
                        var transport = new TeleportPathNode(mapConnection.ConnectedSpawnPoint, mapConnection.ConnectedMap)
                        {
                            ActionTarget = mapConnection.ConnectedMap,
                            TargetSpawn = mapConnection.ConnectedSpawnID
                        };

                        path.Add(transport);
                    }

                    if (FullPath)
                    {
                        curEnd = End;
                        curStart = new Point(mapPath.Last().SpawnPoint);

                        var endMap = MapDictionary[EndMap];

                        var curPath = endMap.FindPath(curStart, curEnd);

                        var smoothedPath = endMap.SmoothPath(curPath);

                        for (var x = 0; x < smoothedPath.Length; x++)
                        {
                            var point = smoothedPath[x];

                            var pathNode = new MovePathNode(point, endMap.MapID);

                            path.Add(pathNode);
                        }
                    }
                }
                else
                {
                    var map = MapDictionary[StartMap];

                    var rawPath = map.FindPath(Start, End);

                    var smoothedPath = map.SmoothPath(rawPath);

                    for(var i = 0; i < smoothedPath.Length; i++)
                    {
                        var point = smoothedPath[i];

                        var pathNode = new MovePathNode(point, map.MapID);

                        path.Add(pathNode);
                    }
                }
            }

            return path.ToArray();
        }

        //public static Point[] FindPath(int startX, int startY, string startMap, int endX, int endY, string endMap)
        //{

        //}
    }
}
