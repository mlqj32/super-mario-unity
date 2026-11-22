using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        void ConnectScaleRopes()
        {
            foreach(var platform in actors.Where(a=>a.data.type=="Platform"&&a.data.inScale).ToArray())
            {
                string part=platform.data.linkPart=="platformLeft"?"stringLeft":
                    platform.data.linkPart=="platformRight"?"stringRight":null;
                if(part!=null&&!string.IsNullOrEmpty(platform.data.link))
                    platform.scaleRope=actors.FirstOrDefault(a=>a.data.type=="String"&&
                        a.data.link==platform.data.link&&a.data.linkPart==part);
                if(platform.scaleRope!=null)continue;

                // Japanese ropes are baked into scenery tiles. Retain the
                // pulley cap and replace only the vertical column with one
                // repeating strip, so it can grow below its original footprint.
                var cap=actors.FirstOrDefault(a=>a.data.type=="JTile"&&a.data.spriteOverride!=null&&
                    a.data.spriteOverride.StartsWith("JapanTiles/")&&
                    (a.data.spriteOverride.EndsWith("/42")||a.data.spriteOverride.EndsWith("/43"))&&
                    Mathf.Abs(a.x+8-(platform.x+platform.w/2))<.01f&&a.y>=platform.Top);
                if(cap==null)continue;
                foreach(var tile in actors)
                    if(tile.data.type=="JTile"&&tile.x==cap.x&&tile.Top<=cap.y&&tile.y>=platform.Top&&
                        tile.data.spriteOverride!=null&&tile.data.spriteOverride.EndsWith("/40"))
                        tile.data.hidden=true;
                string file=cap.data.spriteOverride.Substring(0,cap.data.spriteOverride.LastIndexOf('/')+1)+"40";
                platform.scaleRope=Spawn(new NodeData{type="ScaleRope",group="Scenery",spriteOverride=file,
                    x=cap.x,y=cap.y,width=16,height=Mathf.Max(0,cap.y-platform.Top)});
            }
        }

        void UpdateScaleRope(Actor platform)
        {
            var rope=platform.scaleRope;if(rope==null)return;
            // Match the platform's painted pixel row. Independently rounding a
            // fractional length would leave a gap or move the fixed upper end.
            rope.y=Mathf.Round(platform.y)+platform.h;
            rope.h=Mathf.Max(0,rope.data.y-rope.y);
        }
    }
}
