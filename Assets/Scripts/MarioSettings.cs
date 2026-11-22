using System;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        bool menuSettings,fillScreen=false,desiredFullscreen=true,autoRun;
        int windowPreset=1;
        string settingsKey;
        static readonly Vector2Int[] windowSizes={new Vector2Int(512,480),new Vector2Int(768,720),new Vector2Int(1024,960)};
        void InitializeSettings()
        {
            bool testing=Application.isBatchMode||validationMode||Array.Exists(Environment.GetCommandLineArgs(),a=>a.StartsWith("-mario-"));
            settingsKey=testing?"Mario.Tests.Settings.":"Mario.Settings.";
            ReadSettings();
        }
        void ReadSettings()
        {
            // Replace the old stretch default once, including saved installations.
            // Later explicit choices of stretch remain persistent.
            if(PlayerPrefs.GetInt(settingsKey+"aspectVersion",0)<1)
            {
                PlayerPrefs.SetInt(settingsKey+"fill",0);
                PlayerPrefs.SetInt(settingsKey+"aspectVersion",1);PlayerPrefs.Save();
            }
            masterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(settingsKey+"volume",.7f));
            muted=PlayerPrefs.GetInt(settingsKey+"muted",0)!=0;
            fillScreen=PlayerPrefs.GetInt(settingsKey+"fill",0)!=0;
            desiredFullscreen=PlayerPrefs.GetInt(settingsKey+"fullscreen",1)!=0;
            windowPreset=Mathf.Clamp(PlayerPrefs.GetInt(settingsKey+"window",1),0,windowSizes.Length-1);
            autoRun=PlayerPrefs.GetInt(settingsKey+"autoRun",0)!=0;
            ApplyVolume();
        }
        void SaveSettings()
        {
            PlayerPrefs.SetFloat(settingsKey+"volume",masterVolume);PlayerPrefs.SetInt(settingsKey+"muted",muted?1:0);
            PlayerPrefs.SetInt(settingsKey+"fill",fillScreen?1:0);PlayerPrefs.SetInt(settingsKey+"fullscreen",desiredFullscreen?1:0);
            PlayerPrefs.SetInt(settingsKey+"window",windowPreset);PlayerPrefs.SetInt(settingsKey+"autoRun",autoRun?1:0);PlayerPrefs.Save();
        }
        void ApplyVolume(){AudioListener.volume=muted?0:masterVolume;}
        void SetVolume(float volume){masterVolume=Mathf.Clamp01(volume);muted=false;ApplyVolume();SaveSettings();}
        void ToggleMute(){muted=!muted;ApplyVolume();SaveSettings();}
        void SetAutoRun(bool enabled){autoRun=enabled;SaveSettings();}
        static Rect PresentationRect(int width,int height,bool fill)
        {
            if(fill)return new Rect(0,0,width,height);
            float scale=Mathf.Min(width/256f,height/240f);
            return new Rect((width-256*scale)/2,(height-240*scale)/2,256*scale,240*scale);
        }
        void SetDisplayMode(bool fullscreen){desiredFullscreen=fullscreen;SaveSettings();ApplyDisplayMode();}
        void ApplyDisplayMode()
        {
            if(Application.isEditor)return;
            if(desiredFullscreen)
                Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
            else
            {
                WindowsWindowPolicy.Restore();
                Vector2Int size=windowSizes[windowPreset];
                float fit=Mathf.Min(1,Mathf.Min(Display.main.systemWidth*.9f/size.x,Display.main.systemHeight*.85f/size.y));
                Screen.SetResolution(Mathf.RoundToInt(size.x*fit),Mathf.RoundToInt(size.y*fit),FullScreenMode.Windowed);
            }
        }
        void OpenSettings(){menuSettings=true;ClearRestartInput();}
        void CloseSettings(){menuSettings=false;ClearRestartInput();}
        void DrawSettings()
        {
            Fill(new Rect(8,34,240,202),new Color32(12,16,30,250));Text("设置",116,42);
            if(SyobonActive)Text("猫里奥方向键使用原版自动加速",38,55,.65f);
            Text("音量",24,65);Text(Mathf.RoundToInt(masterVolume*100)+"%",184,67);
            Text("画面",24,136);
        }
    }
}
