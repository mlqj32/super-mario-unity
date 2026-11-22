using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunSettingsAudit(Action<string,bool,string> record)
        {
            void Check(string name,bool pass,string detail="")=>record("Settings: "+name,pass,detail);
            string oldKey=settingsKey;float oldVolume=masterVolume;bool oldMute=muted,oldFill=fillScreen,oldFull=desiredFullscreen,oldAuto=autoRun;int oldSize=windowPreset;
            settingsKey="Mario.Tests.Settings.";
            try
            {
                string migrationKey=settingsKey+"AspectAudit.";
                settingsKey=migrationKey;
                PlayerPrefs.DeleteKey(settingsKey+"aspectVersion");
                PlayerPrefs.SetInt(settingsKey+"fill",1);PlayerPrefs.SetFloat(settingsKey+"volume",.45f);
                PlayerPrefs.SetInt(settingsKey+"fullscreen",0);PlayerPrefs.SetInt(settingsKey+"window",2);
                ReadSettings();
                Check("old stretch preference migrates to original ratio",!fillScreen);
                Check("migration preserves volume and window settings",masterVolume==.45f&&!desiredFullscreen&&windowPreset==2);
                fillScreen=true;SaveSettings();ReadSettings();Check("explicit stretch choice survives reload",fillScreen);
                foreach(string key in new[]{"aspectVersion","fill","volume","fullscreen","window","muted","autoRun"})PlayerPrefs.DeleteKey(settingsKey+key);
                ReadSettings();Check("fresh install defaults to original ratio",!fillScreen);
                Check("automatic running defaults off",!autoRun);
                SetAutoRun(true);autoRun=false;ReadSettings();Check("automatic running on survives reload",autoRun);
                SetAutoRun(false);autoRun=true;ReadSettings();Check("automatic running off survives reload",!autoRun);
                foreach(char c in "自动加速开启关闭火")Check("automatic running glyph "+c,PixelLibrary.Glyph(c)!=null);
                foreach(string key in new[]{"volume","muted","fullscreen","window","autoRun"})PlayerPrefs.DeleteKey(settingsKey+key);
                foreach(string key in new[]{"aspectVersion","fill"})PlayerPrefs.DeleteKey(settingsKey+key);
                settingsKey="Mario.Tests.Settings.";
                foreach(var size in windowSizes)
                {
                    Rect view=PresentationRect(size.x,size.y,false);
                    Check("preset fills without distortion "+size,view==new Rect(0,0,size.x,size.y)&&size.x*240==size.y*256);
                }
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2048,1088),new Vector2Int(768,720),new Vector2Int(800,1200)})
                {
                    Rect r=PresentationRect(size.x,size.y,true);
                    Check("fill entire "+size,r==new Rect(0,0,size.x,size.y));
                    r=PresentationRect(size.x,size.y,false);
                    Check("proportional view fits "+size,Mathf.Abs(r.width/r.height-256f/240)<.001f&&r.x>=-.01f&&r.y>=-.01f&&r.xMax<=size.x+.01f&&r.yMax<=size.y+.01f);
                }
                SetVolume(-1);Check("volume clamps at zero",masterVolume==0&&AudioListener.volume==0);
                Check("volume percent glyph exists",PixelLibrary.Glyph('%')!=null);
                SetVolume(2);Check("volume clamps at one",masterVolume==1&&AudioListener.volume==1);
                SetVolume(.35f);ToggleMute();Check("mute preserves level",muted&&masterVolume==.35f&&AudioListener.volume==0);
                ToggleMute();Check("unmute restores level",!muted&&AudioListener.volume==.35f);
                fillScreen=false;desiredFullscreen=false;windowPreset=2;SaveSettings();
                masterVolume=1;fillScreen=desiredFullscreen=true;windowPreset=0;ReadSettings();
                Check("preferences reload",masterVolume==.35f&&!muted&&!fillScreen&&!desiredFullscreen&&windowPreset==2);
                NewGame("1-1");PauseGame();OpenSettings();int before=frame;Step(default);
                Check("settings keep gameplay paused",menuSettings&&phase==GamePhase.Paused&&frame==before);
                CloseSettings();Check("settings return to pause",!menuSettings&&phase==GamePhase.Paused);
                ResumeGame();Check("resume still works",phase==GamePhase.Playing);
                phase=GamePhase.Title;OpenSettings();CloseSettings();Check("settings return to title",phase==GamePhase.Title&&!menuSettings);
            }
            finally
            {
                settingsKey=oldKey;masterVolume=oldVolume;muted=oldMute;fillScreen=oldFill;desiredFullscreen=oldFull;autoRun=oldAuto;windowPreset=oldSize;ApplyVolume();NewGame("1-1");
            }
        }
        IEnumerator WaitForDisplay(bool fullscreen)
        {
            // Resolution changes complete after the current frame.
            yield return new WaitForSecondsRealtime(.5f);
            float deadline=Time.realtimeSinceStartup+8;
            while(Screen.fullScreen!=fullscreen&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;yield return new WaitForEndOfFrame();
        }
        bool RenderFillsScreen()
        {
            var screen=ScreenCapture.CaptureScreenshotAsTexture();var world=PiranhaPixels();bool match=true;
            foreach(var p in new[]{new Vector2Int(0,Screen.height/2),new Vector2Int(Screen.width-1,Screen.height/2),new Vector2Int(Screen.width/2,0),new Vector2Int(Screen.width/2,Screen.height-1)})
            {
                Color32 actual=screen.GetPixel(p.x,p.y);Color32 expected=world[Mathf.Min(239,p.y*240/Screen.height)*256+Mathf.Min(255,p.x*256/Screen.width)];
                match&=Math.Abs(actual.r-expected.r)<=2&&Math.Abs(actual.g-expected.g)<=2&&Math.Abs(actual.b-expected.b)<=2;
            }
            Destroy(screen);return match;
        }
        IEnumerator SettingsSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            void Check(string n,bool p,string d=""){count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);}
            RunSettingsAudit(Check);NewGame("1-1");fillScreen=true;windowPreset=0;SetVolume(.55f);SetDisplayMode(false);
            yield return WaitForDisplay(false);RefreshVisuals();yield return new WaitForEndOfFrame();
            Check("window mode applies",!Screen.fullScreen);Check("window has no black border",RenderFillsScreen());SaveScreen(output,"window-fill");
            PauseGame();OpenSettings();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"settings-window");CloseSettings();ResumeGame();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            WindowsWindowPolicy.EnsureInstalled();Check("native window policy installed",WindowsWindowPolicy.Installed);
            WindowsWindowPolicy.GetWindowRect(WindowsWindowPolicy.Handle,out var rect);
            bool blocked=true;
            foreach(var p in new[]{new Vector2Int(rect.left+1,rect.top+1),new Vector2Int(rect.right-2,rect.top+1),new Vector2Int(rect.left+1,rect.bottom-2),new Vector2Int(rect.right-2,rect.bottom-2),new Vector2Int(rect.left+1,(rect.top+rect.bottom)/2),new Vector2Int(rect.right-2,(rect.top+rect.bottom)/2),new Vector2Int((rect.left+rect.right)/2,rect.top+1),new Vector2Int((rect.left+rect.right)/2,rect.bottom-2)})
            {long hit=WindowsWindowPolicy.SendMessageW(WindowsWindowPolicy.Handle,0x84,IntPtr.Zero,new IntPtr((p.y<<16)|(p.x&0xffff))).ToInt64();blocked&=hit<10||hit>17;}
            Check("all eight resize edges blocked",blocked);
            int width=Screen.width,height=Screen.height;WindowsWindowPolicy.Maximize();yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
            Check("maximize remains available",WindowsWindowPolicy.Maximized&&Screen.width>=width&&Screen.height>=height);
            Check("maximized view fills screen",RenderFillsScreen());SaveScreen(output,"maximized-fill");
            WindowsWindowPolicy.Restore();yield return new WaitForSecondsRealtime(.5f);
            Check("restore returns fixed window",!WindowsWindowPolicy.Maximized&&Screen.width==width&&Screen.height==height);
#endif
            SetDisplayMode(true);yield return WaitForDisplay(true);
            Check("borderless fullscreen applies",Screen.fullScreen&&Screen.fullScreenMode==FullScreenMode.FullScreenWindow);
            Check("fullscreen has no black border",RenderFillsScreen());SaveScreen(output,"fullscreen-fill");
            PauseGame();OpenSettings();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"settings-fullscreen");CloseSettings();ResumeGame();
            SetDisplayMode(false);yield return WaitForDisplay(false);Check("fullscreen returns to window",!Screen.fullScreen);
            fillScreen=false;windowPreset=1;ApplyDisplayMode();yield return WaitForDisplay(false);
            NewGame("1-1");for(int i=0;i<40;i++)Step(default);RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"window-original-ratio");
            Rect view=PresentationRect(Screen.width,Screen.height,fillScreen);
            Check("standard window uses original ratio",Mathf.Abs((float)Screen.width/Screen.height-256f/240)<.003f&&Mathf.Abs(view.width-Screen.width)<1&&Mathf.Abs(view.height-Screen.height)<1);
            Check("new game starts with ten lives",lives==10);
            Die();CompleteDeath();Check("first death leaves nine lives",lives==9);
            SetVolume(.35f);windowPreset=0;SetAutoRun(true);
            string result="MARIO_SETTINGS_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"settings-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"settings-result.txt"),result);Debug.Log(result);
        }
        IEnumerator SettingsReloadSmoke(string output)
        {
            Directory.CreateDirectory(output);validationMode=true;
            bool pass=settingsKey=="Mario.Tests.Settings."&&Mathf.Approximately(masterVolume,.35f)&&!muted&&!fillScreen&&!desiredFullscreen&&windowPreset==0;
            ApplyDisplayMode();yield return WaitForDisplay(false);
            pass&=!Screen.fullScreen&&Mathf.Approximately(AudioListener.volume,.35f);
            OpenSettings();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"settings-after-restart");
            string result="MARIO_SETTINGS_RESTART_"+(pass?"PASS":"FAIL")+" volume="+masterVolume+" fill="+fillScreen+" fullscreen="+Screen.fullScreen;
            File.WriteAllText(Path.Combine(output,"settings-restart-result.txt"),result);Debug.Log(result);
            foreach(string key in new[]{"volume","muted","fill","fullscreen","window","aspectVersion","autoRun"})PlayerPrefs.DeleteKey(settingsKey+key);PlayerPrefs.Save();
            Application.Quit(pass?0:1);
        }
        IEnumerator AutoRunReloadSmoke(string output)
        {
            Directory.CreateDirectory(output);validationMode=true;
            bool saved=autoRun&&settingsKey=="Mario.Tests.Settings.";
            var controls=GameplayControls(new MarioInputFrame{direction=Vector2.right});
            bool pass=saved&&controls.horizontal==1&&controls.run&&!controls.firePressed;
            ReturnToTitle();OpenSettings();usingGamepad=true;RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"auto-run-after-restart");
            string result=$"MARIO_AUTO_RUN_RESTART_{(pass?"PASS":"FAIL")} enabled={autoRun} run={controls.run} fire={controls.firePressed}";
            File.WriteAllText(Path.Combine(output,"auto-run-restart-result.txt"),result);Debug.Log(result);
            PlayerPrefs.DeleteKey(settingsKey+"autoRun");PlayerPrefs.Save();Application.Quit(pass?0:1);
        }
    }
}
