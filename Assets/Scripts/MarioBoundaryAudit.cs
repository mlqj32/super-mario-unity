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
        public void RunBoundaryAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Boundary: "+n,p,d);
            bool oldAudio=auditAudioPlayback,oldValidation=validationMode;
            try
            {
                foreach(var state in new[]{GamePhase.Playing,GamePhase.Flag,GamePhase.Castle,GamePhase.Clear})
                {
                    EffectsArena();phase=state;phaseFrames=20;
                    bool consumed=HandleMenuInput(new MarioInputFrame{pause=true},1);
                    int pausedFrame=frame;Step(default);
                    Check(state+" pause input suspends simulation",consumed&&phase==GamePhase.Paused&&frame==pausedFrame);
                    if(phase==GamePhase.Paused)ResumeGame();
                    Check(state+" resume restores exact phase",phase==state);
                }
                foreach(var state in new[]{GamePhase.Playing,GamePhase.Flag,GamePhase.Castle,GamePhase.Clear,GamePhase.Dying})
                {
                    EffectsArena();phase=state;usingGamepad=padWasConnected=true;
                    TrackInputDevice(default);
                    Check(state+" disconnect pauses simulation",phase==GamePhase.Paused);
                    if(phase==GamePhase.Paused)ResumeGame();
                    Check(state+" disconnect resumes exact phase",phase==state);
                    EffectsArena();phase=state;validationMode=false;OnApplicationFocus(false);validationMode=true;
                    Check(state+" lost focus pauses simulation",phase==GamePhase.Paused);
                    if(phase==GamePhase.Paused)ResumeGame();
                    Check(state+" focus pause restores exact phase",phase==state);
                }
                EffectsArena();Die();int beforeLives=lives;
                HandleMenuInput(new MarioInputFrame{pause=true,skip=true},2);
                Check("death Start still skips once",phase==GamePhase.Playing&&lives==beforeLives-1);
                foreach(var state in new[]{GamePhase.Flag,GamePhase.Castle,GamePhase.Clear,GamePhase.Dying})
                {
                    EffectsArena();var corpse=AuditActor("Goomba",144,176);Kill(corpse,true);phase=state;player.Y=176;
                    float before=corpse.y;Step(default);
                    Check(state+" corpse continues falling",corpse.age==1&&corpse.y==before+3);
                    for(int i=0;i<110&&corpse.alive;i++)Step(default);
                    Check(state+" corpse exits and expires",!corpse.alive&&corpse.Top<-32);
                }
                EffectsArena();var deadBoss=AuditActor("Bowser",144,128,32);Kill(deadBoss,true);phase=GamePhase.Castle;
                float bossY=deadBoss.y;Step(default);
                Check("defeated Bowser moves once during bridge collapse",deadBoss.y==bossY+3&&deadBoss.age==1);
                foreach(bool reverse in new[]{false,true})
                {
                    EffectsArena();var left=Spawn(new NodeData{type="Platform",group="Solid",x=144,y=64,width=48,height=8,inScale=true,link="audit-scale"});
                    var right=Spawn(new NodeData{type="Platform",group="Solid",x=224,y=176,width=48,height=8,inScale=true,link="audit-scale"});
                    left.active=right.active=true;left.vy=-.5f;standing=left;player.X=156;player.Y=64;player.grounded=true;
                    if(reverse)actors.Reverse();Step(default);
                    Check("scale rope breaks both sides "+reverse,left.data.falling&&right.data.falling&&!left.data.inScale&&!right.data.inScale);
                    float leftY=left.y,rightY=right.y;standing=null;player.X=40;player.Y=0;player.vy256=0;player.grounded=true;
                    for(int i=0;i<10;i++)Step(default);
                    Check("both detached scale platforms fall "+reverse,left.y<leftY&&right.y<rightY,"left="+(left.y-leftY)+" right="+(right.y-rightY));
                }
                foreach(var course in Levels)
                for(int loc=0;loc<course.locations.Length;loc++)
                {
                    if(!course.areas[course.locations[loc].area].underwater)continue;
                    NewGame(course.id);LoadLocation(loc);foreach(var actor in actors)actor.alive=false;
                    player.Y=120;player.vy256=0;player.grounded=false;Step(default);
                    Check(course.id+" water entry has water gravity before first stroke",Mathf.Abs(player.VY+10/256f)<.0001f,"vy="+player.VY);
                }
                foreach(bool invincible in new[]{false,true})
                {
                    EffectsArena();auditAudioPlayback=true;timeLeft=80;star=invincible?100:0;PlayMusic();
                    Check("low time music survives track selection star="+invincible,Mathf.Approximately(music.pitch,1.15f));
                    if(invincible){star=1;Step(default);Check("star expiration restores urgent music",star==0&&Mathf.Approximately(music.pitch,1.15f));}
                    LoadLocation(0);Check("low time music survives area transfer",Mathf.Approximately(music.pitch,1.15f));
                    NewGame("1-1");Check("new course restores normal music speed",Mathf.Approximately(music.pitch,1));
                    auditAudioPlayback=false;
                }
                EffectsArena();ShowScore("100",160,80);var label=actors.Last();Effect("Firework",148,96,24,16);
                CheckEffectPixels(label,"score stays readable over explosion",Check);
                EffectsArena();var visual=AuditActor("Goomba",144,80).visual;
                visual.Set("Character","Goomba","Overworld normal",16,16,1);
                visual.SetText("",8);
                Check("empty text removes stale sprite",visual.GetComponentsInChildren<SpriteRenderer>().All(r=>!r.enabled));
            }
            finally
            {
                auditAudioPlayback=oldAudio;validationMode=oldValidation;usingGamepad=padWasConnected=false;
                menuNeutralRequired=false;heldMenuDirection=0;NewGame("1-1");
            }
        }
        // Longer repeatable input sequences exercise state and numeric stability,
        // not an automatic proof that every course can be completed.
        public void RunStabilityAudit(Action<string,bool,string> record)
        {
            foreach(var course in Levels)
            {
                var random=new System.Random(4100+Array.IndexOf(Levels,course));
                NewGame(course.id);invulnerable=100000;bool stable=true;int restarts=0;
                for(int i=0;i<1200;i++)
                {
                    if(phase==GamePhase.Dying){CompleteDeath();restarts++;invulnerable=100000;}
                    if(phase==GamePhase.GameOver){NewGame(course.id);invulnerable=100000;}
                    if(i%137==0&&phase==GamePhase.Playing)
                    {PauseGame();int pausedFrame=frame;Step(default);stable&=frame==pausedFrame;ResumeGame();}
                    var input=new Controls{horizontal=i%180<140?1:-1,run=i%100<70,jump=i%70<30,jumpPressed=i%70==0,firePressed=i%20==0,down=random.Next(50)==0,up=random.Next(70)==0};
                    Step(input);
                    stable&=Error==null&&float.IsFinite(player.X)&&float.IsFinite(player.Y)&&float.IsFinite(player.VX)&&float.IsFinite(player.VY)&&timeLeft>=0&&lives>=0;
                }
                stable&=actors.All(a=>float.IsFinite(a.x)&&float.IsFinite(a.y)&&float.IsFinite(a.vx)&&float.IsFinite(a.vy));
                record("Stability: "+course.id+" 1200 mixed-input frames",stable,"deaths="+restarts+" actors="+actors.Count);
            }
            NewGame("1-1");
        }
        IEnumerator BoundarySmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            void Check(string n,bool p,string d=""){count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);}
            RunBoundaryAudit(Check);RunStabilityAudit(Check);
            EffectsArena();auditAudioPlayback=true;
            // Let deferred destruction from the stress run finish before audio
            // timing begins. Keep the old clip alive across a slow render frame.
            yield return null;
            bool oldLoop=sounds.loop;
            try
            {
                sounds.clip=Resources.Load<AudioClip>("Audio/Powerup");sounds.loop=true;sounds.Play();
                float deadline=Time.realtimeSinceStartup+2;
                while(sounds.timeSamples==0&&Time.realtimeSinceStartup<deadline)yield return null;
                Check("sound cleanup fixture has active samples",sounds.isPlaying&&sounds.timeSamples>0,
                    "clip="+(sounds.clip==null?"null":sounds.clip.name)+" samples="+sounds.timeSamples+" playing="+sounds.isPlaying);
                Die();Check("death clears earlier sound playback",sounds.timeSamples==0&&!music.isPlaying,"oldClipSamples="+sounds.timeSamples);
            }
            finally{sounds.Stop();sounds.clip=null;sounds.loop=oldLoop;auditAudioPlayback=false;}
            EffectsArena();ShowScore("100",160,80);Effect("Firework",148,96,24,16);RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"score-over-explosion");
            string result=$"MARIO_BOUNDARY_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"boundary-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"boundary-result.txt"),result);Debug.Log(result);
        }
    }
}
