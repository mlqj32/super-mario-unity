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
        Actor LakituArena()
        {
            AuditArena();Spawn(new NodeData{type="Floor",group="Solid",x=0,y=0,width=12000,height=32});
            invulnerable=100000;return AuditActor("Lakitu",180,144,24);
        }
        public void RunLakituAudit(Action<string,bool,string> record)
        {
            RunLakituMotionAudit(record);
            void Check(string name,bool pass,string detail="")=>record("Lakitu: "+name,pass,detail);
            var cloud=LakituArena();bool visible=true;float maxDistance=0;
            for(int f=0;f<1200;f++)
            {
                Step(new Controls{horizontal=1,run=true});
                if(f>120){visible&=cloud.alive&&cloud.x+cloud.w>=cameraX&&cloud.x<=cameraX+256;maxDistance=Mathf.Max(maxDistance,Mathf.Abs(cloud.x-player.X));}
            }
            Check("1200-frame full-speed pursuit stays visible",visible&&player.X>2800,"maxDistance="+maxDistance+" playerX="+player.X);
            for(int f=0;f<240;f++)Step(default);
            Check("idle patrol stays near Mario",cloud.alive&&Mathf.Abs(cloud.x-player.X)<80);
            for(int f=0;f<120;f++)Step(new Controls{horizontal=-1,run=true});
            Check("reversing does not discard cloud",cloud.alive&&Mathf.Abs(cloud.x-player.X)<100);
            cloud.x=cameraX-100;
            for(int f=0;f<180;f++)Step(new Controls{horizontal=1,run=true});
            Check("off-left pursuer catches up instead of despawning",cloud.alive&&cloud.x+cloud.w>cameraX);
            var duplicate=AuditActor("Lakitu",cloud.x+8,144,24);Step(default);
            Check("repeated marker keeps one cloud",cloud.alive&&!duplicate.alive);
            cloud.age=129;RefreshVisuals();Check("throw windup uses hiding sprite",cloud.visual.CurrentSignature.Contains("hiding"));
            cloud.age=149;int eggs=actors.Count(a=>a.data.type=="SpinyEgg");Step(default);RefreshVisuals();
            Check("throw releases egg and exits hiding",actors.Count(a=>a.data.type=="SpinyEgg")==eggs+1&&!cloud.visual.CurrentSignature.Contains("hiding"));
            Kill(cloud,true);int deadScore=score;
            for(int f=0;f<351;f++)Step(default);
            var replacement=actors.FirstOrDefault(a=>a.alive&&a.state<900&&a.data.type=="Lakitu");
            Check("defeated cloud returns after delay",replacement!=null&&replacement!=cloud&&score==deadScore);
            if(replacement!=null)
            {
                replacement.age=149;
                Spawn(new NodeData{type="LakituStop",group="Trigger",x=player.X,y=208,width=16,height=256});
                var oldEggs=new HashSet<Actor>(actors.Where(a=>a.data.type=="SpinyEgg"||a.data.type=="Spiny"));Step(default);
                Check("stop marker cancels same-frame throw",replacement.fleeing&&!actors.Any(a=>a.data.type=="SpinyEgg"&&!oldEggs.Contains(a)));
                for(int f=0;f<600;f++)Step(default);
                Check("fleeing cloud leaves and cannot respawn",!replacement.alive&&!actors.Any(a=>a.alive&&a.state<900&&a.data.type=="Lakitu"));
            }
            NewGame("1-1");for(int f=0;f<400;f++)Step(default);
            Check("new area resets encounter",!actors.Any(a=>a.alive&&a.data.type=="Lakitu"));
            // Replay travel across actual encounter maps; placement is controlled
            // to test scrolling rather than treating this as a normal playthrough.
            foreach(string id in new[]{"4-1","6-1","8-2","J-4-1","J-4-2","J-8-3"})
            {
                NewGame(id);invulnerable=100000;var first=actors.First(a=>a.data.type=="Lakitu");
                float start=first.x-64;
                bool kept=true;
                for(int f=0;f<420;f++)
                {player.X=start+f*2.5f;player.Y=0;player.vx4096=ClassicMotor.RunSpeed;player.vy256=0;Step(new Controls{horizontal=1,run=true});if(f>90)kept&=actors.Any(a=>a.alive&&a.active&&a.state<900&&a.data.type=="Lakitu"&&a.x+a.w>=cameraX&&a.x<=cameraX+256);}
                Check(id+" sustained scroll pursuit",kept&&phase==GamePhase.Playing);
            }
            NewGame("1-1");
        }
        public void RunLakituMotionAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Lakitu motion: "+n,p,d);
            var cloud=LakituArena();player.X=1000;cloud.x=1180;cameraX=888;
            float maxAcceleration=0,maxSpeed=0,maxVerticalStep=0,maxRunDistance=0;
            bool visible=true;
            for(int f=0;f<1380;f++)
            {
                float speed=f<240?0:f<840?2.5f:f<1080?0:-2.5f;
                player.vx4096=Mathf.RoundToInt(speed*4096);player.facing=speed<0?-1:1;
                player.X+=speed;cameraX=Mathf.Max(0,player.X-112);
                float oldVelocity=cloud.vx,oldY=cloud.y;
                TickActor(cloud,default,player.Y);
                maxAcceleration=Mathf.Max(maxAcceleration,Mathf.Abs(cloud.vx-oldVelocity));
                maxVerticalStep=Mathf.Max(maxVerticalStep,Mathf.Abs(cloud.y-oldY));
                maxSpeed=Mathf.Max(maxSpeed,Mathf.Abs(cloud.vx));
                if(f>=360&&f<840){maxRunDistance=Mathf.Max(maxRunDistance,Mathf.Abs(cloud.x-player.X));visible&=cloud.x+cloud.w>=cameraX&&cloud.x<=cameraX+256;}
            }
            Check("start stop and reversal accelerate gradually",maxAcceleration<=.1001f,"maxDeltaV="+maxAcceleration);
            Check("pursuit speed is bounded",maxSpeed<=3.1001f,"maxSpeed="+maxSpeed);
            Check("vertical bob does not snap",maxVerticalStep<=.2001f,"maxStep="+maxVerticalStep);
            Check("full run remains tracked",visible&&maxRunDistance<100,"maxDistance="+maxRunDistance);
            Check("sustained reversal catches player",Mathf.Abs(cloud.x-player.X)<100,"distance="+Mathf.Abs(cloud.x-player.X));
            cloud=LakituArena();player.X=1000;cloud.x=1048;cameraX=888;
            int changes=0;float lastScale=0;maxAcceleration=0;
            for(int f=0;f<180;f++)
            {
                // Small velocity changes around the old 0.5 threshold used to
                // swap the destination between two unrelated patrol modes.
                player.vx4096=Mathf.RoundToInt((f%2==0?.49f:.51f)*4096);
                player.facing=f%2==0?1:-1;player.X+=player.VX;
                float oldVelocity=cloud.vx;TickActor(cloud,default,player.Y);RefreshVisuals();
                maxAcceleration=Mathf.Max(maxAcceleration,Mathf.Abs(cloud.vx-oldVelocity));
                float scale=cloud.visual.transform.localScale.x;if(f>0&&scale!=lastScale)changes++;lastScale=scale;
            }
            Check("small input changes do not jerk pursuit",maxAcceleration<=.1001f,"maxDeltaV="+maxAcceleration);
            Check("small input changes do not flicker facing",changes<=2,"turns="+changes);
            cloud=LakituArena();player.X=1000;cloud.x=1000;cameraX=888;player.vx4096=0;maxSpeed=0;
            for(int f=0;f<600;f++){TickActor(cloud,default,player.Y);maxSpeed=Mathf.Max(maxSpeed,Mathf.Abs(cloud.vx));}
            Check("nearby idle patrol is slow",maxSpeed<=1.1001f&&Mathf.Abs(cloud.x-player.X)<=40,"maxSpeed="+maxSpeed);
            cloud.y=168;float oldHeight=cloud.y;TickActor(cloud,default,player.Y);
            Check("nonstandard spawn height settles without teleport",Mathf.Abs(cloud.y-oldHeight)<=.2001f);
            cloud.vx=2.5f;StopLakituEncounter();float previous=cloud.vx;TickActor(cloud,default,player.Y);
            Check("leaving encounter also turns smoothly",Mathf.Abs(cloud.vx-previous)<=.1001f);
            foreach(int direction in new[]{-1,1})
            {
                cloud=LakituArena();player.X=1000;cloud.x=1040;cameraX=888;player.vx4096=direction*ClassicMotor.RunSpeed;
                for(int f=0;f<300;f++)TickActor(cloud,default,player.Y);
                Check("blocked player does not drag cloud away "+direction,
                    Mathf.Abs(cloud.x-player.X)<40&&Mathf.Abs(cloud.vx)<1.1f,"distance="+Mathf.Abs(cloud.x-player.X));
            }
            NewGame("1-1");
        }
        IEnumerator LakituOnlySmoke(string output)
        {
            Directory.CreateDirectory(output);yield return LakituSmoke(output);
            Application.logMessageReceived-=SmokeLog;Application.Quit(smokeErrors==0?0:1);
        }
        IEnumerator LakituSmoke(string output)
        {
            int before=smokeErrors,count=0;var results=new List<string>();
            RunLakituAudit((n,p,d)=>{count++;results.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            var cloud=LakituArena();player.X=1000;cloud.x=1064;cameraX=888;UpdateCamera();
            var trace=new List<string>{"frame,playerX,cloudX,velocity,screenX,facing"};
            float previousScreen=0,maxScreenStep=0;
            for(int f=0;f<600;f++)
            {
                var input=f<120||f>=300&&f<420?default:new Controls{horizontal=f<300?1:-1,run=true};
                Step(input);RefreshVisuals();
                var renderer=cloud.visual.GetComponentInChildren<SpriteRenderer>();
                float screen=renderer.bounds.center.x-gameCamera.transform.position.x;
                if(f>0)maxScreenStep=Mathf.Max(maxScreenStep,Mathf.Abs(screen-previousScreen));previousScreen=screen;
                trace.Add(FormattableString.Invariant($"{f},{player.X},{cloud.x},{cloud.vx},{screen},{cloud.lakituFacing}"));
                if(f==0||f==119||f==299||f==419||f==599)
                {yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"lakitu-motion-"+f.ToString("D3"));}
            }
            count++;bool smooth=maxScreenStep<=4;
            results.Add((smooth?"PASS ":"FAIL ")+"Lakitu rendered screen motion maxStep="+maxScreenStep);
            SmokeCheck(smooth,"Lakitu rendered screen position must not jump");
            File.WriteAllLines(Path.Combine(output,"lakitu-motion.csv"),trace);
            NewGame("4-1");invulnerable=100000;
            for(int f=0;f<=840;f++)
            {
                player.X=400+f*2.5f;player.Y=0;player.vx4096=ClassicMotor.RunSpeed;player.vy256=0;
                Step(new Controls{horizontal=1,run=true});invulnerable=0;RefreshVisuals();invulnerable=100000;
                if(f%210==0){yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"lakitu-chase-"+f.ToString("D3"));}
            }
            string result="MARIO_LAKITU_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"lakitu-checks.txt"),results);File.WriteAllText(Path.Combine(output,"lakitu-result.txt"),result);Debug.Log(result);
        }
    }
}
