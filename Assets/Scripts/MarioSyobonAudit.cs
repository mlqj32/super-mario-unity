using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        IEnumerator SyobonIntegrationSmoke(string output)
        {
            Directory.CreateDirectory(output);
            yield return MenuSmoke(output);yield return SyobonSmoke(output);
            string result=$"SYOBON_INTEGRATION_{(smokeErrors==0?"PASS":"FAIL")} errors={smokeErrors}";
            File.WriteAllText(Path.Combine(output,"integration-result.txt"),result);Debug.Log(result);
            Application.logMessageReceived-=SmokeLog;Application.Quit(smokeErrors==0?0:1);
        }
        public void RunSyobonAudit(System.Action<string,bool,string> record,string output)
        {
            var root=new GameObject("Syobon audit");
            try{using(var cat=new Syobon.Engine(root))cat.RunAudit(record,output);}
            finally{DestroyImmediate(root);}
        }
        IEnumerator SyobonSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            void Check(string n,bool p,string d=""){count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);}
            RunSyobonAudit(Check,Path.Combine(output,"Syobon"));
            NewGame("1-1");ReturnToTitle();menuCampaign=2;usingGamepad=true;
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"syobon-title");
            PauseGame(true);yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"syobon-courses");
            for(int stage=1;stage<=4;stage++)
            {
                StartSyobon(stage);for(int i=0;i<8;i++)StepSyobon(default);
                syobon.Render();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"syobon-stage-"+stage);
            }
            StartSyobon(2);
            Check("Cat Mario live sign waits with dialog",syobon.OpenStageTwoHintForAudit());
            syobon.Render();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"syobon-dialog");
            StepSyobon(default,true);for(int i=0;i<18;i++)StepSyobon(default);
            Check("Cat Mario live dialog accepts confirmation",!syobon.Waiting);
            syobon.SetAudio(true);RetrySyobon(false);
            float deadline=Time.realtimeSinceStartup+3;
            while(syobon.MusicSamples==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Check("Cat Mario live music starts",syobon.AudioPlaying&&syobon.MusicSamples>0,"samples="+syobon.MusicSamples);
            PauseGame();yield return new WaitForSecondsRealtime(.1f);int samples=syobon.MusicSamples;
            yield return new WaitForSecondsRealtime(.15f);
            Check("Cat Mario pause freezes audio",!syobon.AudioPlaying&&syobon.MusicSamples==samples);
            ResumeGame();yield return new WaitForSecondsRealtime(.15f);
            Check("Cat Mario resume advances audio",syobon.AudioPlaying&&syobon.MusicSamples>samples);
            PauseGame();OpenSettings();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"syobon-settings");
            var catAudio=gameObject.GetComponents<AudioSource>();
            NewGame("1-1");
            Check("Cat Mario audio channels released when switching campaign",gameObject.GetComponents<AudioSource>().Length==catAudio.Length-2);
            string result=$"SYOBON_PLAYER_{(before==smokeErrors?"PASS":"FAIL")} stages=4 areas=8 checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"syobon-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"syobon-result.txt"),result);Debug.Log(result);
        }
    }
}
