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
        void AudioFinishFixture(string id)
        {
            NewGame(id);
            int finishArea=Array.FindIndex(level.areas,a=>a.nodes.Any(n=>n.type=="FlagPole"||n.type=="CastleAxe")||a.sections.Any(s=>s.parts.Any(p=>p.nodes.Any(n=>n.type=="CastleAxe"))));
            LoadLocation(Array.FindIndex(level.locations,l=>l.area==finishArea));
            int section=Array.FindIndex(area.sections,s=>s.parts.Any(p=>p.nodes.Any(n=>n.type=="CastleAxe")));
            if(section>=0)ExpandSection(new Actor{x=512,data=new NodeData{type="Section"}},section);
            var goal=actors.First(a=>a.data.type=="FlagPole"||a.data.type=="CastleAxe");
            timeLeft=202;coins=0;invulnerable=10000;
            player.X=goal.data.type=="FlagPole"?goal.x-8:goal.x;player.Y=goal.data.type=="FlagPole"?64:goal.y;
            cameraX=Mathf.Max(0,player.X-112);Step(default);
            for(int i=0;i<300&&phase!=GamePhase.Clear;i++)Step(default);
        }
        public void RunFinishAudioAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record(n,p,d);
            bool savedMute=muted;float savedVolume=masterVolume;
            try
            {
            foreach(string id in new[]{"1-1","4-4","8-4","J-1-1","J-4-4","J-8-4"})
            foreach(bool silent in new[]{false,true})
            {
                muted=silent;ApplyVolume();
                AudioFinishFixture(id);string name=Stage==4?"World Clear":"Stage Clear";
                var clip=Resources.Load<AudioClip>("Audio/"+name);
                string prefix="Finish audio: "+id+" muted="+silent;
                Check(prefix+" clip exists",clip!=null,name);
                if(clip==null)continue;
                Check(prefix+" independent normal-speed channel",courseJingle.clip==clip&&!courseJingle.loop&&courseJingle.pitch==1);
                int pausedFrame=phaseFrames;PauseGame();for(int i=0;i<30;i++)Step(default);
                Check(prefix+" pause freezes settlement",phaseFrames==pausedFrame&&courseJingle.clip==clip);
                ResumeGame();
                int required=Mathf.Max(211,Mathf.CeilToInt(clip.length*(float)ClassicMotor.Frequency));
                int elapsed=0;
                while(elapsed<required-1&&phase==GamePhase.Clear){Step(default);elapsed++;}
                Check(prefix+" waits for complete jingle",phase==GamePhase.Clear&&level.id==id,
                    $"clip={name} seconds={clip.length} required={required} elapsed={elapsed} phase={phase} level={level.id}");
                for(int i=0;i<5&&phase==GamePhase.Clear;i++)Step(default);
                string expected=(id.StartsWith("J-")?"J-":"")+(id.EndsWith("4-4")?"5-1":"1-2");
                bool final=id.EndsWith("8-4");
                Check(prefix+" proceeds after music",final?phase==GamePhase.Victory:phase==GamePhase.Playing&&level.id==expected,phase+" "+level.id);
                Check(prefix+" no old jingle in next state",final?courseJingle.clip!=null&&courseJingle.clip.name=="Ending":courseJingle.clip==null);
                NewGame("1-1");Check(prefix+" new game clears ending and jingle",courseJingle.clip==null&&!courseJingle.isPlaying);
            }
            AudioFinishFixture("1-1");timeLeft=5000;int before=score;
            for(int i=0;i<999;i++)Step(default);
            Check("Finish audio: long score conversion is not truncated",phase==GamePhase.Clear&&timeLeft==5&&score==before+4995*50);
            Step(default);Check("Finish audio: full score awarded before advancing",level.id=="1-2"&&score==before+5000*50);
            AudioFinishFixture("1-1");
            for(int i=0;i<300&&!FinishEntranceReady;i++)Step(default);
            timeLeft=0;phaseFrames=clearAudioFrames+1;finishFireworks=6;finishEmitted=finishClock=0;finishPending=true;
            for(int i=0;i<140;i++)Step(default);
            Check("Finish audio: ready music still waits for fireworks",level.id=="1-1"&&phase==GamePhase.Clear&&finishPending);
            for(int i=0;i<40&&phase==GamePhase.Clear;i++)Step(default);
            Check("Finish audio: final fireworks eventually advance",level.id=="1-2");
            AudioFinishFixture("1-1");BeginClearSequence("Missing audit clip");
            for(int i=0;i<211;i++)Step(default);
            Check("Finish audio: missing clip cannot deadlock settlement",level.id=="1-2");
            }
            finally{muted=savedMute;masterVolume=savedVolume;ApplyVolume();NewGame("1-1");}
        }
        IEnumerator FinishAudioSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            void Check(string n,bool p,string d=""){count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);}
            RunFinishAudioAudit(Check);
            bool savedMute=muted;float savedVolume=masterVolume;
            auditAudioPlayback=true;
            try
            {
                foreach(string id in new[]{"1-1","4-4","J-8-4"})
                {
                    muted=id.StartsWith("J-");ApplyVolume();AudioFinishFixture(id);
                    var clip=courseJingle.clip;double began=AudioSettings.dspTime;bool advancedSamples=false;
                    Check(id+" live jingle starts",clip!=null&&courseJingle.isPlaying);
                    Check(id+" background music stopped during jingle",!music.isPlaying);
                    for(int i=0;i<clearAudioFrames+10;i++)Step(default);
                    Check(id+" fast simulation cannot outrun live music",phase==GamePhase.Clear&&level.id==id&&courseJingle.isPlaying);
                    if(id=="1-1")
                    {
                        PauseGame();yield return new WaitForSecondsRealtime(.12f);
                        int sample=courseJingle.timeSamples;
                        yield return new WaitForSecondsRealtime(.12f);
                        Check("live jingle pauses with settlement",!courseJingle.isPlaying&&courseJingle.timeSamples==sample);
                        ResumeGame();Check("live jingle resumes",courseJingle.isPlaying);
                    }
                    float deadline=Time.realtimeSinceStartup+(clip==null?2:clip.length+3);
                    while(phase==GamePhase.Clear&&Time.realtimeSinceStartup<deadline)
                    {
                        advancedSamples|=courseJingle.timeSamples>clip.samples/3;
                        Step(default);RefreshVisuals();yield return null;
                    }
                    double elapsed=AudioSettings.dspTime-began;
                    Check(id+" real audio samples advance",advancedSamples);
                    Check(id+" transition waits for actual audio duration",elapsed>=clip.length-.05,elapsed.ToString("F3")+"s / "+clip.length);
                    bool final=id.EndsWith("8-4");
                    Check(id+" live finish reaches correct destination",final?phase==GamePhase.Victory:phase==GamePhase.Playing&&level.id==(id=="1-1"?"1-2":"5-1"));
                    Check(id+" old clear clip cannot overlap next audio",courseJingle.clip!=clip&&(!final?!courseJingle.isPlaying:courseJingle.clip.name=="Ending"));
                    if(!final)Check(id+" next background music starts",music.isPlaying);
                    NewGame("1-1");Check(id+" replay stops all fanfares",courseJingle.clip==null&&!courseJingle.isPlaying&&music.isPlaying);
                }
                Sound("Coin");yield return null;
                Check("transient sound starts before cleanup check",sounds.isPlaying);
                ReturnToTitle();Check("return to title stops every audio channel",!sounds.isPlaying&&!music.isPlaying&&!courseJingle.isPlaying);
            }
            finally{auditAudioPlayback=false;muted=savedMute;masterVolume=savedVolume;ApplyVolume();StopCourseAudio();NewGame("1-1");}
            string result=$"MARIO_FINISH_AUDIO_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"finish-audio-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"finish-audio-result.txt"),result);Debug.Log(result);
        }
    }
}
