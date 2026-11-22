using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunPodobooAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Podoboo: "+n,p,d);
            EffectsArena("Castle");var fire=AuditActor("Podoboo",128,-32,16);player.X=40;
            for(int cycle=0;cycle<3;cycle++)
            {
                for(int tick=1;tick<=150;tick++)
                {
                    float oldY=fire.y;TickActor(fire,default,player.Y);
                    if(tick==20)CheckEffectPixels(fire,"Podoboo rising "+cycle,Check);
                    if(tick==44)Check("reaches apex "+cycle,fire.y>80&&fire.vy==0,"y="+fire.y);
                    if(tick==45)Check("gravity continues through zero velocity "+cycle,fire.vy<0);
                    if(tick==60)
                    {
                        RefreshVisuals();Check("falls with downward sprite "+cycle,fire.y<oldY&&fire.vy<0&&fire.visual.GetComponentInChildren<SpriteRenderer>().flipY);
                        CheckEffectPixels(fire,"Podoboo falling "+cycle,Check);
                    }
                    if(tick==90||tick==150)
                    {
                        RefreshVisuals();Check("returns and waits below lava "+cycle+"/"+tick,fire.y==-32&&fire.vy==0);
                        Check("submerged body is hidden "+cycle+"/"+tick,!fire.visual.gameObject.activeSelf);
                    }
                }
            }
            TickActor(fire,default,player.Y);RefreshVisuals();
            Check("next cycle reappears",fire.y>-32&&fire.vy>0&&fire.visual.gameObject.activeSelf);
            fire.age=44;fire.y=91.75f;fire.vy=0;phase=GamePhase.Paused;
            Step(default);Check("pause holds apex",fire.y==91.75f&&fire.vy==0);
            phase=GamePhase.Playing;Step(default);Check("resume continues falling",fire.vy<0);
            foreach(var course in Levels)
            for(int loc=0;loc<course.locations.Length;loc++)
            {
                var map=course.areas[course.locations[loc].area];
                if(!map.nodes.Any(n=>n.type=="Podoboo"))continue;
                if(Array.FindIndex(course.locations,l=>l.area==course.locations[loc].area)!=loc)continue;
                NewGame(course.id);LoadLocation(loc);invulnerable=10000;
                foreach(var actor in actors.Where(a=>a.data.type=="Podoboo").ToArray())
                {
                    cameraX=actor.x-128;actor.active=true;actor.age=0;actor.vy=0;player.X=actor.x+64;
                    for(int f=0;f<100;f++)TickActor(actor,default,player.Y);
                    Check(course.id+"/"+loc+" actual lava fire completes arc",actor.alive&&actor.y==-32&&actor.vy==0);
                }
            }
            NewGame("1-1");
        }
        IEnumerator PodobooSmoke(string output)
        {
            int before=smokeErrors,count=0;var lines=new List<string>();
            RunPodobooAudit((n,p,d)=>{count++;lines.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            var course=Levels.First(l=>!l.Japanese&&l.areas[0].nodes.Any(n=>n.type=="Podoboo"));NewGame(course.id);
            var fire=actors.First(a=>a.data.type=="Podoboo");fire.active=true;fire.age=0;fire.vy=0;
            cameraX=fire.x-96;player.X=fire.x+48;UpdateCamera();
            for(int f=1;f<=151;f++)
            {
                TickActor(fire,default,player.Y);
                if(f==20||f==44||f==60||f==100||f==151)
                {RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"podoboo-cycle-"+f.ToString("D3"));}
            }
            string result=$"MARIO_PODOBOO_{(smokeErrors==before?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"podoboo-checks.txt"),lines);File.WriteAllText(Path.Combine(output,"podoboo-result.txt"),result);Debug.Log(result);
        }
        IEnumerator PodobooOnlySmoke(string output)
        {
            Directory.CreateDirectory(output);yield return PodobooSmoke(output);yield return EffectsSmoke(output);
            Application.logMessageReceived-=SmokeLog;Application.Quit(smokeErrors==0?0:1);
        }
    }
}
