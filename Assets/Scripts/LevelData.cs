using System;
using UnityEngine;

namespace Mario
{
    [CreateAssetMenu(menuName = "Mario/Level")]
    public sealed class LevelData : ScriptableObject
    {
        public string id;
        public bool Japanese => id.StartsWith("J-");
        public string Course => Japanese?id.Substring(2):id;
        public int World => int.Parse(Course.Substring(0,1));
        public int Stage => int.Parse(Course.Substring(2,1));
        public int timeLimit = 400;
        public LocationData[] locations;
        public AreaData[] areas;
    }
    [Serializable] public sealed class LevelDocument
    {
        public string id; public int timeLimit;
        public LocationData[] locations; public AreaData[] areas;
    }
    [Serializable] public sealed class LocationData { public int area; public string entry; public float x,y; public bool positioned; }
    [Serializable] public sealed class AreaData
    {
        public string setting; public int exit=-1; public bool underwater; public float width;
        public NodeData[] nodes; public SectionData[] sections;

        // The reference adds water as area-wide scenery outside its creation
        // list. Native Japanese tiles and explicit water nodes supply their own art.
        public bool NeedsWaterBackdrop => underwater&&!Array.Exists(nodes,n=>n.type=="Water"||
            (n.spriteOverride!=null&&n.spriteOverride.StartsWith("JapanTiles/")));

        public bool HasFlagPoleArtwork(NodeData pole)
        {
            if(pole.type!="FlagPole"||!string.IsNullOrEmpty(pole.spriteOverride))return false;
            // SMB2J's 0x21 metatile already includes the finial and the start
            // of the shaft. The separate FlagPole node is still the goal trigger.
            foreach(var tile in nodes)
                if(tile.type=="JTile"&&tile.spriteOverride!=null&&
                    tile.spriteOverride.StartsWith("JapanTiles/")&&tile.spriteOverride.EndsWith("/21")&&
                    tile.y==pole.y&&tile.x<=pole.x&&tile.x+tile.width>=pole.x+pole.width)
                    return true;
            return false;
        }
    }
    [Serializable] public sealed class SectionData { public SectionPart[] parts; }
    [Serializable] public sealed class SectionPart { public string kind; public float width; public NodeData[] nodes; }
    [Serializable] public sealed class NodeData
    {
        public string type, group;
        public string spriteOverride;
        [Tooltip("Original pixels. X is left edge; Y is top edge above the ground.")]
        public float x, y, width = 16, height = 16;
        public bool hidden, jumping, floating, sliding, falling, smart, hard, throwing, inScale, onPipe, red, small, flying, ride;
        public float begin, end, speed = .75f, vx, vy;
        public int fireballs, section, pass, fail, entrance = -1, targetLocation = -1;
        public string movement, action, link, linkPart, contents, targetMap, text;
        public NodeData Copy() => (NodeData)MemberwiseClone();
    }
}
