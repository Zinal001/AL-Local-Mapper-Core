using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AL_Local_Mapper_Core.Classes
{
    public class LocalPath
    {
        private static readonly String[] RequiredFields = new String[] {
            "MapName",
            "Pos1",
            "Pos2",
            "Path"
        };

        public String MapName { get; set; }
        public String Description { get; set; }
        public bool BiDirectional { get; set; } = false;
        public LocalPathPoint Pos1 { get; set; }
        public LocalPathPoint Pos2 { get; set; }
        public bool OptimizePath { get; set; } = false;
        public List<PathPart> Path { get; set; }

        public static LocalPath ReadFromJObject(JObject pathObj)
        {
            foreach (String field in RequiredFields)
            {
                if (!pathObj.ContainsKey(field))
                    throw new Exception($"Path doesn't specify a required field of {field}.");
            }

            LocalPath path = new LocalPath()
            {
                MapName = pathObj.Value<String>("MapName"),
                Pos1 = pathObj.Value<JObject>("Pos1").ToObject<LocalPathPoint>(),
                Pos2 = pathObj.Value<JObject>("Pos2").ToObject<LocalPathPoint>(),
                Path = new List<PathPart>()
            };

            if (pathObj.ContainsKey("Description"))
                path.Description = pathObj.Value<String>("Description");

            if (pathObj.ContainsKey("BiDirectional"))
                path.BiDirectional = pathObj.Value<bool>("BiDirectional");

            if (pathObj.ContainsKey("OptimizePath"))
                path.OptimizePath = pathObj.Value<bool>("OptimizePath");

            foreach (JObject partObj in pathObj.Value<JArray>("Path"))
            {
                PathPart part = PathPart.ReadFromJObject(partObj);
                if (part != null)
                    path.Path.Add(part);
            }

            return path;
        }
    }

    public class LocalPathPoint
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        [JsonIgnore()]
        public System.Drawing.Rectangle Rect
        {
            get
            {
                int x1 = Math.Min(X1, X2);
                int y1 = Math.Min(Y1, Y2);
                int x2 = Math.Max(X1, X2);
                int y2 = Math.Max(Y1, Y2);

                return new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1);
            }
        }
    }

    public abstract class PathPart
    {
        public abstract String Type { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public String MapName { get; set; }

        public static PathPart ReadFromJObject(JObject partObj)
        {
            if (!partObj.ContainsKey("Type"))
                return null;

            switch (partObj.Value<String>("Type"))
            {
                case "Move":
                    return partObj.ToObject<MovePathPart>();
                case "Teleport":
                    return partObj.ToObject<TeleportPathPart>();
                case "Town":
                    return partObj.ToObject<TownPathPart>();
                default:
                    throw new Exception($"A Path Part of type {partObj.Value<String>("Type")} is not supported.");
            }
        }
    }

    public class MovePathPart : PathPart
    {
        public override string Type => "Move";
    }

    public class TeleportPathPart : PathPart
    {
        public override string Type => "Teleport";
        public String ToMapName { get; set; }
        public int SpawnIndex { get; set; }
    }

    public class TownPathPart : PathPart
    {
        public override string Type => "Town";
    }
}
