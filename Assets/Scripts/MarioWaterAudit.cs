using System;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunWaterSurfaceAudit(Action<string,bool,string> record)
        {
            void Check(string name,bool pass,string detail="")=>record("Water surface: "+name,pass,detail);
            bool original=responsiveControls;int entries=0;
            try
            {
                foreach(var course in Levels)
                for(int loc=0;loc<course.locations.Length;loc++)
                {
                    if(!course.areas[course.locations[loc].area].underwater)continue;
                    entries++;
                    foreach(bool responsive in new[]{false,true})
                    foreach(int size in new[]{1,2,3})
                    {
                        responsiveControls=responsive;NewGame(course.id);power=size;LoadLocation(loc);
                        string label=course.id+"/"+loc+" power="+size+" responsive="+responsive;
                        Check(label+" entry starts below the surface",player.Y+PlayerHeight<=176,
                            "head="+(player.Y+PlayerHeight));
                        foreach(var actor in actors)actor.alive=false;
                        player.X=160;player.Y=176-PlayerHeight-4;player.vx4096=player.vy256=0;
                        player.grounded=false;player.crouched=false;standing=null;invulnerable=0;
                        float maxHead=0;bool stayedSwimming=true;
                        for(int f=0;f<240;f++)
                        {
                            Step(new Controls{jump=f%8<4,jumpPressed=f%8==0});
                            maxHead=Mathf.Max(maxHead,player.Y+PlayerHeight);
                            stayedSwimming&=phase==GamePhase.Playing&&player.swimming&&!player.grounded;
                        }
                        Check(label+" repeated strokes cannot leave water",maxHead<=176&&stayedSwimming,"maxHead="+maxHead);
                        float top=player.Y;
                        for(int f=0;f<24;f++)Step(default);
                        Check(label+" releasing swim sinks normally",player.Y<top-1&&player.VY<0);
                        player.Y=176-PlayerHeight-.25f;player.vy256=8*256;
                        Step(new Controls{jump=true});
                        Check(label+" upward impulse stays under surface",player.Y+PlayerHeight<=176&&player.vy256<=0);
                        float pausedY=player.Y;phase=GamePhase.Paused;Step(new Controls{jump=true,jumpPressed=true});
                        Check(label+" pause keeps position",player.Y==pausedY);phase=GamePhase.Playing;
                    }
                    // A mushroom can change the height after the ordinary
                    // movement clamp, followed by a 30-frame power-up freeze.
                    responsiveControls=true;NewGame(course.id);LoadLocation(loc);
                    foreach(var actor in actors)actor.alive=false;
                    power=1;player.X=160;player.Y=160;player.vy256=0;player.grounded=false;standing=null;
                    var mushroom=Spawn(new NodeData{type="Mushroom",group="Character",x=160,y=176,width=16,height=16});
                    mushroom.active=true;Step(default);
                    Check(course.id+"/"+loc+" growth is bounded before power-up freeze",power==2&&player.Y+PlayerHeight<=176);
                    for(int f=0;f<31;f++)Step(default);
                    Check(course.id+"/"+loc+" power-up freeze preserves surface",player.Y+PlayerHeight<=176);

                    NewGame(course.id);LoadLocation(loc);
                    var portal=actors.FirstOrDefault(a=>a.data.type=="PipeHorizontal"||a.data.type=="PipeSideEntry");
                    if(portal!=null&&(portal.data.targetLocation>=0||!string.IsNullOrEmpty(portal.data.targetMap)))
                    {
                        foreach(var a in actors)if(a.IsEnemy)a.alive=false;
                        power=1;player.X=portal.x-11;player.Y=portal.y+4;
                        player.vx4096=4096;player.vy256=0;player.grounded=false;standing=null;
                        cameraX=Mathf.Max(0,portal.x-120);
                        Step(new Controls{horizontal=1});
                        Check(course.id+"/"+loc+" side exit still triggers",inTransport);
                        int destination=Math.Max(0,portal.data.targetLocation);
                        for(int f=0;f<41&&inTransport;f++)Step(default);
                        Check(course.id+"/"+loc+" pipe transfer completes",!inTransport&&location==destination);
                    }
                }
                Check("all water entrance coverage",entries==11,"entries="+entries);
                NewGame("1-1");foreach(var actor in actors)actor.alive=false;
                player.Y=180;player.vy256=256;player.grounded=false;standing=null;Step(default);
                Check("land jumping keeps its upper range",player.Y>180&&player.VY>0);
                NewGame("2-2");LoadLocation(1);foreach(var actor in actors)actor.alive=false;
                player.Y=120;player.vy256=0;player.grounded=false;standing=null;Step(default);
                Check("ordinary water gravity stays unchanged",Mathf.Abs(player.VY+10/256f)<.0001f);
                player.Y=175;Die();float deathY=player.Y;
                for(int f=0;f<35;f++)Step(default);
                Check("death animation can rise above swimming limit",phase==GamePhase.Dying&&player.Y>deathY&&player.Y+PlayerHeight>176);
            }
            finally{responsiveControls=original;NewGame("1-1");}
        }
    }
}
