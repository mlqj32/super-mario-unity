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
        int smokeErrors;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            WindowsWindowPolicy.EnsureInstalled();
            if(Array.IndexOf(args,"-mario-presentation-smoke")>=0)
            {
                validationMode=true;Application.runInBackground=true;Application.logMessageReceived+=SmokeLog;
                int arg=Array.IndexOf(args,"-mario-output");string path=arg>=0&&arg+1<args.Length?args[arg+1]:Path.Combine(Application.persistentDataPath,"Smoke");
                StartCoroutine(PresentationOnlySmoke(path));return;
            }
            if(Array.IndexOf(args,"-mario-auto-run-reload")>=0)
            {
                validationMode=true;Application.runInBackground=true;
                int arg=Array.IndexOf(args,"-mario-output");string path=arg>=0&&arg+1<args.Length?args[arg+1]:Path.Combine(Application.persistentDataPath,"Smoke");
                StartCoroutine(AutoRunReloadSmoke(path));return;
            }
            if(Array.IndexOf(args,"-mario-settings-reload")>=0)
            {
                validationMode=true;Application.runInBackground=true;
                int arg=Array.IndexOf(args,"-mario-output");string path=arg>=0&&arg+1<args.Length?args[arg+1]:Path.Combine(Application.persistentDataPath,"Smoke");
                StartCoroutine(SettingsReloadSmoke(path));return;
            }
            if(Array.IndexOf(args,"-mario-smoke")>=0||Array.IndexOf(args,"-mario-syobon-smoke")>=0||Array.IndexOf(args,"-mario-lakitu-smoke")>=0||Array.IndexOf(args,"-mario-podoboo-smoke")>=0)
            {
                validationMode=true;Application.runInBackground=true;
                Application.logMessageReceived+=SmokeLog;
                string output=Path.Combine(Application.persistentDataPath,"Smoke");
                int i=Array.IndexOf(args,"-mario-output");if(i>=0&&i+1<args.Length)output=args[i+1];
                StartCoroutine(Array.IndexOf(args,"-mario-podoboo-smoke")>=0?PodobooOnlySmoke(output):Array.IndexOf(args,"-mario-lakitu-smoke")>=0?LakituOnlySmoke(output):Array.IndexOf(args,"-mario-syobon-smoke")>=0?SyobonIntegrationSmoke(output):Smoke(output));
            }
            else ApplyDisplayMode();
        }
        void SmokeLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)smokeErrors++;}
        IEnumerator Smoke(string output)
        {
            Directory.CreateDirectory(output);phase=GamePhase.Title;RefreshVisuals();
            SmokeCheck(lives==10,"serialized startup scene begins with ten lives");
            SmokeCheck(responsiveControls&&player.responsive,"startup scene enables responsive controls by default");
            yield return new WaitForSecondsRealtime(1);yield return new WaitForEndOfFrame();SaveScreen(output,"title");
            NewGame("1-1");for(int i=0;i<100;i++)Step(new Controls{horizontal=1,run=true,jump=i>=90,jumpPressed=i==90});
            RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"1-1-running");
            phase=GamePhase.Paused;menuLevels=true;yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"course-select");menuLevels=false;
            foreach(var l in Levels)
            {
                NewGame(l.id);invulnerable=10000;
                for(int i=0;i<720;i++)Step(new Controls{horizontal=1,run=true,jump=i%100<45,jumpPressed=i%100==0});
                RefreshVisuals();yield return null;
            }
            foreach(string id in new[]{"1-2","2-2","4-4","8-4"})
            {NewGame(id);if(id=="1-2"||id=="2-2")LoadLocation(1);for(int i=0;i<30;i++)Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,id);}
            yield return PowerupAnimationSmoke(output);
            yield return CastleAnimationSmoke(output);
            yield return PresentationSmoke(output);
            yield return GameplayAuditSmoke(output);
            yield return CoinChineseSmoke(output);
            yield return LakituSmoke(output);
            yield return PiranhaSmoke(output);
            yield return CannonSmoke(output);
            yield return SettingsSmoke(output);
            yield return EffectsSmoke(output);
            yield return RestartSmoke(output);
            yield return ControlsSmoke(output);
            yield return PlatformSmoke(output);
            yield return SpringSmoke(output);
            yield return MazeSmoke(output);
            yield return MenuSmoke(output);
            yield return FinishAudioSmoke(output);
            yield return CastleEntrySmoke(output);
            yield return CorpseSmoke(output);
            yield return ContactSmoke(output);
            yield return BoundarySmoke(output);
            yield return SyobonSmoke(output);
            string result="MARIO_PLAYER_SMOKE_"+(smokeErrors==0?"PASS":"FAIL")+" courses="+Levels.Length+" simulatedFrames="+(Levels.Length*(720+1200))+" errors="+smokeErrors;
            File.WriteAllText(Path.Combine(output,"player-result.txt"),result);Debug.Log(result);Application.logMessageReceived-=SmokeLog;
            Application.Quit(smokeErrors==0?0:1);
        }
        void SmokeCheck(bool pass,string message)
        {if(!pass)Debug.LogError("MARIO_REGRESSION_FAIL "+message);}
        IEnumerator PowerupAnimationSmoke(string output)
        {
            int previousErrors=smokeErrors;
            NewGame("1-1");player.X=338;player.Y=16;player.grounded=true;
            foreach(var enemy in actors.Where(a=>a.IsEnemy))enemy.alive=false;
            Actor mushroom=null;
            for(int i=0;i<60&&mushroom==null;i++)
            {Step(new Controls{jump=true,jumpPressed=i==0});mushroom=actors.FirstOrDefault(a=>a.data.type=="Mushroom");}
            SmokeCheck(mushroom!=null,"block must spawn mushroom");
            if(mushroom!=null)
            {
                for(int i=0;i<48;i++)Step(default);
                var brick=actors.First(a=>a.data.type=="Brick"&&a.x==352);
                SmokeCheck(mushroom.alive&&mushroom.x>=brick.x&&mushroom.x<brick.x+brick.w,"emerged mushroom walks onto adjacent brick");
                player.X=brick.x+2;player.Y=brick.y-PlayerHeight-.5f;player.vy256=768;player.grounded=false;
                int scoreBefore=score;Step(default);
                SmokeCheck(mushroom.state<900&&mushroom.vy>0&&score==scoreBefore,"brick bump must bounce powerup without enemy kill");
                player.X=260;player.Y=0;player.vy256=0;
                for(int i=0;i<81;i++)
                {
                    Step(default);
                    if(i==0||i==16||i==48||i==80)
                    {
                        RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"mushroom-"+i.ToString("D2"));
                        SmokeCheck(mushroom.alive&&mushroom.state<900,"mushroom survives bump at frame "+i);
                    }
                }
                player.X=mushroom.x;player.Y=mushroom.y;player.vy256=0;Step(default);
                SmokeCheck(power==2&&!mushroom.alive,"surviving mushroom is collectible");
            }
            // Render successive logic frames, including the actual mirrored
            // Goomba cell, rather than only checking catalog key existence.
            NewGame("1-1");foreach(var a in actors)a.alive=false;
            Spawn(new NodeData{type="Floor",group="Solid",x=0,y=0,width=256,height=32});
            var gallery=new List<Actor>();
            string[] types={"Goomba","Koopa","Beetle","Spiny"};
            for(int i=0;i<types.Length;i++)
            {var a=Spawn(new NodeData{type=types[i],group="Character",x=72+i*48,y=types[i]=="Koopa"?24:16,height=types[i]=="Koopa"?24:16});gallery.Add(a);}
            var poses=gallery.Select(a=>new HashSet<string>()).ToArray();
            player.X=8;player.Y=0;
            for(int f=0;f<24;f++)
            {
                Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();
                for(int i=0;i<gallery.Count;i++)
                {
                    var a=gallery[i];var renderer=a.visual.GetComponentInChildren<SpriteRenderer>();
                    SmokeCheck(renderer!=null&&renderer.enabled&&a.visual.gameObject.activeInHierarchy,types[i]+" has visible renderer");
                    poses[i].Add(a.visual.CurrentSignature+"/flip="+a.visual.transform.localScale.x);
                }
                if(f%4==0)SaveScreen(output,"enemy-walk-"+f.ToString("D2"));
            }
            for(int i=0;i<types.Length;i++)SmokeCheck(poses[i].Count>=2,types[i]+" walks through both animation poses");
            string result="MARIO_POWERUP_ANIMATION_"+(smokeErrors==previousErrors?"PASS":"FAIL")+" mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors="+(smokeErrors-previousErrors);
            File.WriteAllText(Path.Combine(output,"powerup-animation-result.txt"),result);Debug.Log(result);
        }
        IEnumerator CastleAnimationSmoke(string output)
        {
            int previousErrors=smokeErrors;
            NewGame("1-4");power=2;player.X=700;player.Y=64;player.grounded=true;player.vx4096=ClassicMotor.RunSpeed;cameraX=588;frame=0;
            var mario=playerVisual.GetComponentInChildren<SpriteRenderer>();
            var runPixels=new HashSet<string>();var flamePixels=new HashSet<string>();Actor flame=null;
            for(int f=0;f<24;f++)
            {
                invulnerable=1000;Step(new Controls{horizontal=1,run=true});invulnerable=0;RefreshVisuals();
                yield return null;yield return new WaitForEndOfFrame();
                runPixels.Add(SmokeRenderedCell(mario));
                flame=flame??actors.FirstOrDefault(a=>a.data.type=="CastleFireball"&&a.originX==792&&a.state==2);
                if(flame!=null)flamePixels.Add(SmokeRenderedCell(flame.visual.GetComponentInChildren<SpriteRenderer>()));
                if(f%3==0)SaveScreen(output,"castle-run-"+f.ToString("D2"));
            }
            SmokeCheck(runPixels.Count==3,"castle Mario must display three running pixel frames");
            SmokeCheck(flamePixels.Count==4,"castle firebar must display four flame pixel frames");
            player.X=700;player.Y=64;player.vx4096=0;player.grounded=true;cameraX=588;Step(new Controls{down=true});RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"castle-crouching");
            float crouchHeight=mario.bounds.size.y;
            SmokeCheck(Mathf.Abs(crouchHeight-22)<.01f,"crouching Mario displays complete 22-pixel artwork");SmokeRenderedCell(mario);
            player.crouched=false;player.vx4096=0;star=600;
            var palettes=new HashSet<string>();
            for(int i=0;i<4;i++)
            {
                frame=i*4;RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();
                palettes.Add(SmokeRenderedCell(mario));SaveScreen(output,"castle-star-"+i);
            }
            SmokeCheck(palettes.Count==4,"invincible Mario displays four palettes");
            string result="MARIO_CASTLE_ANIMATION_"+(smokeErrors==previousErrors?"PASS":"FAIL")+" renderedFrames=24 runCells="+runPixels.Count+" flameCells="+flamePixels.Count+" starPalettes="+palettes.Count+" crouchHeight="+crouchHeight+" errors="+(smokeErrors-previousErrors);
            File.WriteAllText(Path.Combine(output,"castle-animation-result.txt"),result);Debug.Log(result);
        }
        string SmokeRenderedCell(SpriteRenderer r)
        {
            var old=RenderTexture.active;RenderTexture.active=target;
            var p=gameCamera.WorldToScreenPoint(r.bounds.min);int w=Mathf.RoundToInt(r.bounds.size.x),h=Mathf.RoundToInt(r.bounds.size.y);
            var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(Mathf.Round(p.x),Mathf.Round(p.y),w,h),0,0);t.Apply();
            var actual=t.GetPixels32();var source=r.sprite.texture.GetPixels32();uint hash=2166136261;int mismatches=0;
            for(int i=0;i<actual.Length;i++)
            {
                int x=i%w,y=i/w;var expected=source[y*w+(r.transform.lossyScale.x<0?w-1-x:x)];
                var c=actual[i];if(expected.a==0)c=new Color32(0,0,0,0);
                else if(c.r!=expected.r||c.g!=expected.g||c.b!=expected.b)mismatches++;
                hash=(hash^c.r)*16777619;hash=(hash^c.g)*16777619;hash=(hash^c.b)*16777619;
            }
            Destroy(t);RenderTexture.active=old;SmokeCheck(mismatches==0,"rendered cell "+r.sprite.name+" frame="+frame+" mismatches="+mismatches);return hash.ToString("X8");
        }
        void SaveScreen(string output,string name)
        {var image=ScreenCapture.CaptureScreenshotAsTexture();var pixels=image.GetPixels32();bool visible=false;for(int i=0;i<pixels.Length;i+=97)if(pixels[i].r+pixels[i].g+pixels[i].b>24){visible=true;break;}if(!visible)Debug.LogError("Smoke screenshot is blank: "+name);File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
    }
}
