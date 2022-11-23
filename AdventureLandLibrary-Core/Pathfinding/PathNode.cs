using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureLandLibrary.Pathfinding
{


    public abstract class PathNode
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("action")]
        public abstract string Action { get; }

        [JsonIgnore()]
        public String MapName { get; set; }

        public PathNode(int x, int y, string mapName)
        {
            this.X = x;
            this.Y = y;
            this.MapName = mapName;
        }

        public PathNode(Geometry.Point point, string mapName)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.MapName = mapName;
        }

        public PathNode(Geometry.PointStruct point, string mapName)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.MapName = mapName;
        }

        public virtual bool IsSame(PathNode other)
        {
            if (!other.GetType().Equals(GetType()))
                return false;

            return X == other.X && Y == other.Y && MapName == other.MapName;
        }
    }

    public class TownPathNode : PathNode
    {
        public override string Action => "Town";

        public TownPathNode(int x, int y, string mapName) : base(x, y, mapName) { }
    }

    public class MovePathNode : PathNode
    {
        public override string Action => "Move";

        public MovePathNode(int x, int y, string mapName) : base(x, y, mapName) { }

        public MovePathNode(Geometry.Point point, string mapName) : base(point, mapName) { }
    }

    public class TeleportPathNode : PathNode
    {
        public override string Action => "Teleport";

        [JsonProperty("action_target")]
        public String ActionTarget { get; set; }

        [JsonProperty("target_spawn")]
        public int TargetSpawn { get; set; }

        public TeleportPathNode(int x, int y, string mapName) : base(x, y, mapName) { }
        public TeleportPathNode(Geometry.PointStruct point, string mapName) : base(point, mapName) { }

        public override bool IsSame(PathNode other)
        {
            if (!base.IsSame(other))
                return false;

            TeleportPathNode tOther = other as TeleportPathNode;
            return ActionTarget == tOther.ActionTarget && TargetSpawn == tOther.TargetSpawn;
        }
    }
}
