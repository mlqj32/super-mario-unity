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
        Actor SpringFixture(bool actual=false)
        {
            Actor spring;
            if(actual)
            {
                NewGame("2-1");spring=actors.First(a=>a.data.type=="Springboard");
                foreach(var a in actors)if(a.IsEnemy)a.alive=false;
            }
            else
            {
                AuditArena();spring=Spawn(new NodeData{type="Springboard",group="Solid",x=160,y=29,width=16,height=29});
            }
            spring.active=true;cameraX=spring.x-96;player.X=spring.x+2;player.Y=spring.Top+1;
            player.grounded=false;player.vx4096=0;player.vy256=-512;
            player.jumpCut=true;player.jumpOrigin=player.y256+100*256;standing=null;ClearJumpAssist();
            return spring;
        }
        public void RunSpringAudit(Action<string,bool,string> record)
        {
            bool original=responsiveControls;
            void Check(string n,bool p,string d="")=>record("Springs: "+n,p,d);
            try
            {
                foreach(bool legacy in new[]{false,true})
                foreach(bool boost in new[]{false,true})
                {
                    responsiveControls=!legacy;var spring=SpringFixture();float top=spring.Top,baseY=spring.y,peak=top,min=spring.h;
                    bool baseFixed=true,restored=false;int firstLaunch=-1;var poses=new HashSet<string>();
                    for(int i=0;i<70;i++)
                    {
                        Step(new Controls{jump=boost,jumpPressed=boost&&i==0});
                        if(firstLaunch<0&&player.vy256>0&&!player.grounded)firstLaunch=i;
                        if(firstLaunch>=0&&Mathf.Abs(spring.h-spring.data.height)<.01f)restored=true;
                        peak=Mathf.Max(peak,player.Y);min=Mathf.Min(min,spring.h);baseFixed&=Mathf.Abs(spring.y-baseY)<.01f;
                        if(i<18){RefreshVisuals();poses.Add(spring.visual.CurrentSignature);}
                    }
                    string label="legacy="+legacy+" boost="+boost;
                    Check("compresses and restores "+label,min<spring.data.height-6&&restored,"min="+min+" restored="+restored);
                    Check("rendered poses change "+label,poses.Count>=3,"poses="+poses.Count);
                    Check("base stays fixed "+label,baseFixed);
                    Check("launch follows compression "+label,firstLaunch>=6&&firstLaunch<=20,"tick="+firstLaunch);
                    Check("bounce has useful height after falling "+label,peak-top>(boost?150:45),"rise="+(peak-top));
                    Check("bounce clears jump assistance "+label,jumpBufferTicks==0);
                }
                // The wall next to the actual 2-1 spring is 160px tall. Verify a
                // complete playable arc rather than just checking positive velocity.
                responsiveControls=true;var real=SpringFixture(true);float wallX=real.x+32,wallTop=160;bool landed=false,crossing=false;
                for(int i=0;i<160;i++)
                {
                    crossing|=player.Y>wallTop+4;
                    int dir=crossing&&player.X<wallX+5?1:0;
                    Step(new Controls{jump=true,jumpPressed=i==0,horizontal=dir});
                    if(player.grounded&&player.X>=wallX&&Mathf.Abs(player.Y-wallTop)<.1f){landed=true;break;}
                }
                Check("2-1 spring clears adjacent 160px wall",landed,"player="+player.X+","+player.Y);
                foreach(int direction in new[]{-1,1})
                {
                    var spring=SpringFixture();float lastX=player.X;bool movesForward=true;
                    for(int i=0;i<12;i++)
                    {
                        Step(new Controls{horizontal=direction,run=true,jump=true,jumpPressed=i==0});
                        movesForward&=(player.X-lastX)*direction>=0;lastX=player.X;
                    }
                    Check("direction input during compression does not push rider sideways "+direction,
                        movesForward&&player.VY>10&&springRider==null,"x="+player.X+" vy="+player.VY);
                }
                foreach(int powerLevel in new[]{1,2,3})
                {
                    var spring=SpringFixture();power=powerLevel;float peak=player.Y;bool launchCleared=false;
                    for(int i=0;i<48;i++)
                    {
                        // Tap midway through compression; releasing must not lose the boost.
                        Step(new Controls{jumpPressed=i==5,jump=i==5});peak=Mathf.Max(peak,player.Y);
                        if(player.VY>10)launchCleared=springRider==null&&standing==null&&jumpBufferTicks==0&&coyoteTicks==0;
                    }
                    Check("compression tap boosts every player form "+powerLevel,peak-spring.data.y>150&&launchCleared,"peak="+peak);
                }
                {
                    var spring=SpringFixture();Step(default);player.X=spring.x+spring.w+8;
                    for(int i=0;i<20;i++)Step(default);
                    Check("leaving compressed spring cancels remote launch",springRider==null&&player.VY<=0&&Mathf.Abs(spring.h-spring.data.height)<.01f);
                }
                {
                    var spring=SpringFixture();Step(default);int state=spring.springFrame;float y=player.Y;
                    PauseGame();for(int i=0;i<20;i++)Step(default);
                    Check("pause freezes spring and rider",spring.springFrame==state&&player.Y==y);
                    ResumeGame();for(int i=0;i<12&&player.vy256<=0;i++)Step(default);
                    Check("resume completes spring launch",player.VY>4&&springRider==null);
                }
                foreach(string transition in new[]{"death","load","pipe"})
                {
                    var spring=SpringFixture();Step(default);
                    if(transition=="death")Die();
                    else if(transition=="load")LoadLocation(0);
                    else BeginTransport(new Actor{data=new NodeData{targetLocation=0}},Vector2.down);
                    Check("transition clears spring rider "+transition,springRider==null&&spring.springFrame==0&&Mathf.Abs(spring.h-spring.data.height)<.01f);
                }
                {
                    var spring=SpringFixture();Spawn(new NodeData{type="Stone",group="Solid",x=144,y=80,width=64,height=16});
                    float peak=player.Y;for(int i=0;i<30;i++){Step(new Controls{jump=true,jumpPressed=i==0});peak=Mathf.Max(peak,player.Y);}
                    Check("boost still respects solid ceiling",peak<=48.01f,"peak="+peak);
                }
            }
            finally{responsiveControls=original;NewGame("1-1");}
        }
        IEnumerator SpringSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunSpringAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            var spring=SpringFixture(true);var trace=new List<string>{"tick,playerY,vy,springHeight,grounded"};
            for(int i=0;i<50;i++)
            {
                Step(new Controls{jump=true,jumpPressed=i==0});
                trace.Add(FormattableString.Invariant($"{i},{player.Y},{player.VY},{spring.h},{player.grounded}"));
                if(i==0||i==5||i==11||i==30)
                {RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"spring-"+i);}
            }
            string result="MARIO_SPRING_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"spring-checks.txt"),checks);File.WriteAllLines(Path.Combine(output,"spring-trace.csv"),trace);
            File.WriteAllText(Path.Combine(output,"spring-result.txt"),result);Debug.Log(result);NewGame("1-1");
        }
    }
}
