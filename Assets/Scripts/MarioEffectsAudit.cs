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
        void EffectsArena(string setting="Overworld")
        {
            AuditArena();area=new AreaData{setting=setting,sections=Array.Empty<SectionData>(),nodes=Array.Empty<NodeData>()};
            gameCamera.backgroundColor=setting=="Overworld"?new Color32(92,148,252,255):Color.black;
        }
        string CheckEffectPixels(Actor a,string label,Action<string,bool,string> record)
        {
            RefreshVisuals();var image=PiranhaPixels();int painted=0,errors=0;uint hash=2166136261;
            foreach(var r in a.visual.GetComponentsInChildren<SpriteRenderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r.sprite==null)continue;
                var origin=gameCamera.WorldToScreenPoint(r.bounds.min);int x0=Mathf.RoundToInt(origin.x),y0=Mathf.RoundToInt(origin.y);
                int w=r.sprite.texture.width,h=r.sprite.texture.height;var source=r.sprite.texture.GetPixels32();
                if(Mathf.RoundToInt(r.bounds.size.x)!=w||Mathf.RoundToInt(r.bounds.size.y)!=h){errors++;continue;}
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    int sx=r.transform.lossyScale.x<0?w-1-x:x,sy=r.flipY?h-1-y:y;var expected=source[sy*w+sx];
                    if(expected.a==0){hash*=16777619;continue;}
                    painted++;if(x0+x<0||x0+x>=256||y0+y<0||y0+y>=240){errors++;continue;}
                    var actual=image[(y0+y)*256+x0+x];
                    if(expected.r!=actual.r||expected.g!=actual.g||expected.b!=actual.b)errors++;
                    hash=(hash^actual.r)*16777619;hash=(hash^actual.g)*16777619;hash=(hash^actual.b)*16777619;
                }
            }
            record(label+" rendered pixels",painted>0&&errors==0,"painted="+painted+" mismatches="+errors);
            return hash.ToString("X8");
        }
        Actor ImpactFixture(string kind)
        {
            EffectsArena();Actor shot;
            if(kind.StartsWith("wall"))
            {
                Spawn(new NodeData{type="Pipe",group="Solid",x=160,y=80,width=32,height=80});
                shot=AuditActor("Fireball",kind=="wall-left"?148:196,32,8);shot.vx=kind=="wall-left"?4:-4;
            }
            else if(kind=="ceiling")
            {Spawn(new NodeData{type="Stone",group="Solid",x=144,y=64,width=48,height=16});shot=AuditActor("Fireball",160,38,8);shot.vy=4;}
            else
            {AuditActor(kind,160,32,kind=="Bowser"?32:16);shot=AuditActor("Fireball",152,32,8);shot.vx=4;}
            shot.w=shot.data.width=8;
            for(int i=0;i<4&&shot.alive;i++)Step(default);
            return shot;
        }
        public void RunEffectsAudit(Action<string,bool,string> record)
        {
            RunPodobooAudit(record);
            void Check(string n,bool p,string d="")=>record("Effects: "+n,p,d);
            foreach(string kind in new[]{"wall-left","wall-right","ceiling","Goomba","Beetle","BeetleShell","BulletBill","Bowser"})
            {
                var shot=ImpactFixture(kind);var blasts=actors.Where(a=>a.alive&&a.data.type=="Firework").ToArray();
                Check(kind+" impact spawns one explosion",!shot.alive&&blasts.Length==1);
                if(blasts.Length==0)continue;var blast=blasts[0];ExplodeFireball(shot);
                Check(kind+" cannot duplicate explosion",actors.Count(a=>a.data.type=="Firework")==1);
                var cells=new HashSet<string>();
                foreach(int age in new[]{0,7,14}){blast.age=age;cells.Add(CheckEffectPixels(blast,kind+" explosion "+age,Check));}
                Check(kind+" displays three explosion cells",cells.Count==3);
                int points=score;blast.age=20;Step(default);Check(kind+" explosion expires without damage or points",!blast.alive&&score==points&&phase==GamePhase.Playing);
            }
            EffectsArena();var off=AuditActor("Fireball",600);off.vx=4;Step(default);
            Check("offscreen cleanup does not explode",!off.alive&&!actors.Any(a=>a.data.type=="Firework"));
            EffectsArena();var bounce=AuditActor("Fireball",160,1,8);bounce.vy=-2;Step(default);
            Check("ground bounce does not explode",bounce.alive&&bounce.vy>0&&!actors.Any(a=>a.data.type=="Firework"));
            ImpactFixture("wall-left");power=3;Step(new Controls{firePressed=true});
            Check("explosion releases projectile slot",actors.Count(a=>a.alive&&a.data.type=="Fireball")==1);
            foreach(string setting in new[]{"Overworld","Underworld","Castle"})
            {
                EffectsArena(setting);var block=Spawn(new NodeData{type="Block",group="Solid",x=144,y=64,contents="Coin"});Bump(block);
                var coin=actors.First(a=>a.data.type=="CoinPop");var cells=new HashSet<string>();
                foreach(int age in new[]{0,2,4,6}){coin.age=age;cells.Add(CheckEffectPixels(coin,setting+" pop coin "+age,Check));}
                Check(setting+" coin rotates through four frames",cells.Count==4);
                coin.age=31;Step(default);Check("coin becomes 200 without duplicate points "+setting,coin.data.type=="ScorePop"&&score==200&&coins==1);
                CheckEffectPixels(coin,setting+" 200 popup",Check);
                coin.age=29;Step(default);Check(setting+" score text expires",!coin.alive);
                power=2;var brick=Spawn(new NodeData{type="Brick",group="Solid",x=160,y=96});Bump(brick);
                var shards=actors.Where(a=>a.data.type=="BrickShard").ToArray();
                Check(setting+" brick produces four fragments",!brick.alive&&shards.Length==4);
                foreach(var shard in shards){shard.x+=shard.vx*8;CheckEffectPixels(shard,setting+" brick fragment",Check);}
                RefreshVisuals();Check(setting+" fragments use dedicated 8px art",shards.All(a=>a.visual.CurrentSignature.StartsWith("Character BrickShard")&&a.visual.GetComponentInChildren<SpriteRenderer>().sprite.texture.width==8));
            }
            foreach(string type in new[]{"Goomba","Mushroom","FireFlower","Mushroom1Up","Star"})
            {
                EffectsArena();var source=AuditActor(type,type=="Goomba"?160:40);if(type=="Goomba")Kill(source,true);else Step(default);
                var text=actors.FirstOrDefault(a=>a.data.type=="ScorePop");Check(type+" awards visible score label",text!=null);
                if(text!=null){text.x=120;text.y=64;CheckEffectPixels(text,type+" score text",Check);}
            }
            EffectsArena();var shell=AuditActor("Shell",156);shell.vx=3.5f;AuditActor("Goomba",162);AuditActor("Koopa",166);
            Step(default);Check("shell chain safely creates multiple labels",actors.Count(a=>a.data.type=="ScorePop")==2&&score==200);
            foreach(var state in new[]{GamePhase.Flag,GamePhase.Castle,GamePhase.Clear,GamePhase.Dying})
            {
                EffectsArena();var burst=Effect("Firework",160,80,16,16);phase=state;phaseFrames=0;
                for(int i=0;i<22;i++)Step(default);Check(state+" does not freeze transient effects",!burst.alive);
            }
            EffectsArena();area.underwater=true;frame=95;player.Y=40;Step(default);
            var bubble=actors.FirstOrDefault(a=>a.data.type=="Bubble");Check("underwater breathing emits bubble",bubble!=null);
            if(bubble!=null){CheckEffectPixels(bubble,"water bubble",Check);float y=bubble.y;TickActor(bubble,default,0);Check("bubble rises",bubble.y>y);bubble.y=176;TickActor(bubble,default,0);Check("bubble expires at surface",!bubble.alive);}
            foreach(bool japan in new[]{false,true})for(int digit=0;digit<10;digit++)
            {int expected=japan?(digit%2==0?6:3):(digit==1||digit==3||digit==6?digit:0);Check("firework rule "+japan+" digit "+digit,FireworkCount(japan,300+digit,digit)==expected);if(japan)Check("Japanese unmatched digits "+digit,FireworkCount(true,300+digit,(digit+1)%10)==0);}
            EffectsArena();timeLeft=6;BeginFinishEffects();timeLeft=0;phase=GamePhase.Clear;phaseFrames=99;
            for(int i=0;i<160;i++)Step(default);
            Check("six fireworks finish before advancing",level.id=="1-1"&&finishEmitted==6&&score==3000);
            for(int i=0;i<30&&level.id=="1-1";i++)Step(default);
            Check("fireworks allow next course",level.id=="1-2");
            EffectsArena();var targetBoss=AuditActor("Bowser",160,0,32);targetBoss.w=32;
            for(int i=0;i<5;i++){var shot=AuditActor("Fireball",156,8,8);shot.w=8;shot.vx=4;TickActor(shot,default,0);}
            Check("five fireballs defeat Bowser with five impacts",targetBoss.health==0&&targetBoss.state==1000&&actors.Count(a=>a.data.type=="Firework")==5);
            foreach(var effect in actors.Where(a=>a.data.group=="Effect"))effect.alive=false;
            CheckEffectPixels(targetBoss,"defeated Bowser upside down",Check);
            foreach(string id in new[]{"1-4","J-1-4"})
            {
                NewGame(id);var axe=actors.First(a=>a.data.type=="CastleAxe");var boss=actors.First(a=>a.data.type=="Bowser");
                var bridge=actors.First(a=>a.data.type=="CastleBridge");float width=bridge.w,bossY=boss.y;
                player.X=axe.x;player.Y=axe.y;invulnerable=10000;Step(default);Check(id+" axe starts collapse",phase==GamePhase.Castle);
                for(int i=0;i<5;i++)Step(default);cameraX=Mathf.Max(0,bridge.x-112);RefreshVisuals();
                var bridgeRenderer=bridge.visual.GetComponentInChildren<SpriteRenderer>();
                Check(id+" bridge removes visible eight-pixel section",bridgeRenderer!=null&&bridge.w==width-8&&Mathf.Abs(bridgeRenderer.bounds.size.x-bridge.w)<.01f);
                Check(id+" Bowser visibly falls",boss.y<bossY);
            }
            EffectsAnimationAudit(Check);BossEffectsAudit(Check);NewGame("1-1");
        }
        void EffectsAnimationAudit(Action<string,bool,string> check)
        {
            foreach(string type in new[]{"Fireball","FireFlower","Star","Hammer","CastleFireball","BowserFire","Podoboo","Coin","BrickShard"})
            {
                EffectsArena("Castle");int w=type=="BowserFire"?24:type=="Podoboo"||type=="Star"?14:type=="Coin"?10:type=="FireFlower"||type=="Hammer"?16:8;
                int h=type=="Coin"?14:type=="BowserFire"||type=="Fireball"||type=="CastleFireball"||type=="BrickShard"?8:16;
                Spawn(new NodeData{type="Stone",group="Solid",x=120,y=100,width=40,height=40});
                var actor=AuditActor(type,128,70,h);actor.w=actor.data.width=w;var hashes=new HashSet<string>();
                int[] ages=type=="BrickShard"?new[]{0,4}:type=="BowserFire"||type=="Podoboo"?new[]{0,8}:type=="Coin"?new[]{0,24,32}:new[]{0,4,8,12};
                foreach(int age in ages){actor.age=age;actor.vy=age==0?2:-2;hashes.Add(CheckEffectPixels(actor,type+" on castle wall "+age,check));}
                check(type+" painted animation states",hashes.Count==ages.Length,"states="+hashes.Count);
            }
        }
        void BossEffectsAudit(Action<string,bool,string> check)
        {
            int bosses=0;
            foreach(var course in Levels.Where(l=>l.Stage==4))
            for(int loc=0;loc<course.locations.Length;loc++)
            {
                int ai=course.locations[loc].area;if(Array.FindIndex(course.locations,l=>l.area==ai)!=loc)continue;
                NewGame(course.id);LoadLocation(loc);
                for(int section=0;section<area.sections.Length;section++)
                    if(area.sections[section].parts.Any(p=>p.nodes.Any(n=>n.type=="Bowser")))
                        ExpandSection(Spawn(new NodeData{type="Section",group="Trigger",x=1024,section=section}),section);
                var enemies=actors.Where(a=>a.data.type=="Bowser").ToArray();
                foreach(var boss in enemies)
                {
                    bosses++;string label=course.id+" boss "+bosses;cameraX=boss.x-144;player.X=boss.x-96;player.Y=boss.y;invulnerable=10000;boss.active=true;
                    var hashes=new HashSet<string>();
                    foreach(int age in new[]{12,22,0,10}){boss.age=age;hashes.Add(CheckEffectPixels(boss,label+" pose "+age,check));}
                    check(label+" four body/mouth poses",hashes.Count==4,"states="+hashes.Count);
                    for(int i=0;i<60;i++)TickActor(boss,default,player.Y);
                    check(label+" remains on castle terrain",boss.alive&&boss.y>-32,"y="+boss.y);
                    boss.age=109;TickActor(boss,default,player.Y);var fire=actors.LastOrDefault(a=>a.data.type=="BowserFire");
                    check(label+" emits mouth flame",fire!=null&&fire.x<boss.x,"bossX="+boss.x);
                    if(fire!=null)
                    {
                        CheckEffectPixels(fire,label+" breath",check);fire.age=8;CheckEffectPixels(fire,label+" inverted breath frame",check);
                        float x=fire.x;TickActor(fire,default,player.Y);check(label+" breath travels",fire.x<x,"");
                    }
                    player.X=boss.x+80;boss.age=109;TickActor(boss,default,player.Y);fire=actors.LastOrDefault(a=>a.data.type=="BowserFire");RefreshVisuals();
                    check(label+" facing matches breath direction",fire!=null&&boss.visual.transform.localScale.x<0&&fire.vx>0&&fire.x>boss.x,"");
                }
            }
            check("all castle bosses covered",bosses>=17,"bosses="+bosses);
        }
        IEnumerator EffectsSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunEffectsAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            ImpactFixture("wall-left");var explosion=actors.First(a=>a.data.type=="Firework");
            foreach(int age in new[]{0,7,14}){explosion.age=age;RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"explosion-"+age);}
            foreach(string setting in new[]{"Overworld","Underworld","Castle"})
            {
                EffectsArena(setting);power=2;Bump(Spawn(new NodeData{type="Brick",group="Solid",x=160,y=96}));
                Bump(Spawn(new NodeData{type="Block",group="Solid",x=96,y=64,contents="Coin"}));
                for(int i=0;i<8;i++)Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"coin-shards-"+setting);
                for(int i=0;i<25;i++)Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"coin-score-"+setting);
            }
            foreach(string id in new[]{"1-4","8-4","J-1-4","J-8-4"})
            {
                NewGame(id);int locationIndex=Array.FindIndex(level.locations,l=>level.areas[l.area].nodes.Any(n=>n.type=="Bowser"));LoadLocation(locationIndex);
                var boss=actors.First(a=>a.data.type=="Bowser");cameraX=boss.x-144;player.X=boss.x-96;player.Y=boss.y;power=3;boss.age=109;TickActor(boss,default,player.Y);
                foreach(int age in new[]{0,8}){var flame=actors.Last(a=>a.data.type=="BowserFire");flame.age=age;RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"boss-"+id+"-"+age);}
            }
            NewGame("1-1");timeLeft=6;var pole=actors.First(a=>a.data.type=="FlagPole");player.X=pole.x-8;player.Y=100;invulnerable=10000;
            for(int i=0;i<240;i++){Step(default);if(finishEmitted==1){RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"finish-firework");break;}}
            string result="MARIO_EFFECTS_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"effects-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"effects-result.txt"),result);Debug.Log(result);
        }
    }
}
