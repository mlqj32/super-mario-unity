using System;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunScaleRopeAudit(Action<string,bool,string> record)
        {
            void Check(string name,bool pass,string detail="")=>record("Scale ropes: "+name,pass,detail);
            int pairs=0;
            foreach(var course in Levels)
            for(int ai=0;ai<course.areas.Length;ai++)
            {
                int loc=Array.FindIndex(course.locations,l=>l.area==ai);if(loc<0)continue;
                var links=course.areas[ai].nodes.Where(n=>n.type=="Platform"&&n.inScale)
                    .Select(n=>n.link).Distinct().ToArray();
                foreach(string link in links)
                foreach(bool rightSide in new[]{false,true})
                {
                    NewGame(course.id);LoadLocation(loc);invulnerable=100000;
                    var pair=actors.Where(a=>a.data.type=="Platform"&&a.data.inScale&&a.data.link==link).OrderBy(a=>a.x).ToArray();
                    string label=course.id+" area="+ai+" "+link+" rider="+(rightSide?"right":"left");
                    Check(label+" two linked platforms",pair.Length==2);if(pair.Length!=2)continue;
                    if(!rightSide)pairs++;
                    Check(label+" both ropes connected",pair.All(p=>p.scaleRope!=null));
                    if(pair.Any(p=>p.scaleRope==null))continue;
                    foreach(var a in actors)if(a.IsEnemy)a.alive=false;
                    cameraX=pair[0].x-48;UpdateCamera();
                    var loaded=pair[rightSide?1:0];var raised=pair[rightSide?0:1];
                    foreach(var p in pair)p.active=true;
                    float start=loaded.Top,sum=pair.Sum(p=>p.Top);
                    standing=loaded;player.X=loaded.x+8;player.Y=loaded.Top;player.grounded=true;player.vy256=player.vx4096=0;
                    bool attached=true,rendered=true;string failure="";
                    for(int f=0;f<40;f++)
                    {
                        Step(default);RefreshVisuals();
                        foreach(var p in pair)
                        {
                            var rope=p.scaleRope;
                            attached&=Mathf.Abs(rope.y-(Mathf.Round(p.y)+p.h))<.01f&&
                                Mathf.Abs(rope.Top-rope.data.y)<.01f;
                            var r=rope.visual.GetComponentInChildren<SpriteRenderer>();
                            rendered&=r!=null&&r.enabled&&Mathf.Abs(r.bounds.min.y-(Mathf.Round(p.y)+p.h))<.01f&&
                                Mathf.Abs(r.bounds.max.y-rope.data.y)<.01f;
                            if((!attached||!rendered)&&failure=="")failure=$"frame={f} platform={p.Top} rope={rope.y},{rope.h} anchor={rope.data.y} bounds={(r==null?"none":r.bounds.ToString())} attached={attached} rendered={rendered}";
                        }
                    }
                    Check(label+" ropes follow every painted frame",attached&&rendered,failure);
                    Check(label+" opposite motion conserves span",loaded.Top<start-16&&Mathf.Abs(pair.Sum(p=>p.Top)-sum)<.01f);
                    if(course.Japanese)
                        Check(label+" original rope columns are suppressed",actors.Where(a=>a.data.type=="JTile"&&
                            a.data.spriteOverride!=null&&a.data.spriteOverride.EndsWith("/40")&&
                            pair.Any(p=>p.scaleRope!=a&&p.scaleRope.x==a.x)).All(a=>a.data.hidden));

                    float ropeY=loaded.scaleRope.y,height=loaded.scaleRope.h;
                    phase=GamePhase.Paused;Step(default);RefreshVisuals();
                    Check(label+" pause keeps ropes still",loaded.scaleRope.y==ropeY&&loaded.scaleRope.h==height);
                    phase=GamePhase.Playing;
                    standing=raised;player.X=raised.x+8;player.Y=raised.Top;player.grounded=true;player.vy256=0;
                    float beforeReverse=raised.Top;
                    for(int f=0;f<12;f++){Step(default);RefreshVisuals();}
                    Check(label+" changing sides reverses ropes",raised.Top<beforeReverse&&
                        raised.scaleRope.y==Mathf.Round(raised.y)+raised.h);

                    // Keep the established break threshold and verify the final
                    // attached span freezes instead of extending during free fall.
                    raised.y=176-raised.h-.1f;loaded.vy=-.5f;
                    standing=loaded;player.X=loaded.x+8;player.Y=loaded.Top;player.grounded=true;player.vy256=0;
                    Step(default);RefreshVisuals();
                    Check(label+" both platforms detach",pair.All(p=>p.data.falling&&!p.data.inScale));
                    var spans=pair.Select(p=>new Vector2(p.scaleRope.y,p.scaleRope.h)).ToArray();
                    float oldLeft=pair[0].y,oldRight=pair[1].y;
                    standing=null;player.X=cameraX+8;player.Y=176;player.grounded=false;player.vy256=0;
                    for(int f=0;f<10;f++){Step(default);RefreshVisuals();}
                    Check(label+" broken ropes hang while platforms fall",pair[0].y<oldLeft&&pair[1].y<oldRight&&
                        pair.Select((p,i)=>p.scaleRope.y==spans[i].x&&p.scaleRope.h==spans[i].y).All(v=>v));
                }
            }
            Check("real level pair coverage",pairs==22,"pairs="+pairs);
        }
    }
}
