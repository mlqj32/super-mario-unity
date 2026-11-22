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
        void ControlsArena()
        {AuditArena();responsiveControls=true;ClearJumpAssist();standing=null;}
        void FallingControlsFixture()
        {ControlsArena();player.Y=40;player.vy256=-512;player.grounded=false;}
        void LedgeControlsFixture()
        {
            ControlsArena();actors.Last(a=>a.data.type=="Floor"&&a.alive).w=80;
            player.X=79;player.vx4096=ClassicMotor.RunSpeed;
            Step(new Controls{horizontal=1,run=true});
        }
        public void RunControlsAudit(Action<string,bool,string> record)
        {
            bool original=responsiveControls;
            void Check(string n,bool p,string d="")=>record("Controls: "+n,p,d);
            try
            {
                FallingControlsFixture();int landing=0;
                while(!player.grounded&&landing<60){Step(default);landing++;}
                Check("fall fixture reaches support",player.grounded&&landing>9,"ticks="+landing);
                foreach(int lead in new[]{0,1,4,8,9,11})
                {
                    FallingControlsFixture();
                    for(int i=1;i<=landing;i++)Step(new Controls{jumpPressed=i==landing-lead});
                    Check("released preinput landing lead="+lead,lead<9?player.vy256>0&&!player.grounded:player.grounded&&player.vy256==0,"vy="+player.VY);
                }
                foreach(bool held in new[]{false,true})
                {
                    FallingControlsFixture();
                    for(int i=1;i<=landing;i++)Step(new Controls{jumpPressed=i==landing-4,jump=held&&i>=landing-4});
                    float peak=player.Y;
                    for(int i=0;i<90;i++){Step(new Controls{jump=held});peak=Math.Max(peak,player.Y);}
                    Check(held?"held buffered jump reaches full height":"released buffered tap remains a short hop",held?peak>50:peak>8&&peak<30,"peak="+peak);
                }
                foreach(int delay in new[]{1,3,6,7,10})
                {
                    LedgeControlsFixture();Check("ledge departure is airborne "+delay,!player.grounded&&coyoteTicks==6);
                    for(int i=1;i<=delay;i++)Step(new Controls{horizontal=1,run=true,jump=i==delay,jumpPressed=i==delay});
                    Check("ledge grace delay="+delay,delay<=6?player.vy256>0:player.vy256<0,"vy="+player.VY);
                }
                ControlsArena();Step(new Controls{jump=true,jumpPressed=true});
                Check("ground press takes off immediately",player.Y>0&&player.vy256>0&&coyoteTicks==0);
                for(int i=0;i<35;i++)Step(new Controls{jump=true});
                int fallingSpeed=player.vy256;Step(new Controls{jump=true,jumpPressed=true});
                Check("fresh air press cannot double jump",fallingSpeed<0&&player.vy256<0);
                ControlsArena();int takeoffs=0;bool previous=true;
                for(int i=0;i<180;i++){Step(new Controls{jump=true,jumpPressed=i==0});if(previous&&!player.grounded)takeoffs++;previous=player.grounded;}
                Check("holding jump does not repeat on landing",takeoffs==1&&player.grounded,"takeoffs="+takeoffs);
                foreach(int direction in new[]{-1,1})
                foreach(bool run in new[]{false,true})
                {
                    ControlsArena();player.X=700;cameraX=550;
                    for(int i=0;i<18;i++)Step(new Controls{horizontal=direction,run=run});
                    int cap=run?ClassicMotor.RunSpeed:ClassicMotor.WalkSpeed;
                    Check("reaches unchanged speed cap in 18 ticks "+direction+"/"+run,player.vx4096==direction*cap);
                    float releaseX=player.X;int stopTicks=0;
                    while(player.vx4096!=0&&stopTicks<30){Step(default);stopTicks++;}
                    float stopDistance=Math.Abs(player.X-releaseX);
                    Check("ground stop within 100ms and half tile "+direction+"/"+run,stopTicks<=6&&stopDistance<=8,
                        "ticks="+stopTicks+" distance="+stopDistance);
                    float stopped=player.X;for(int i=0;i<30;i++)Step(default);
                    Check("release stops without slow drift "+direction+"/"+run,player.vx4096==0&&player.X==stopped);
                }
                foreach(bool airborne in new[]{false,true})
                foreach(int direction in new[]{-1,1})
                foreach(bool run in new[]{false,true})
                {
                    int cap=run?ClassicMotor.RunSpeed:ClassicMotor.WalkSpeed;
                    // Use the motor in isolation so a landing cannot mask missing air brakes.
                    var motor=new ClassicMotor{responsive=true,grounded=!airborne,vx4096=direction*cap};
                    int ticks=0;float distance=0;bool monotonic=true;
                    while(motor.vx4096!=0&&ticks<60)
                    {
                        int old=motor.vx4096;float x=motor.X;motor.Step(default);ticks++;
                        distance+=Math.Abs(motor.X-x);
                        monotonic&=Math.Abs(motor.vx4096)<Math.Abs(old)&&motor.vx4096*direction>=0;
                    }
                    Check("bounded release ground/air="+airborne+" dir/run="+direction+"/"+run,
                        monotonic&&motor.vx4096==0&&ticks<=(airborne?9:6)&&distance<=(airborne?10.5f:6.5f),
                        "ticks="+ticks+" distance="+distance);
                    float stopped=motor.X;for(int i=0;i<30;i++)motor.Step(default);
                    Check("ground/air release has no rebound "+airborne+"/"+direction+"/"+run,motor.X==stopped&&motor.vx4096==0);
                    motor.vx4096=direction*cap;float overshoot=0;ticks=0;
                    while(motor.vx4096*direction>0&&ticks<30)
                    {float x=motor.X;motor.Step(new Controls{horizontal=-direction,run=run});ticks++;overshoot+=Math.Max(0,(motor.X-x)*direction);}
                    Check("reverse arrests old direction promptly "+airborne+"/"+direction+"/"+run,
                        ticks<=(airborne?6:4)&&overshoot<=(airborne?6.5f:4f),"ticks="+ticks+" overshoot="+overshoot);
                    motor.Step(new Controls{horizontal=-direction,run=run});
                    Check("reverse accelerates in requested direction "+airborne+"/"+direction+"/"+run,motor.vx4096*direction<0&&motor.facing==-direction);
                }
                foreach(bool airborne in new[]{false,true})
                foreach(int direction in new[]{-1,1})
                {
                    var motor=new ClassicMotor{responsive=true,grounded=!airborne,vx4096=direction*ClassicMotor.RunSpeed};
                    for(int i=0;i<4;i++)motor.Step(new Controls{horizontal=direction});
                    Check("releasing run reaches walk speed even in air "+airborne+"/"+direction,
                        motor.vx4096==direction*ClassicMotor.WalkSpeed,"vx="+motor.VX);
                    for(int i=0;i<30;i++)motor.Step(new Controls{horizontal=direction,run=true});
                    Check("run can be engaged again in air "+airborne+"/"+direction,motor.vx4096==direction*ClassicMotor.RunSpeed);
                }
                // Held-direction full-speed jumps must retain the original reachable distance.
                foreach(int direction in new[]{-1,1})
                {
                    float[] ranges=new float[2];
                    for(int mode=0;mode<2;mode++)
                    {
                        var motor=new ClassicMotor{responsive=mode==1,grounded=true,facing=direction,movingDirection=direction,vx4096=direction*ClassicMotor.RunSpeed};
                        int ticks=0;
                        do{motor.Step(new Controls{horizontal=direction,run=true,jump=true,jumpPressed=ticks==0});ticks++;}while(motor.Y>0&&ticks<120);
                        ranges[mode]=Math.Abs(motor.X);
                    }
                    Check("held run jump preserves gap reach "+direction,ranges[1]>=ranges[0]-1,
                        "legacy="+ranges[0]+" responsive="+ranges[1]);
                }
                ControlsArena();player.vx4096=ClassicMotor.RunSpeed;
                for(int i=0;i<10;i++)Step(new Controls{horizontal=-1,run=true});
                Check("run reversal responds within ten ticks",player.vx4096<0&&player.facing==-1);
                ControlsArena();power=3;player.grounded=false;player.Y=80;
                Step(new Controls{horizontal=-1,firePressed=true});
                Check("air turn and shot use current input facing",player.facing==-1&&actors.Any(a=>a.alive&&a.data.type=="Fireball"&&a.vx<0));
                // Moving support must establish the same immediate landing jump.
                ControlsArena();var platform=Spawn(new NodeData{type="Platform",group="Solid",x=24,y=32,width=64,height=8,vx=1,sliding=true,begin=0,end=200});
                platform.active=true;player.Y=33;player.vy256=-512;player.grounded=false;
                Step(new Controls{jumpPressed=true});
                Check("moving platform accepts landing preinput",player.Y==platform.Top&&player.vy256>0&&!player.grounded&&standing==null);
                ControlsArena();Spawn(new NodeData{type="Springboard",group="Solid",x=32,y=16,width=16,height=16});
                player.Y=17;player.vy256=-512;player.grounded=false;Step(new Controls{jumpPressed=true,jump=true});
                Check("spring compression consumes queued jump",springRider!=null&&jumpBufferTicks==0&&coyoteTicks==0);
                for(int i=0;i<12&&player.vy256<=0;i++)Step(default);
                Check("queued spring boost survives released jump",player.VY>10&&jumpBufferTicks==0&&coyoteTicks==0&&springRider==null);
                ControlsArena();var enemy=AuditActor("Goomba",40,0);player.Y=17;player.vy256=-512;player.grounded=false;
                Step(new Controls{jumpPressed=true});Check("stomp consumes queue and cannot grant air jump",enemy.state>=900&&player.vy256>0&&jumpBufferTicks==0&&coyoteTicks==0);
                ControlsArena();player.swimming=true;player.Y=80;player.grounded=false;
                Step(new Controls{jump=true,jumpPressed=true});int swim=player.vy256;Step(new Controls{jump=true});
                Check("swim presses produce one stroke each",swim>0&&player.vy256<swim&&jumpBufferTicks==0&&coyoteTicks==0);
                foreach(string transition in new[]{"pause","death","load","pipe","confirm","cancel"})
                {
                    FallingControlsFixture();Step(new Controls{jumpPressed=true});coyoteTicks=4;
                    if(transition=="pause"){PauseGame();ResumeGame();}
                    else if(transition=="death"){Die();CompleteDeath();}
                    else if(transition=="load")LoadLocation(0);
                    else if(transition=="pipe")BeginTransport(new Actor{data=new NodeData{targetLocation=0}},Vector2.down);
                    else {PauseGame();RequestRestart();if(transition=="confirm")ConfirmRestart();else CancelRestart();}
                    Check("transition clears queued input "+transition,jumpBufferTicks==0&&coyoteTicks==0);
                }
                ControlsArena();powerFreeze=4;Step(new Controls{jumpPressed=true});
                for(int i=0;i<4;i++)Step(default);
                Check("recent jump survives short transform freeze",player.vy256>0&&!player.grounded);
                ControlsArena();powerFreeze=15;Step(new Controls{jumpPressed=true});for(int i=0;i<15;i++)Step(default);
                Check("stale transform input expires",player.grounded&&jumpBufferTicks==0);
                ControlsArena();responsiveControls=false;player.Y=1;player.vy256=-512;player.grounded=false;
                Step(new Controls{jumpPressed=true});Check("legacy profile keeps strict landing timing",player.grounded&&player.vy256==0);
            }
            finally{responsiveControls=original;NewGame("1-1");}
        }
        IEnumerator ControlsSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunControlsAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            var trace=new List<string>{"scenario,tick,x,y,vx,vy,grounded,jumpPressed"};
            foreach(bool ledge in new[]{false,true})
            {
                if(ledge)LedgeControlsFixture();else FallingControlsFixture();
                for(int i=0;i<30;i++)
                {
                    bool press=i==(ledge?3:7);Step(new Controls{jumpPressed=press,jump=i>=(ledge?3:7),horizontal=ledge?1:0,run=ledge});
                    trace.Add(FormattableString.Invariant($"{(ledge?"ledge":"buffer")},{i},{player.X},{player.Y},{player.VX},{player.VY},{player.grounded},{press}"));
                    if(i==12){RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,ledge?"controls-ledge-jump":"controls-buffered-jump");}
                }
            }
            string result="MARIO_CONTROLS_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"controls-checks.txt"),checks);File.WriteAllLines(Path.Combine(output,"controls-trace.csv"),trace);
            File.WriteAllText(Path.Combine(output,"controls-result.txt"),result);Debug.Log(result);NewGame("1-1");
        }
    }
}
