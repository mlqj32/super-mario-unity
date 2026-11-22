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
        public void RunCorpseAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Corpse: "+n,p,d);
            foreach(string type in new[]{"Goomba","Koopa","Beetle","Spiny","SpinyEgg","Piranha","Blooper","CheepCheep","BulletBill","Lakitu","HammerBro","Bowser","Shell","BeetleShell"})
            foreach(int height in new[]{0,96,176})
            {
                EffectsArena();var a=AuditActor(type,144,height,type=="Bowser"?32:type=="Koopa"||type=="HammerBro"||type=="Piranha"?24:16);
                if(type=="Bowser")a.w=32;
                Kill(a,true);string label=type+" height="+height;
                for(int i=0;i<41&&a.alive;i++)TickActor(a,default,player.Y);
                if(height>0)
                {
                    RefreshVisuals();
                    Check(label+" remains visible beyond old 40-tick cutoff",a.alive&&a.visual.gameObject.activeInHierarchy&&a.Top>0,"age="+a.age+" top="+a.Top);
                    if(height==176)CheckEffectPixels(a,label,Check);
                }
                bool premature=false;int ticks=0;
                while(a.alive&&ticks++<180)
                {TickActor(a,default,player.Y);if(!a.alive&&a.Top>=-32)premature=true;}
                Check(label+" disappears only below visible screen",!premature&&!a.alive&&a.Top<-32,"age="+a.age+" top="+a.Top);
                int awarded=score;Kill(a,true);Check(label+" cannot score twice",score==awarded&&awarded==100);
            }
            // Exercise the real collision paths, rather than only Kill directly.
            foreach(string cause in new[]{"fireball","shell","star","block","stomp"})
            {
                EffectsArena();var a=AuditActor(cause=="stomp"?"HammerBro":"Goomba",160,160,cause=="stomp"?24:16);
                if(cause=="fireball")
                {var shot=AuditActor("Fireball",156,164,8);shot.w=8;shot.vx=4;TickActor(shot,default,0);}
                else if(cause=="shell")
                {var shell=AuditActor("Shell",150,160);shell.vx=3.5f;TickActor(shell,default,0);shell.alive=false;}
                else if(cause=="star")
                {star=300;player.X=158;player.Y=160;TickActor(a,default,160);}
                else if(cause=="block")
                {var block=Spawn(new NodeData{type="Block",group="Solid",x=160,y=160});Bump(block);}
                else AuditLand(a);
                Check(cause+" produces falling corpse",a.state==1000&&score>=100);
                player.X=40;player.Y=0;player.vy256=0;player.grounded=true;star=0;
                for(int i=0;i<41;i++)Step(default);
                Check(cause+" survives old cutoff in real simulation",a.alive&&a.Top>-32&&phase==GamePhase.Playing,"top="+a.Top);
                for(int i=0;i<200;i++)Step(default);
                Check(cause+" releases actor and visual after falling",!a.alive&&!actors.Contains(a));
            }
            EffectsArena();var flat=AuditActor("Goomba");AuditLand(flat);player.X=40;player.Y=0;
            Check("stomped Goomba uses flat non-solid corpse",flat.state==900&&flat.data.type=="DeadGoomba"&&!flat.Solid);
            for(int i=0;i<39;i++)Step(default);
            Check("flat corpse remains briefly",flat.alive);
            for(int i=0;i<3;i++)Step(default);
            Check("flat corpse still expires promptly",!flat.alive);
            // A defeated body crosses pipes/ground; living plants retain their pipe mask.
            foreach(string setting in new[]{"Overworld","Underworld","Underwater","Castle"})
            foreach(string type in new[]{"Goomba","Piranha"})
            {
                EffectsArena(setting);Spawn(new NodeData{type="Pipe",group="Solid",x=144,y=96,width=32,height=96});
                var a=AuditActor(type,152,64,type=="Piranha"?24:16);Kill(a,true);
                foreach(var fx in actors.Where(e=>e.data.group=="Effect"))fx.alive=false;
                CheckEffectPixels(a,setting+" "+type+" corpse in front of pipe",Check);
            }
            EffectsArena();var harmless=AuditActor("Goomba",40,96);Kill(harmless,true);int initialScore=score;
            harmless.vx=0;bool hurt=false;
            for(int i=0;i<100;i++){Step(default);hurt|=phase!=GamePhase.Playing;}
            Check("corpse passes through player without damage or score",!hurt&&score==initialScore);
            EffectsArena();var paused=AuditActor("Koopa",144,176,24);Kill(paused,true);PauseGame();
            for(int i=0;i<90;i++)Step(default);
            Check("pause preserves corpse time and position",paused.age==0&&paused.y==176&&paused.alive);
            ResumeGame();for(int i=0;i<41;i++)Step(default);
            Check("resumed corpse continues falling",paused.age==41&&paused.alive&&paused.y<176);
            NewGame("1-1");
        }
        IEnumerator CorpseSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunCorpseAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            EffectsArena();var a=AuditActor("Goomba",144,176);Kill(a,true);
            foreach(int tick in new[]{0,41,55,70})
            {
                while(a.alive&&a.age<tick)Step(default);
                RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"corpse-fall-"+tick);
            }
            string result=$"MARIO_CORPSE_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"corpse-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"corpse-result.txt"),result);Debug.Log(result);
        }
    }
}
