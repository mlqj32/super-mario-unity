using System;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        // Hammer Bros can backpedal while aiming; patrol velocity is not facing.
        int HammerBroDirection(Actor a)=>PlayerBounds.center.x>a.Bounds.center.x?1:-1;
        void TickActor(Actor a,Controls input,float oldPlayerY)
        {
            string t=a.data.type;a.age++;if(a.cooldown>0)a.cooldown--;if(a.coinTimer>0)a.coinTimer--;
            if(t=="Firework"){if(a.age>=21)a.alive=false;return;}
            if(t=="Bubble"){a.y+=.5f;if(a.y>=176||a.age>=420)a.alive=false;return;}
            if(t=="BrickShard"){a.x+=a.vx;a.y+=a.vy;a.vy-=.18f;if(a.age>80||a.y<-96)a.alive=false;return;}
            if(t=="CoinPop")
            {a.y+=a.vy;a.vy-=.3f;if(a.age>=32){a.data.type="ScorePop";a.data.text="200";a.x-=8;a.w=24;a.h=8;a.age=0;}return;}
            if(t=="ScorePop"){a.y+=.5f;if(a.age>=30)a.alive=false;return;}
            if(t=="PipeEntry")
            {if(input.down&&Mathf.Abs(player.Y-a.Top)<2&&Mathf.Abs(player.X+6-a.x-16)<8)BeginTransport(a,Vector2.down*.5f);return;}
            if(t=="PipeSideEntry")
            {if(input.horizontal>0&&player.X+12>=a.x&&player.X<a.x+a.w&&player.Y<a.Top&&player.Y+PlayerHeight>a.y)BeginTransport(a,Vector2.right*.5f);return;}
            if(t=="BowserFireStart"&&a.age%110==0)
            {var f=Spawn(new NodeData{type="BowserFire",group="Character",x=cameraX+256,y=48+(a.age%3)*16,width=24,height=8});f.vx=-1.5f;f.active=true;return;}
            if(t=="LoopGate")
            {
                if(!a.triggered&&player.X>=a.x)
                {
                    a.triggered=true;mazePasses++;if(player.grounded&&Mathf.Abs(player.Y-a.data.begin)<4)mazeCorrect++;
                    if(mazePasses>=a.data.section)
                    {
                        if(mazeCorrect<mazePasses)
                        {player.X=Mathf.Max(40,player.X-1024);cameraX=Mathf.Max(0,player.X-112);foreach(var gate in actors.Where(g=>g.data.type=="LoopGate"&&g.x>=player.X))gate.triggered=false;}
                        mazePasses=mazeCorrect=0;
                    }
                }
                return;
            }
            if(a.state>=900)
            {
                if(a.state==1000)
                {
                    a.x+=a.vx;a.y+=a.vy;a.vy-=.2f;
                    // Airborne corpses must finish falling. The old shared
                    // 40-tick squash timer removed them in mid-screen.
                    // The viewport ends at -32; retain the whole body until
                    // it is below that edge with a 32px safety margin.
                    if(a.Top<-64)a.alive=false;
                }
                else if(a.age>40||a.Top<-64)a.alive=false;
                return;
            }
            if(a.state>0&&a.state<=12&&(t=="Brick"||t=="Block"))a.state--;
            if(t=="Section"&&!a.triggered&&a.x<cameraX+288){ExpandSection(a,a.data.section);return;}
            if(t=="SectionDecider"&&!a.triggered&&player.X+12>=a.x){ExpandSection(a,sectionPassed?a.data.pass:a.data.fail);return;}
            if(t=="SectionPass"&&PlayerBounds.Overlaps(a.Bounds))sectionPassed=true;
            if(t=="SectionFail"&&PlayerBounds.Overlaps(a.Bounds))sectionPassed=false;
            if(t=="CheepsStart"&&player.X>=a.x)cheeps=true;
            if(t=="CheepsStop"&&player.X>=a.x)cheeps=false;
            if(t=="BulletBillsStart"&&player.X>=a.x)bills=true;
            if(t=="BulletBillsStop"&&player.X>=a.x)bills=false;
            if(t=="LakituStop"&&player.X>=a.x)StopLakituEncounter();
            if(t=="FlagPole"&&player.X+12>=a.x&&player.X<a.x+12&&player.Y<a.Top)
            {flagX=a.x;PrepareFlagEntry(a);phase=GamePhase.Flag;phaseFrames=0;int points=player.Y>=144?5000:player.Y>=104?2000:player.Y>=64?800:player.Y>=24?400:100;score+=points;ShowScore(points.ToString(),a.x,player.Y+16);BeginFinishEffects();music.Stop();Sound("Flagpole");return;}
            if(t=="CastleAxe"&&PlayerBounds.Overlaps(a.Bounds))
            {a.alive=false;phase=GamePhase.Castle;phaseFrames=0;music.Stop();Sound("Bowser Falls");return;}
            if(t=="Coin"&&PlayerBounds.Overlaps(a.Bounds)){a.alive=false;Coin();return;}
            if(t=="Platform")
            {
                if(a.data.ride){if(standing==a)a.vx=1.5f;a.x+=a.vx;}
                else if(a.data.sliding)
                {if(a.vx==0)a.vx=Math.Max(.25f,a.data.speed);a.x+=a.vx;if(a.x<=a.data.begin){a.x=a.data.begin;a.vx=Math.Abs(a.vx);}if(a.x+a.w>=a.data.end&&a.data.end>a.data.begin){a.x=a.data.end-a.w;a.vx=-Math.Abs(a.vx);}}
                else if(a.data.floating)
                {if(a.vy==0)a.vy=.75f;a.y+=a.vy;if(a.Top>=Math.Max(a.data.begin,a.data.end)){a.y=Math.Max(a.data.begin,a.data.end)-a.h;a.vy=-Math.Abs(a.vy);}if(a.Top<=Math.Min(a.data.begin,a.data.end)){a.y=Math.Min(a.data.begin,a.data.end)-a.h;a.vy=Math.Abs(a.vy);}}
                else if(a.data.falling){if(standing==a)a.vy-=.04f;a.y+=a.vy;}
                else if(a.data.inScale)
                {
                    var partner=actors.FirstOrDefault(b=>b!=a&&b.data.type=="Platform"&&b.data.link==a.data.link);
                    if(standing==a){a.vy=Math.Max(-2,a.vy-.03f);a.y+=a.vy;if(partner!=null)partner.y-=a.vy;}
                    else a.vy=0;
                    if(partner!=null&&partner.Top>176)
                    {
                        UpdateScaleRope(a);UpdateScaleRope(partner);
                        a.data.inScale=partner.data.inScale=false;a.data.falling=partner.data.falling=true;
                        // The unloaded side had zero velocity and otherwise hung
                        // in mid-air forever after its rope was detached.
                        a.vy=Mathf.Min(-.5f,a.vy);partner.vy=Mathf.Min(-.5f,partner.vy);partner.active=true;
                    }
                }
                else if(a.data.movement=="movePlatformSpawn"||a.vy!=0)
                {
                    a.y+=a.vy;
                    if(a.y>192){a.y=-32;a.platformWrapped=true;}
                    else if(a.y<-40){a.y=176;a.platformWrapped=true;}
                }
                return;
            }
            if(t=="Cannon"&&a.age%150==0&&Mathf.Abs(player.X-a.x)>=40&&a.x+a.w>cameraX&&a.x<cameraX+256)
            {var b=Spawn(new NodeData{type="BulletBill",group="Character",x=a.x,y=a.Top,width=16,height=14});b.vx=player.X<a.x?-1.5f:1.5f;b.active=true;Sound("Firework");}
            if(t=="CastleBlock"&&a.data.fireballs>0)
            {
                // Native hazard sprites are children of the block and share its deterministic frame clock.
                if(!a.triggered)
                {a.triggered=true;for(int j=1;j<=a.data.fireballs;j++){var f=Spawn(new NodeData{type="CastleFireball",group="Character",x=a.x+4,y=a.Top-4,width=8,height=8});f.originX=a.x+8;f.originY=a.y+8;f.state=j;f.vx=a.data.speed;f.active=true;}}
            }
            if(t=="CastleFireball")
            {float angle=(frame*(a.vx==0?1:a.vx)*1.4f)*Mathf.Deg2Rad;a.x=a.originX+Mathf.Cos(angle)*a.state*8-4;a.y=a.originY+Mathf.Sin(angle)*a.state*8-4;if(TouchesDamage(a))Hurt();return;}
            if(a.data.group!="Character")return;
            if(t=="Coin")return;
            bool projectile=t=="Fireball"||t=="Hammer"||t=="BowserFire"||t=="BulletBill";
            if((t!="Lakitu"&&a.x+a.w<cameraX-64)||(a.y<-96&&t!="Podoboo")||(projectile&&a.x>cameraX+272))
            {a.alive=false;return;}
            if(t=="Vine")
            {a.h=Mathf.Min(240,a.h+1);if(input.up&&PlayerBounds.Overlaps(a.Bounds))climbing=a;return;}
            bool item=a.IsPowerup;
            if(item&&a.state<0){a.state++;a.y+=.5f;return;}
            if(item&&a.cooldown==0&&PlayerBounds.Overlaps(a.Bounds))
            {
                a.alive=false;if(t=="PoisonMushroom"){Hurt();return;}score+=1000;ShowScore(t=="Mushroom1Up"?"1UP":"1000",a.x+a.w/2,a.Top);
                if(t=="Mushroom1Up"){lives++;Sound("Gain Life");}
                else if(t=="Star"){star=600;PlayMusic();}
                else {power=t=="FireFlower"?(power==1?2:3):Math.Max(power,2);powerFreeze=30;Sound("Powerup");}
                return;
            }
            if(t=="FireFlower")return;
            if(t=="Piranha")
            {
                int cycle=a.age%(level.Japanese&&a.data.red?140:180),wait=level.Japanese?32:48;
                // Gate departure, not the rendered height. Advancing a hidden
                // plant's clock caused it to teleport out at the hold phase.
                // Reference left-edge distances: green 0x21, Japanese red 0x13.
                // Inverted Japanese plants deliberately have no proximity gate.
                if(cycle==wait&&a.data.action!="upside-down"&&Mathf.Abs(player.X-a.x)<(level.Japanese&&a.data.red?19:33))a.age--;
                float amount;
                if(level.Japanese)amount=Mathf.Clamp01(cycle<32?0:cycle<64?(cycle-32)/32f:cycle<108?1:1-(cycle-108)/32f);
                else amount=cycle<48?0:cycle<80?(cycle-48)/32f:cycle<132?1:1-(cycle-132)/48f;
                a.y=a.data.action=="upside-down"?a.originY+a.h-amount*a.h:a.originY-a.h+amount*a.h;if(amount<.1f)return;
            }
            else if(t=="Podoboo")
            {
                if(a.age%150==1){a.y=-32;a.vy=5.5f;}
                // Zero vertical speed at the apex is still airborne. Only the
                // submerged resting position may suspend gravity between jumps.
                if(a.vy!=0||a.y>-32){a.y+=a.vy;a.vy-=.125f;if(a.y<=-32&&a.vy<0){a.y=-32;a.vy=0;}}
                if(a.y<=-32&&a.vy==0)return;
            }
            else if(t=="Blooper")
            {if(a.age%100<45){a.x+=Math.Sign(player.X-a.x)*.5f;a.y+=1;}else a.y-=.5f;a.y=Mathf.Clamp(a.y,16,160);}
            else if(t=="CheepCheep")
            {a.x+=a.vx;if(a.data.jumping){a.y+=a.vy;a.vy-=.08f;}else a.y=a.originY+Mathf.Sin(a.age*.025f)*8;}
            else if(t=="BulletBill"||t=="BowserFire")a.x+=a.vx;
            else if(t=="Lakitu")
            {
                if(TickLakitu(a))return;
            }
            else if(t=="Hammer") {a.x+=a.vx;a.y+=a.vy;a.vy-=.12f;}
            else if(t=="Koopa"&&a.data.floating)
            {a.y=a.originY+Mathf.Sin(a.age*.025f)*Mathf.Max(24,Mathf.Abs(a.data.end-a.data.begin)/2);}
            else if(t=="Koopa"&&a.data.action=="horizontal-flight")
            {a.x=a.originX+Mathf.Sin(a.age*.025f)*48;a.vx=Mathf.Cos(a.age*.025f);}
            else
            {
                if(t=="HammerBro")
                {
                    a.vx=Mathf.Sin(a.age*.04f)*.5f;
                    if(a.age%120==0&&a.grounded)a.vy=4;
                    if(a.age%40==0&&a.age%180<120){var h=Spawn(new NodeData{type="Hammer",group="Character",x=a.x,y=a.Top+8,width=16,height=16});h.vx=HammerBroDirection(a)*1.5f;h.vy=4;h.active=true;}
                }
                if(t=="Bowser")
                {
                    a.vx=Mathf.Sin(a.age*.018f)*.6f;if(a.age%140==0&&a.grounded)a.vy=3;
                    if(a.age%110==0)BowserBreath(a);
                    if(a.data.throwing&&a.age%35==0){var h=Spawn(new NodeData{type="Hammer",group="Character",x=a.x,y=a.Top+8,width=16,height=16});h.vx=-1.5f;h.vy=4;h.active=true;}
                }
                float ox=a.x,oy=a.y;a.x+=a.vx;
                foreach(var b in actors)
                {
                    if(b==a||!b.alive||!b.Solid||b.Platform||b.data.hidden||b.data.type=="DeadGoomba")continue;
                    if(!a.Bounds.Overlaps(b.Bounds))continue;
                    if(a.vx>0)a.x=b.x-a.w;else if(a.vx<0)a.x=b.x+b.w;
                    if(t=="Fireball"){ExplodeFireball(a);break;}a.vx=-a.vx;
                }
                if(!a.alive)return;
                a.y+=a.vy;a.vy=Mathf.Max(-4,a.vy-(t=="Fireball"?.2f:t=="Star"?.125f:.2f));a.grounded=false;
                foreach(var b in actors)
                {
                    if(b==a||!b.alive||!b.Solid||b.data.hidden||b.data.type=="DeadGoomba")continue;
                    if(a.x+a.w<=b.x||a.x>=b.x+b.w)continue;
                    if(oy>=b.Top-.05f&&a.y<=b.Top&&a.vy<=0){a.y=b.Top;a.vy=0;a.grounded=true;}
                    else if(!b.Platform&&a.y>oy&&oy+a.h<=b.y+.05f&&a.Top>=b.y)
                    {a.y=b.y-a.h;a.vy=0;if(t=="Fireball"){ExplodeFireball(a);break;}}
                }
                if(a.grounded)
                {
                    if(t=="Star")a.vy=3;
                    if(t=="Fireball")a.vy=3;
                    if(t=="Koopa"&&a.data.jumping)a.vy=3.5f;
                    if(t=="SpinyEgg"){a.data.type="Spiny";a.vx=player.X<a.x?-.5f:.5f;}
                    if(a.data.smart&&t=="Koopa")
                    {float foot=a.vx<0?a.x-1:a.x+a.w+1;bool support=actors.Any(b=>b!=a&&b.alive&&b.Solid&&!b.data.hidden&&foot>=b.x&&foot<=b.x+b.w&&Mathf.Abs(b.Top-a.y)<2);if(!support)a.vx=-a.vx;}
                }
            }
            if(t=="Fireball")
            {if(!a.alive)return;foreach(var e in actors){if(e==a||!e.alive||e.state>=900||!e.IsEnemy)continue;if(!a.Bounds.Overlaps(e.Bounds))continue;ExplodeFireball(a);if(e.data.type=="Bowser"){if(--e.health<=0)Kill(e,true);}else if(e.data.type!="Beetle"&&e.data.type!="BeetleShell"&&e.data.type!="BulletBill")Kill(e,true);break;}return;}
            if(t=="Shell"||t=="BeetleShell")
            {
                if(Mathf.Abs(a.vx)>1)
                {a.idleFrames=0;int count=actors.Count;for(int i=0;i<count;i++){var e=actors[i];if(e==a||!e.alive||e.state>=900||!e.IsEnemy)continue;if(a.Bounds.Overlaps(e.Bounds))Kill(e,true);}}
                else if(++a.idleFrames>600)
                {a.data.type=t=="Shell"?"Koopa":"Beetle";a.h=t=="Shell"?24:16;a.vx=-.5f;a.age=0;a.idleFrames=0;return;}
            }
            if(item||a.cooldown>0||!a.alive||!PlayerBounds.Overlaps(a.Bounds))return;
            if(star>0&&t!="Podoboo"&&t!="CastleFireball"&&t!="BowserFire"&&t!="Hammer"){Kill(a,true);return;}
            bool stomp=!area.underwater&&player.vy256<=0&&oldPlayerY>=a.Top-6&&t!="Piranha"&&t!="Spiny"&&t!="SpinyEgg"&&t!="Podoboo"&&t!="Bowser"&&t!="BowserFire"&&t!="Hammer";
            if(t=="Shell"||t=="BeetleShell")
            {
                if(stomp||Mathf.Abs(a.vx)<1)
                {
                    bool kick=Mathf.Abs(a.vx)<1;
                    a.vx=kick?(player.X+6<a.x+a.w/2?3.5f:-3.5f):0;
                    a.idleFrames=0;a.cooldown=12;if(kick){score+=400;ShowScore("400",a.x+a.w/2,a.Top);}
                    if(stomp){ClearJumpAssist();player.Y=a.Top;player.vy256=768;player.grounded=false;}
                    Sound("Kick");
                }
                else if(TouchesDamage(a))Hurt();return;
            }
            if(stomp)
            {
                ClearJumpAssist();player.Y=a.Top;player.vy256=input.jump?1024:768;player.grounded=false;a.cooldown=12;
                if(t=="Koopa"&&(a.data.jumping||a.data.floating||a.data.action=="horizontal-flight")){score+=100;ShowScore("100",a.x+a.w/2,a.Top);a.data.jumping=a.data.floating=false;a.data.action=null;a.vx=-.5f;}
                else if(t=="Koopa"||t=="Beetle"){score+=100;ShowScore("100",a.x+a.w/2,a.Top);a.data.type=t=="Koopa"?"Shell":"BeetleShell";a.h=16;a.vx=0;a.age=0;a.idleFrames=0;}
                else Kill(a);Sound("Kick");
            }
            else if(TouchesDamage(a))Hurt();
        }
    }
}
