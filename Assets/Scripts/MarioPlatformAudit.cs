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
        Actor ElevatorFixture(bool upward,bool originalControls=false)
        {
            NewGame("1-2");LoadLocation(1);responsiveControls=!originalControls;ClearJumpAssist();
            var platform=actors.First(a=>a.data.type=="Platform"&&Math.Sign(a.vy)==(upward?1:-1));
            foreach(var a in actors)if(a.IsEnemy||(a.data.type=="Platform"&&a!=platform))a.alive=false;
            platform.active=true;cameraX=platform.x-96;
            player.X=platform.x+12;player.Y=64;player.grounded=false;player.vx4096=0;player.vy256=-256;standing=null;
            return platform;
        }
        public void RunPlatformAudit(Action<string,bool,string> record)
        {
            bool original=responsiveControls;
            void Check(string name,bool pass,string detail="")=>record("Platforms: "+name,pass,detail);
            try
            {
                foreach(bool legacy in new[]{false,true})
                foreach(bool upward in new[]{false,true})
                {
                    string label=(upward?"up":"down")+" legacy="+legacy;
                    foreach(float y in new[]{0f,32f,80f,128f})
                    {
                        var p=ElevatorFixture(upward,legacy);p.y=upward?191.75f:-39.75f;player.Y=y;
                        Step(default);
                        Check("wrap does not catch airborne player "+label+" y="+y,Mathf.Abs(player.Y-y)<=4&&!player.grounded,
                            "before="+y+" after="+player.Y+" platform="+p.Top);
                    }
                    {
                        var p=ElevatorFixture(upward,legacy);p.y=upward?191.75f:-39.75f;
                        player.Y=p.Top;player.grounded=true;player.vy256=0;standing=p;float before=player.Y;
                        Step(default);
                        Check("wrap detaches rider without vertical transfer "+label,
                            Mathf.Abs(player.Y-before)<=4&&!player.grounded&&standing!=p,
                            "before="+before+" after="+player.Y+" platform="+p.Top);
                    }
                    foreach(bool reverseOrder in new[]{false,true})
                    {
                        var p=ElevatorFixture(upward,legacy);p.y=upward?191.75f:-39.75f;
                        var support=Spawn(new NodeData{type="Platform",group="Solid",x=p.x,y=80,width=p.w,height=8,vy=p.vy,movement="movePlatformSpawn"});
                        support.active=true;player.Y=80;player.vy256=0;player.grounded=true;standing=support;
                        if(reverseOrder)actors.Reverse();Step(default);
                        Check("same-column wrap cannot steal another rider "+label+" reversed="+reverseOrder,
                            standing==support&&player.grounded&&Mathf.Abs(player.Y-support.Top)<.01f,
                            "player="+player.Y+" support="+support.Top+" wrapped="+p.Top);
                    }
                    {
                        var p=ElevatorFixture(upward,legacy);p.y=upward?191.75f:-39.75f;Step(default);
                        player.X=p.x+12;player.Y=p.Top+1;player.vy256=-512;player.grounded=false;standing=null;
                        Step(default);
                        Check("recycled platform collides again next tick "+label,standing==p&&player.grounded&&Mathf.Abs(player.Y-p.Top)<.01f);
                    }
                    {
                        var p=ElevatorFixture(upward,legacy);p.y=72;
                        player.Y=p.Top;player.vy256=0;player.grounded=true;standing=p;
                        bool carried=true;
                        for(int i=0;i<40;i++){Step(default);carried&=standing==p&&Mathf.Abs(player.Y-p.Top)<.01f;}
                        Check("ordinary vertical movement carries rider "+label,carried);
                        Step(new Controls{jump=true,jumpPressed=true});
                        Check("rider can jump off elevator "+label,player.vy256>0&&!player.grounded&&standing!=p);
                    }
                }
                foreach(bool upward in new[]{false,true})
                {
                    var p=ElevatorFixture(upward);p.y=upward?191.75f:-39.75f;
                    // A grounded player on a stationary support is another non-rider.
                    Spawn(new NodeData{type="Stone",group="Solid",x=p.x,y=64,width=48,height=16});
                    player.Y=64;player.grounded=true;player.vy256=0;Step(default);
                    Check("wrap cannot pull player off stationary ledge "+upward,player.grounded&&Mathf.Abs(player.Y-64)<.01f&&standing!=p);
                }
                // Exercise several real elevator cycles while sampling a falling player
                // in the shaft. A normal landing can move feet by at most one small step.
                foreach(bool upward in new[]{false,true})
                {
                    var p=ElevatorFixture(upward);float maxStep=0;int wraps=0;
                    for(int i=0;i<900;i++)
                    {
                        player.X=p.x+12;player.Y=64;player.vy256=-256;player.vx4096=0;player.grounded=false;standing=null;
                        float old=p.y;Step(default);if(Mathf.Abs(p.y-old)>100)wraps++;
                        maxStep=Mathf.Max(maxStep,Mathf.Abs(player.Y-64));
                    }
                    Check("multiple real elevator cycles stay local "+upward,wraps>=3&&maxStep<=4,"wraps="+wraps+" maxPlayerStep="+maxStep);
                }
            }
            finally{responsiveControls=original;NewGame("1-1");}
        }
        IEnumerator PlatformSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunPlatformAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            var trace=new List<string>{"tick,playerY,platformTop,grounded"};
            var platform=ElevatorFixture(false);platform.y=-39.75f;
            for(int i=0;i<12;i++)
            {
                if(i>0)Step(default);
                trace.Add(FormattableString.Invariant($"{i},{player.Y},{platform.Top},{player.grounded}"));
                if(i<2){RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"elevator-wrap-"+i);}
            }
            string result="MARIO_PLATFORM_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"platform-checks.txt"),checks);
            File.WriteAllLines(Path.Combine(output,"platform-trace.csv"),trace);
            File.WriteAllText(Path.Combine(output,"platform-result.txt"),result);Debug.Log(result);NewGame("1-1");
        }
    }
}
