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
        // Invoked only by editor validation or the explicit -mario-smoke run.
        // Fixtures use runtime actor copies, never edit LevelData assets.
        void AuditArena()
        {
            NewGame("1-1");foreach(var a in actors)a.alive=false;
            Spawn(new NodeData{type="Floor",group="Solid",x=0,y=0,width=4096,height=32});
            player.X=40;player.Y=0;player.grounded=true;frame=0;
        }
        Actor AuditActor(string type,float x=160,float y=0,float h=16)
        {
            var a=Spawn(new NodeData{type=type,group="Character",x=x,y=y+h,width=16,height=h});
            a.active=true;return a;
        }
        void AuditLand(Actor a,bool fromRight=false)
        {player.X=a.x+(fromRight?8:-2);player.Y=a.Top+1;player.vy256=-512;player.vx4096=0;player.grounded=false;Step(default);}
        public void RunGameplayAudit(Action<string,bool,string> record)
        {
            RunHammerBroAudit(record);
            void Verify(string name,bool pass,string detail="")=>record("Audit: "+name,pass,detail);
            foreach(string type in new[]{"Koopa","Beetle"})
            {
                AuditArena();var turtle=AuditActor(type,h:type=="Koopa"?24:16);AuditLand(turtle);
                Verify(type+" first stomp creates stationary shell",turtle.data.type==(type=="Koopa"?"Shell":"BeetleShell")&&turtle.vx==0&&player.VY>0&&score==100);
            }
            foreach(string type in new[]{"Shell","BeetleShell"})
            {
                foreach(bool right in new[]{false,true})
                {
                    AuditArena();var shell=AuditActor(type);AuditLand(shell,right);
                    Verify(type+" top kick away from "+(right?"right":"left"),right?shell.vx<0:shell.vx>0,"vx="+shell.vx);
                    float x=shell.x;player.X=40;player.Y=0;player.vy256=0;
                    for(int i=0;i<8;i++)Step(default);
                    Verify(type+" keeps rolling "+right,Mathf.Abs(shell.x-x)>20&&shell.alive);
                }
                AuditArena();var stopped=AuditActor(type);stopped.vx=3.5f;stopped.age=700;AuditLand(stopped);
                Verify(type+" stomp stops moving shell",stopped.vx==0&&player.VY>0);
                player.X=40;player.Y=0;player.vy256=0;for(int i=0;i<30;i++)Step(default);
                Verify(type+" stopped shell gets fresh wake timer",stopped.data.type==type,"type="+stopped.data.type);
                for(int i=0;i<620;i++)Step(default);
                Verify(type+" idle shell eventually wakes",stopped.data.type==(type=="Shell"?"Koopa":"Beetle"));
                AuditArena();var side=AuditActor(type);player.X=149;Step(default);
                Verify(type+" side kick starts shell",side.vx>0&&phase==GamePhase.Playing);
                AuditArena();var incoming=AuditActor(type,60);incoming.vx=-3.5f;player.X=49;Step(default);
                Verify(type+" moving shell side hit hurts",phase==GamePhase.Dying);
                AuditArena();var bouncing=AuditActor(type,180);bouncing.vx=3.5f;
                Spawn(new NodeData{type="Stone",group="Solid",x=208,y=64,width=16,height=64});
                for(int i=0;i<10;i++)Step(default);
                Verify(type+" rolls back from wall",bouncing.vx<0&&bouncing.x+bouncing.w<=208);
            }
            AuditArena();var goomba=AuditActor("Goomba");AuditLand(goomba);
            Verify("Goomba stomp awards 100 once",score==100,"score="+score);
            Verify("stomped Goomba expires",goomba.state>=900);player.X=40;for(int i=0;i<45;i++)Step(default);
            Verify("dead enemy is removed",!goomba.alive);
            foreach(string type in new[]{"Beetle","BeetleShell"})
            {
                AuditArena();var beetle=AuditActor(type);var shot=AuditActor("Fireball",156);shot.vx=4;Step(default);
                Verify(type+" resists fireball",beetle.alive&&beetle.state<900&&!shot.alive);
            }
            AuditArena();power=3;float originalWidth=areaWidth;
            Step(new Controls{firePressed=true});Step(new Controls{firePressed=true});
            for(int i=0;i<150;i++)Step(default);
            Verify("offscreen fireballs release firing slots",actors.Count(a=>a.alive&&a.data.type=="Fireball")==0);
            AuditActor("Fireball",originalWidth+1000);Verify("projectiles cannot extend course boundaries",areaWidth==originalWidth);
            Step(new Controls{firePressed=true});Verify("player can fire again after shots leave screen",actors.Count(a=>a.alive&&a.data.type=="Fireball")==1);
            AuditArena();var rising=AuditActor("Fireball",160,32,8);rising.vy=4;
            Spawn(new NodeData{type="Stone",group="Solid",x=144,y=64,width=48,height=16});
            bool penetrated=false;for(int i=0;i<5;i++){Step(default);if(rising.alive&&rising.Top>48)penetrated=true;}
            Verify("upward fireball hits ceiling",!rising.alive);
            Verify("upward fireball never penetrates ceiling",!penetrated);
            AuditArena();var lava=AuditActor("Podoboo",180,-80);var jumpStarts=new List<int>();
            for(int i=0;i<450;i++){float vy=lava.vy;Step(default);if(lava.alive&&lava.vy>5&&vy<=0)jumpStarts.Add(i);}
            Verify("lava bubble survives and jumps repeatedly",lava.alive&&jumpStarts.Count>=3,"alive="+lava.alive+" jumps="+jumpStarts.Count);
            foreach(int initial in new[]{1,2,3})
            {
                AuditArena();power=initial;var flower=AuditActor("FireFlower",40);Step(default);
                Verify("fire flower from power "+initial,power==(initial==1?2:3)&&!flower.alive,"power="+power);
            }
            AuditArena();coins=99;int oldLives=lives;var coin=AuditActor("Coin",40);Step(default);Step(default);
            Verify("100 coins grants exactly one life",coins==0&&lives==oldLives+1&&score==200&&!coin.alive);
            AuditArena();var block=Spawn(new NodeData{type="Block",group="Solid",x=160,y=64,contents="Coin"});Bump(block);Bump(block);
            Verify("used question block pays only once",coins==1&&score==200);
            AuditArena();var vine=AuditActor("Vine",40,0,80);var walking=AuditActor("Goomba",220);climbing=vine;
            int age=walking.age;float start=player.Y;for(int i=0;i<12;i++)Step(new Controls{up=true});
            Verify("vine climb moves player",player.Y>start);
            Verify("enemies continue while player climbs",walking.age==age+12,"age="+walking.age);
            Step(new Controls{jumpPressed=true,jump=true});Verify("jump detaches from vine",climbing==null&&player.VY>0);
            AuditArena();star=300;invulnerable=90;Die();for(int i=0;i<151;i++)Step(default);
            Verify("death respawn clears temporary powers",phase==GamePhase.Playing&&star==0&&invulnerable==0&&power==1&&lives==StartingLives-1);
            AuditArena();power=2;Hurt();Die();for(int i=0;i<151;i++)Step(default);
            Verify("death is not delayed by transformation freeze",phase==GamePhase.Playing&&lives==StartingLives-1,"phase="+phase+" lives="+lives);
            AuditArena();Die();int deathFrame=frame;PauseGame(true);Step(default);ResumeGame();
            Verify("closing course menu cannot revive dying player",phase==GamePhase.Dying&&frame==deathFrame);
            phase=GamePhase.Title;PauseGame(true);ResumeGame();Verify("course menu restores title state",phase==GamePhase.Title);
            AuditArena();PauseGame();int paused=frame;Step(new Controls{horizontal=1});ResumeGame();
            Verify("pause freezes simulation and resumes gameplay",frame==paused&&phase==GamePhase.Playing);
            frame=23;NewGame("1-1");int time=timeLeft;Step(default);
            Verify("new course timer does not inherit previous frame phase",timeLeft==time);
            for(int i=0;i<23;i++)Step(default);Verify("timer ticks after 24 gameplay steps",timeLeft==time-1);
            AuditArena();power=3;player.grounded=false;player.Y=48;Step(new Controls{firePressed=true});RefreshVisuals();
            Verify("airborne firing keeps jumping pose",PlayerVisualSignature.Contains("jumping")&&PlayerVisualSignature.Contains("firing"));
            AuditArena();var hammerBro=AuditActor("HammerBro",160,h:24);AuditLand(hammerBro);player.X=40;
            for(int i=0;i<5;i++)Step(default);RefreshVisuals();
            Verify("stomped non-squash enemy falls upside down",hammerBro.state==1000&&hammerBro.vy<0&&hammerBro.visual.GetComponentInChildren<SpriteRenderer>().flipY);
            AuditArena();var platform=Spawn(new NodeData{type="Platform",group="Solid",x=96,y=40,width=48,height=8,vy=.84f});
            platform.active=true;player.X=108;player.Y=40;player.grounded=true;
            for(int i=0;i<20;i++)Step(default);
            Verify("rider remains on moving platform",Mathf.Abs(player.Y-platform.Top)<.01f,"gap="+(player.Y-platform.Top));
            AuditArena();platform=Spawn(new NodeData{type="Platform",group="Solid",x=96,y=40,width=48,height=8,vy=-.84f});
            platform.active=true;player.X=108;player.Y=40;player.grounded=true;
            for(int i=0;i<20;i++)Step(default);
            Verify("rider follows descending platform",Mathf.Abs(player.Y-platform.Top)<.01f);
            AuditArena();platform=Spawn(new NodeData{type="Platform",group="Solid",x=96,y=40,width=48,height=8,sliding=true,begin=96,end=240,speed=1});
            platform.active=true;player.X=108;player.Y=40;player.grounded=true;
            for(int i=0;i<20;i++)Step(default);
            Verify("rider follows horizontal platform",Mathf.Abs(player.X-platform.x-12)<.01f);
            AuditArena();platform=Spawn(new NodeData{type="Platform",group="Solid",x=96,y=200,width=48,height=8,vy=.84f});
            platform.active=true;player.X=108;player.Y=200;player.grounded=true;Step(default);
            Verify("wrapping platform does not teleport rider",player.Y>190&&platform.y<0);
            AuditArena();for(int i=0;i<100;i++){var dead=AuditActor("Fireball",180);dead.alive=false;}
            for(int i=0;i<240;i++)Step(default);
            Verify("expired actor storage is bounded",actors.Count(a=>!a.alive)<10,"deadActors="+actors.Count(a=>!a.alive));
            foreach(string type in new[]{"Goomba","Koopa","Beetle","Spiny","SpinyEgg","Piranha","Blooper","CheepCheep","BulletBill","Lakitu","HammerBro","Bowser","Podoboo","Hammer","BowserFire","CastleFireball"})
            foreach(bool invincible in new[]{false,true})
            {
                AuditArena();player.X=149;player.Y=type=="Lakitu"?148:type=="Blooper"?16:0;star=invincible?300:0;
                var enemy=AuditActor(type,148,player.Y);enemy.age=100;
                if(type=="CastleFireball"){enemy.originX=player.X+6;enemy.originY=player.Y+8;enemy.state=0;}
                Step(default);
                Verify(type+" side contact "+(invincible?"with star":"without star"),phase==(invincible?GamePhase.Playing:GamePhase.Dying),"phase="+phase);
            }
            AuditArena();power=2;player.crouched=true;
            Spawn(new NodeData{type="Stone",group="Solid",x=32,y=36,width=48,height=16});Step(default);
            Verify("crouching player cannot stand into ceiling",player.crouched&&PlayerHeight==16);
            AuditArena();var spring=Spawn(new NodeData{type="Springboard",group="Solid",x=160,y=16,width=16,height=16});
            AuditLand(spring);for(int i=0;i<20&&player.vy256<=0;i++)Step(default);
            Verify("springboard launches player after compression",player.VY>4&&!player.grounded);
            NewGame("2-2");LoadLocation(1);foreach(var a in actors)a.alive=false;
            var waterEnemy=AuditActor("Blooper",160,64);waterEnemy.age=50;AuditLand(waterEnemy);
            // First edge overlap is now forgiving; continue into its body.
            for(int i=0;i<4&&phase==GamePhase.Playing;i++)Step(default);
            Verify("underwater landing on enemy is not a stomp",phase==GamePhase.Dying&&waterEnemy.state<900);
            foreach(string maze in new[]{"4-4","7-4","8-4"})
            {
                NewGame(maze);
                for(int loc=0;loc<level.locations.Length;loc++)
                {
                    LoadLocation(loc);
                    for(int section=0;section<area.sections.Length;section++)
                    foreach(bool pass in new[]{false,true})
                    {
                        LoadLocation(loc);ExpandSection(new Actor{x=512,data=new NodeData{type="Section"}},section);
                        var decider=actors.LastOrDefault(a=>a.data.type=="SectionDecider");
                        if(decider==null)continue;
                        sectionPassed=pass;player.X=decider.x;int before=actors.Count;
                        int next=pass?decider.data.pass:decider.data.fail;
                        TickActor(decider,default,player.Y);
                        Verify(maze+"/"+loc+" section "+section+" "+(pass?"pass":"retry"),decider.triggered&&actors.Count-before==area.sections[next].parts.Sum(p=>p.nodes.Length));
                    }
                }
            }
            // All editable courses: lethal fall, respawn, every declared pipe
            // destination, and reset, in addition to the existing stress suite.
            foreach(var course in Levels)
            {
                NewGame(course.id);Verify(course.id+" starts with ten lives",lives==10);
                player.Y=-70;Step(default);for(int i=0;i<151;i++)Step(default);
                Verify(course.id+" fall and respawn",phase==GamePhase.Playing&&lives==StartingLives-1,"phase="+phase+" lives="+lives);
                foreach(var loc in Enumerable.Range(0,course.locations.Length))
                {
                    LoadLocation(loc);
                    foreach(var portal in actors.Where(a=>a.data.targetLocation>=0||!string.IsNullOrEmpty(a.data.targetMap)).ToArray())
                    {
                        string targetMap=string.IsNullOrEmpty(portal.data.targetMap)?course.id:portal.data.targetMap;
                        int targetLocation=Math.Max(0,portal.data.targetLocation);
                        BeginTransport(portal,Vector2.zero);for(int i=0;i<40;i++)Step(default);
                        Verify(course.id+"/"+loc+" portal "+portal.data.type+"@"+portal.x,level.id==targetMap&&location==targetLocation,"actual="+level.id+"/"+location);
                        LoadLevel(course.id,loc);
                    }
                }
                int finishArea=Array.FindIndex(course.areas,a=>a.nodes.Any(n=>n.type=="FlagPole"||n.type=="CastleAxe")||a.sections.Any(s=>s.parts.Any(p=>p.nodes.Any(n=>n.type=="CastleAxe"))));
                int finishLocation=Array.FindIndex(course.locations,l=>l.area==finishArea);
                Verify(course.id+" has reachable finish area",finishArea>=0&&finishLocation>=0);
                if(finishLocation>=0)
                {
                    LoadLevel(course.id,finishLocation);
                    int finishSection=Array.FindIndex(area.sections,s=>s.parts.Any(p=>p.nodes.Any(n=>n.type=="CastleAxe")));
                    if(finishSection>=0)ExpandSection(new Actor{x=512,data=new NodeData{type="Section"}},finishSection);
                    var goal=actors.First(a=>a.data.type=="FlagPole"||a.data.type=="CastleAxe");
                    player.X=goal.data.type=="FlagPole"?goal.x-8:goal.x;player.Y=goal.data.type=="FlagPole"?64:goal.y;
                    cameraX=Mathf.Max(0,player.X-112);invulnerable=10000;Step(default);
                    Verify(course.id+" finish trigger",phase==(goal.data.type=="FlagPole"?GamePhase.Flag:GamePhase.Castle),"phase="+phase);
                    for(int i=0;i<1200&&(phase==GamePhase.Flag||phase==GamePhase.Castle||phase==GamePhase.Clear);i++)Step(default);
                    bool final=course.Course=="8-4";
                    string next=final?course.id:Levels[Array.IndexOf(Levels,course)+1].id;
                    Verify(course.id+" finish advances correctly",level.id==next&&(!final||phase==GamePhase.Victory),"level="+level.id+" phase="+phase);
                }
            }
        }
        public void RunHammerBroAudit(Action<string,bool,string> record)
        {
            void Check(string name,bool pass,string detail="")=>record("HammerBro: "+name,pass,detail);
            foreach(int direction in new[]{-1,1})
            foreach(int age in new[]{0,20,40,80,100,120})
            {
                AuditArena();var bro=AuditActor("HammerBro",128,48,24);
                player.X=bro.x+direction*64;bro.age=age;bro.vy=age==100?2:0;
                TickActor(bro,default,player.Y);RefreshVisuals();
                Check("faces player independently of patrol and jump "+direction+"/"+age,
                    bro.visual.transform.localScale.x==(direction>0?-1:1),"vx="+bro.vx);
                CheckEffectPixels(bro,"HammerBro pose "+direction+"/"+age,Check);
            }
            foreach(int direction in new[]{-1,1})
            {
                AuditArena();var bro=AuditActor("HammerBro",128,48,24);
                player.X=bro.x+direction*64;bro.age=39;TickActor(bro,default,player.Y);RefreshVisuals();
                var hammer=actors.LastOrDefault(a=>a.data.type=="Hammer");
                Check("throw and visible face agree "+direction,hammer!=null&&hammer.vx==direction*1.5f&&
                    bro.visual.transform.localScale.x==(direction>0?-1:1));
                player.X=bro.x-direction*64;TickActor(bro,default,player.Y);RefreshVisuals();
                Check("crossing player turns living enemy",bro.visual.transform.localScale.x==(direction<0?-1:1));
                if(hammer!=null){TickActor(hammer,default,player.Y);Check("released hammer does not home",hammer.vx==direction*1.5f);}
                bro.age=79;TickActor(bro,default,player.Y);
                Check("next throw follows new player side",actors.Last(a=>a.data.type=="Hammer").vx==-direction*1.5f);
                Kill(bro,true);RefreshVisuals();float deadScale=bro.visual.transform.localScale.x;
                player.X=bro.x+direction*64;RefreshVisuals();
                Check("defeated body retains direction and upside-down pose",bro.visual.transform.localScale.x==deadScale&&
                    bro.visual.GetComponentInChildren<SpriteRenderer>().flipY);
            }
            foreach(var course in Levels)
            {
                if(!course.areas.Any(a=>a.nodes.Any(n=>n.type=="HammerBro")))continue;
                NewGame(course.id);
                foreach(var bro in actors.Where(a=>a.data.type=="HammerBro").ToArray())
                foreach(int direction in new[]{-1,1})
                {
                    bro.active=true;player.X=bro.x+direction*48;player.Y=bro.y-48;bro.age=39;
                    cameraX=Mathf.Max(0,bro.x-128);TickActor(bro,default,player.Y);RefreshVisuals();
                    Check(course.id+" actual enemy faces and throws toward player "+direction,
                        bro.visual.transform.localScale.x==(direction>0?-1:1)&&actors.Last(a=>a.data.type=="Hammer").vx==direction*1.5f);
                }
            }
            NewGame("1-1");
        }
        IEnumerator GameplayAuditSmoke(string output)
        {
            int count=0,failed=0;var details=new List<string>();
            RunGameplayAudit((name,pass,detail)=>{count++;if(!pass)failed++;details.Add((pass?"PASS ":"FAIL ")+name+" "+detail);SmokeCheck(pass,name+" "+detail);});
            File.WriteAllLines(Path.Combine(output,"gameplay-audit-checks.txt"),details);
            foreach(int direction in new[]{-1,1})
            {
                NewGame("3-1");var bros=actors.Where(a=>a.data.type=="HammerBro").Take(2).ToArray();
                cameraX=bros.Min(a=>a.x)-80;player.X=direction<0?bros.Min(a=>a.x)-48:bros.Max(a=>a.x)+48;
                player.Y=0;player.grounded=true;UpdateCamera();
                foreach(var bro in bros){bro.active=true;bro.age=39;TickActor(bro,default,player.Y);}
                RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();
                SaveScreen(output,"hammer-bros-3-1-face-"+(direction<0?"left":"right"));
            }
            AuditArena();var turtle=AuditActor("Koopa",160,h:24);AuditLand(turtle);player.X=40;player.Y=0;player.vy256=0;
            for(int i=0;i<14;i++)Step(default);
            RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"shell-01-stopped");
            AuditLand(turtle);player.X=40;player.Y=0;player.vy256=0;
            for(int i=0;i<24;i++)
            {Step(default);if(i%6==0){RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"shell-rolling-"+i.ToString("D2"));}}
            turtle.cooldown=0;AuditLand(turtle);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"shell-02-stopped-again");
            string result="MARIO_GAMEPLAY_AUDIT_"+(failed==0?"PASS":"FAIL")+" checks="+count+" failed="+failed;
            File.WriteAllText(Path.Combine(output,"gameplay-audit-result.txt"),result);Debug.Log(result);
        }
    }
}
