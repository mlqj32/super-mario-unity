using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioValidation
{
    [Serializable] public class CheckResult { public string name; public bool pass; public string detail; }
    [Serializable] public class Report { public bool pass; public string unity; public int levels,areas,nodes; public List<CheckResult> checks=new List<CheckResult>(); }
    static Report report;
    static void Check(string name,bool pass,string detail="")
    { report.checks.Add(new CheckResult{name=name,pass=pass,detail=detail});if(!pass)Debug.LogError("MARIO_CHECK_FAIL "+name+" "+detail); }
    [MenuItem("Mario/Validate project and gameplay")]
    public static void Run()
    {
        report=new Report{unity=Application.unityVersion};Directory.CreateDirectory("Validation/Screenshots");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        var originalLevels=game.Levels.ToDictionary(l=>l.id,l=>JsonUtility.ToJson(l));
        try
        {
            Check("Initial session starts with ten lives",game.lives==10);
            report.levels=game.Levels.Length;Check("64 unique courses",game.Levels.Length==64&&game.Levels.Select(l=>l.id).Distinct().Count()==64);
            var absent=new HashSet<string>();
            foreach(var level in game.Levels)
            {
                int w=level.World,s=level.Stage;Check("Course ID "+level.id,w>=1&&w<=8&&s>=1&&s<=4);
                for(int i=0;i<level.locations.Length;i++)Check(level.id+" location "+i,level.locations[i].area>=0&&level.locations[i].area<level.areas.Length);
                foreach(var a in level.areas)
                {
                    report.areas++;
                    var all=a.nodes.Concat(a.sections.SelectMany(s=>s.parts.SelectMany(p=>p.nodes)));
                    foreach(var n in all)
                    {
                        report.nodes++;
                        if(n.targetLocation>=0)Check(level.id+" pipe/vine target",n.targetLocation<level.locations.Length,n.type+" -> "+n.targetLocation);
                        if(!string.IsNullOrEmpty(n.targetMap))Check(level.id+" world transition",game.Levels.Any(l=>l.id==n.targetMap),n.targetMap);
                        if(n.type=="Section")Check(level.id+" section target",n.section<a.sections.Length);
                        if(n.type=="SectionDecider")Check(level.id+" maze branches",n.pass<a.sections.Length&&n.fail<a.sections.Length);
                        if(!string.IsNullOrEmpty(n.spriteOverride)){if(Resources.Load<Texture2D>(n.spriteOverride)==null)absent.Add(n.spriteOverride);}
                        else if(n.group=="Character"||n.group=="Scenery"||n.group=="Solid")
                        {
                            if(!n.type.StartsWith("Scroll")&&!n.type.StartsWith("Detect")&&PixelLibrary.Find(n.group,n.type,a.setting+" one")==null)absent.Add(n.group+" "+n.type);
                        }
                    }
                    if(a.exit>=0)Check(level.id+" sky exit",a.exit<level.locations.Length);
                }
                game.LoadLevel(level.id);
                for(int f=0;f<120;f++)game.Step(default);
                Check(level.id+" boot and simulate",game.Error==null&&game.phase==GamePhase.Playing,game.phase+" at "+game.player.X+","+game.player.Y);
                game.RefreshVisuals();Capture(game,"Validation/Screenshots/"+level.id+".png");
                for(int i=0;i<level.locations.Length;i++)
                {game.LoadLocation(i);for(int f=0;f<12;f++)game.Step(default);Check(level.id+" load location "+i,game.area!=null&&game.actors.Count>0);}
            }
            Check("All visible node types have sprites",absent.Count==0,string.Join(", ",absent));
            MovementChecks();AnimationChecks(game);ReferenceMotionChecks();InteractionChecks(game);PowerupRegressionChecks(game);RenderedAnimationChecks(game);
            game.RunGameplayAudit(Check);
            game.RunCoinChineseAudit(Check);
            game.RunLakituAudit(Check);
            game.RunPiranhaAudit(Check);
            game.RunCannonAudit(Check);
            game.RunSettingsAudit(Check);
            game.RunEffectsAudit(Check);
            game.RunRestartAudit(Check);
            game.RunControlsAudit(Check);
            game.RunPlatformAudit(Check);
            game.RunSpringAudit(Check);
            game.RunMazeAudit(Check);
            game.RunMenuAudit(Check);
            game.RunFinishAudioAudit(Check);
            game.RunCastleEntryAudit(Check);
            game.RunCorpseAudit(Check);
            game.RunContactAudit(Check);
            game.RunBoundaryAudit(Check);
            game.RunPresentationAudit(Check);
            game.RunStabilityAudit(Check);
            game.RunSyobonAudit(Check,"Validation/Syobon");
            LevelEditorChecks();
            foreach(char c in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-!./:+%")Check("HUD glyph "+c,PixelLibrary.Glyph(c)!=null);
            foreach(var l in game.Levels)
            {
                game.NewGame(l.id);game.invulnerable=10000;
                for(int i=0;i<720;i++)game.Step(new Controls{horizontal=1,run=true,jump=i%100<45,jumpPressed=i%100==0});
                Check(l.id+" 720-frame runtime stress",game.Error==null);
            }
            foreach(var l in game.Levels)Check(l.id+" runtime audits preserve editable data",originalLevels[l.id]==JsonUtility.ToJson(l));
            report.pass=report.checks.All(c=>c.pass);
        }
        finally
        {
            File.WriteAllText("Validation/results.json",JsonUtility.ToJson(report,true));
            UnityEngine.Object.DestroyImmediate(root);
        }
        string summary="MARIO_VALIDATION_"+(report.pass?"PASS":"FAIL")+" levels="+report.levels+" areas="+report.areas+" nodes="+report.nodes+" checks="+report.checks.Count+" failed="+report.checks.Count(c=>!c.pass);
        File.WriteAllText("Validation/summary.txt",summary);Debug.Log(summary);
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        if(!report.pass)throw new Exception(summary);
    }
    static void LevelEditorChecks()
    {
        var window=ScriptableObject.CreateInstance<MarioLevelEditor>();
        var a=ScriptableObject.CreateInstance<LevelData>();var b=ScriptableObject.CreateInstance<LevelData>();
        a.id="fixture A";b.id="fixture B";
        AreaData Area(string type)=>new AreaData{setting="Overworld",nodes=new[]{new NodeData{type=type,group="Solid",x=32,y=32}},sections=Array.Empty<SectionData>()};
        a.areas=new[]{Area("Brick"),Area("Stone")};b.areas=new[]{Area("Block")};
        const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(MarioLevelEditor);
        void Set(string field,object value)=>type.GetField(field,flags).SetValue(window,value);
        void Call(string method)=>type.GetMethod(method,flags).Invoke(window,null);
        try
        {
            Set("selected",a);Set("areaIndex",0);Call("Preview");Set("selected",b);Call("Apply");
            Check("Editor: old preview cannot overwrite different course",b.areas[0].nodes[0].type=="Block");
            Set("selected",a);Set("areaIndex",1);Call("Apply");
            Check("Editor: old preview cannot overwrite different area",a.areas[1].nodes[0].type=="Stone");
            type.GetMethod("OnPlayMode",flags).Invoke(window,new object[]{PlayModeStateChange.ExitingEditMode});
            Check("Editor: preview removed before Play",type.GetField("previewRoot",flags).GetValue(window)==null);
            a.areas[0].nodes=new[]{
                new NodeData{type="CustomText",group="Text",x=32,y=64,width=24,height=8,text="100"},
                new NodeData{type="Stone",group="Solid",x=96,y=64,width=48,height=32,spriteOverride="JapanTiles/3/c5"}};
            Set("selected",a);Set("areaIndex",0);Call("Preview");
            var preview=(GameObject)type.GetField("previewRoot",flags).GetValue(window);
            var handles=preview.GetComponentsInChildren<LevelNodeHandle>();
            Check("Editor: custom text is visible in preview",handles[0].GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled&&r.sprite!=null)==3);
            var tile=handles[1].GetComponentInChildren<SpriteRenderer>();
            Check("Editor: reference tile preview honors edited dimensions",tile!=null&&tile.size==new Vector2(48,32));
        }
        finally{UnityEngine.Object.DestroyImmediate(window);UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
    }
    static void Capture(MarioGame g,string path)
    {
        g.gameCamera.Render();var old=RenderTexture.active;RenderTexture.active=g.gameCamera.targetTexture;
        var t=new Texture2D(256,240,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,256,240),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;
    }
    static void MovementChecks()
    {
        var walk=new ClassicMotor{grounded=true};var run=new ClassicMotor{grounded=true};
        for(int i=0;i<180;i++){walk.Step(new Controls{horizontal=1});run.Step(new Controls{horizontal=1,run=true});}
        Check("Walk cap 1.5 px/frame",Math.Abs(walk.VX-1.5f)<.00001,walk.VX.ToString());
        Check("Run cap 2.5 px/frame",Math.Abs(run.VX-2.5f)<.00001,run.VX.ToString());
        float start=run.X;int brake=0;while(run.VX!=0&&brake<180){run.Step(default);brake++;}
        Check("Ground braking preserves inertia",run.X>start+10&&brake>10&&brake<100,"frames="+brake+" distance="+(run.X-start));
        var air=new ClassicMotor{vx4096=ClassicMotor.RunSpeed};for(int i=0;i<20;i++)air.Step(default);
        Check("No artificial air drag",air.vx4096==ClassicMotor.RunSpeed);
        float JumpHeight(int hold){var m=new ClassicMotor{grounded=true};float max=0;for(int i=0;i<90;i++){m.Step(new Controls{jump=i<hold,jumpPressed=i==0});max=Math.Max(max,m.Y);}return max;}
        float shortJump=JumpHeight(1),fullJump=JumpHeight(90);
        Check("Variable-height jump",fullJump>shortJump+20&&fullJump<90,"tap="+shortJump+" hold="+fullJump);
        var a=new ClassicMotor{grounded=true};var b=new ClassicMotor{grounded=true};
        for(int i=0;i<300;i++){var c=new Controls{horizontal=i<150?1:-1,run=true,jump=i%90<30,jumpPressed=i%90==0};a.Step(c);b.Step(c);}
        Check("Deterministic integer replay",a.x4096==b.x4096&&a.y256==b.y256&&a.vx4096==b.vx4096);
        var water=new ClassicMotor{swimming=true};water.Step(new Controls{jump=true,jumpPressed=true});Check("Swimming stroke",water.VY>0&&water.VY<2);
    }
    static void AnimationChecks(MarioGame game)
    {
        string Key(string group,string type,string state)
        {var e=PixelLibrary.Find(group,type,state);return e==null?"<missing>":e.key;}
        Check("Four-frame fireball animation",Key("Character","Fireball","Overworld normal")=="Character Fireball normal"&&Key("Character","Fireball","Overworld two")=="Character Fireball two"&&Key("Character","Fireball","Overworld three")=="Character Fireball three"&&Key("Character","Fireball","Overworld four")=="Character Fireball four");
        Check("Four-frame star animation",Key("Character","Star","Overworld normal")=="Character Star normal"&&Key("Character","Star","Overworld two")=="Character Star two"&&Key("Character","Star","Overworld three")=="Character Star three"&&Key("Character","Star","Overworld four")=="Character Star four");
        Check("Animated coin cells",Key("Character","Coin","Overworld anim normal")=="Character Coin anim normal"&&Key("Character","Coin","Overworld anim anim2")=="Character Coin anim anim2"&&Key("Character","Coin","Overworld anim anim3")=="Character Coin anim anim3"&&Key("Character","Coin","Overworld anim anim4")=="Character Coin anim anim4");
        Check("Star Mario first cell",Key("Character","Player","Overworld normal star").StartsWith("Character Player star "));
        Check("Fire Mario fire pose",Key("Character","Player","Overworld fiery firing").EndsWith("fiery normal firing"));
        Check("Blooper squeeze pose",Key("Character","Blooper","Underwater squeeze")=="Character Blooper Underwater squeeze");
        game.NewGame("1-1");
        var running=new HashSet<string>();
        for(int i=0;i<36;i++){game.Step(new Controls{horizontal=1,run=true});game.RefreshVisuals();running.Add(game.PlayerVisualSignature);}
        Check("Player running animation cycles",running.Count>=3,string.Join(",",running));
        game.power=3;game.Step(new Controls{firePressed=true});game.RefreshVisuals();
        Check("Player fire pose is visible",game.PlayerVisualSignature.Contains("firing"),game.PlayerVisualSignature);
        game.NewGame("1-1");
        game.player.X=260;game.cameraX=148;
        var goomba=game.actors.FirstOrDefault(a=>a.alive&&a.data.type=="Goomba");
        var goombaFrames=new HashSet<string>();
        if(goomba!=null)
        {
            game.invulnerable=1000;
            for(int i=0;i<36;i++){game.Step(default);game.RefreshVisuals();goombaFrames.Add(goomba.visual.CurrentSignature+"/flip="+goomba.visual.transform.localScale.x);}
        }
        Check("Goomba runtime animation cycles",goomba!=null&&goombaFrames.Count>=2,goomba==null?"missing":$"frames={goombaFrames.Count} signature={goomba.visual.CurrentSignature} type={goomba.data.type} active={goomba.active} x={goomba.x} camera={game.cameraX}");
    }
    static void PowerupRegressionChecks(MarioGame g)
    {
        g.NewGame("1-1");g.player.X=338;g.player.Y=16;g.player.grounded=true;
        Actor item=null;
        for(int i=0;i<60&&item==null;i++)
        {g.Step(new Controls{jump=true,jumpPressed=i==0});item=g.actors.FirstOrDefault(a=>a.data.type=="Mushroom");}
        Check("Regression: mushroom emerges from real 1-1 block",item!=null);
        if(item!=null)
        {
            for(int i=0;i<32;i++)g.Step(default);
            var brick=g.actors.First(a=>a.data.type=="Brick"&&a.x==352);
            item.x=brick.x+2;item.y=brick.Top;item.vx=1;item.vy=0;
            g.player.X=brick.x+2;g.player.Y=brick.y-g.PlayerHeight-.5f;g.player.vy256=768;g.player.grounded=false;
            int before=g.score;g.Step(default);
            Check("Regression: bumping brick bounces mushroom instead of killing it",item.alive&&item.state<900&&item.vy>0,$"alive={item.alive} state={item.state} vy={item.vy}");
            Check("Regression: mushroom bump gives no enemy kill score",g.score==before,$"before={before} after={g.score}");
            g.player.X=260;g.player.Y=0;g.player.vy256=0;
            for(int i=0;i<60;i++)g.Step(default);
            Check("Regression: bumped mushroom survives old 40-frame death timeout",item.alive&&item.state<900,$"alive={item.alive} x={item.x} y={item.y}");
            g.player.X=item.x;g.player.Y=item.y;g.player.vy256=0;g.Step(default);
            Check("Regression: bumped mushroom remains collectible",g.power==2&&!item.alive);
        }
        foreach(string type in new[]{"Mushroom","Mushroom1Up","Star","FireFlower"})
        foreach(string attack in new[]{"Fireball","Shell"})
        {
            g.NewGame("1-1");
            var powerup=g.Spawn(new NodeData{type=type,group="Character",x=160,y=32});powerup.active=true;
            var projectile=g.Spawn(new NodeData{type=attack,group="Character",x=156,y=32});projectile.vx=4;projectile.active=true;
            g.Step(default);
            Check($"Regression: {attack} does not kill {type}",powerup.alive&&powerup.state<900,$"state={powerup.state}");
            Check($"Regression: {attack} passes through {type}",projectile.alive&&projectile.vx>0);
        }
        foreach(string attack in new[]{"Fireball","Shell"})
        {
            g.NewGame("1-1");
            var enemy=g.Spawn(new NodeData{type="Goomba",group="Character",x=160,y=32});enemy.active=true;
            var projectile=g.Spawn(new NodeData{type=attack,group="Character",x=156,y=32});projectile.vx=4;projectile.active=true;
            g.Step(default);
            Check("Regression: "+attack+" still defeats enemies",enemy.state>=900);
        }
        foreach(string type in new[]{"Koopa","Beetle","Spiny","Piranha"})
        {
            g.NewGame("1-1");
            var enemy=g.Spawn(new NodeData{type=type,group="Character",x=160,y=48});enemy.active=true;
            var poses=new HashSet<string>();
            for(int i=0;i<24;i++){g.Step(default);g.RefreshVisuals();poses.Add(enemy.visual.CurrentSignature);}
            Check("Regression: "+type+" changes rendered sprite",poses.Count>=2,string.Join(",",poses));
            string pausedPose=enemy.visual.CurrentSignature;int pausedAge=enemy.age;
            g.phase=GamePhase.Paused;
            for(int i=0;i<24;i++){g.Step(default);g.RefreshVisuals();}
            Check("Regression: "+type+" animation freezes when paused",enemy.age==pausedAge&&enemy.visual.CurrentSignature==pausedPose);
        }
    }
    static string RenderedPixels(MarioGame g, SpriteRenderer r)
    {
        g.gameCamera.Render();var old=RenderTexture.active;RenderTexture.active=g.gameCamera.targetTexture;
        var p=g.gameCamera.WorldToScreenPoint(r.bounds.min);
        int w=Mathf.RoundToInt(r.bounds.size.x),h=Mathf.RoundToInt(r.bounds.size.y);
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false);
        t.ReadPixels(new Rect(Mathf.Round(p.x),Mathf.Round(p.y),w,h),0,0);t.Apply();
        var actual=t.GetPixels32();var expected=r.sprite.texture.GetPixels32();int mismatches=0;
        // Transparent pixels show the moving background; compare all painted
        // pixels to the selected cell before hashing the normalized capture.
        uint hash=2166136261;
        for(int i=0;i<actual.Length;i++)
        {
            int x=i%w,y=i/w;var source=expected[y*w+(r.transform.lossyScale.x<0?w-1-x:x)];
            var c=actual[i];if(source.a==0)c=new Color32(0,0,0,0);
            else if(c.r!=source.r||c.g!=source.g||c.b!=source.b)mismatches++;
            hash=(hash^c.r)*16777619;hash=(hash^c.g)*16777619;hash=(hash^c.b)*16777619;
        }
        if(mismatches>0)Check("Rendered pixel fidelity "+r.sprite.name+" frame "+g.frame,false,"mismatches="+mismatches+" bounds="+r.bounds+" screen="+p);
        UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;return hash.ToString("X8");
    }
    static void RenderedAnimationChecks(MarioGame g)
    {
        g.NewGame("1-4");g.player.X=700;g.player.Y=64;g.player.grounded=true;g.cameraX=588;
        // This fixture now advances real logic to animate Mario. Isolate the
        // sprite checks from castle enemies, damage flashes and foreground art.
        foreach(var actor in g.actors)actor.alive=false;
        g.power=2;g.player.crouched=true;g.RefreshVisuals();
        var r=g.GetComponentsInChildren<PixelVisual>(true).First(v=>v.name.StartsWith("Mario /")).GetComponentInChildren<SpriteRenderer>(true);
        Check("Rendered: crouching Mario keeps all 22 sprite rows",Mathf.Abs(r.bounds.size.y-22)<.01f,"renderedHeight="+r.bounds.size.y);
        Capture(g,"Validation/Screenshots/castle-crouching.png");
        foreach(int power in new[]{1,2,3})
        {
            g.power=power;g.player.crouched=false;g.player.vx4096=ClassicMotor.RunSpeed;
            var cells=new List<string>();var pixels=new HashSet<string>();
            for(int f=0;f<9;f++){g.Step(new Controls{horizontal=1,run=true});g.player.Y=64;g.player.grounded=true;g.RefreshVisuals();cells.Add(r.sprite.name);pixels.Add(RenderedPixels(g,r));}
            Check("Rendered: power "+power+" run has three equal 3-frame cells",cells.Distinct().Count()==3&&cells.GroupBy(s=>s).All(s=>s.Count()==3),string.Join(",",cells));
            Check("Rendered: power "+power+" run changes actual pixels",pixels.Count==3,"pixelFrames="+pixels.Count);
        }
        g.player.vx4096=0;g.power=2;g.star=600;
        var starPixels=new HashSet<string>();for(int f=0;f<16;f++){g.frame=f;g.RefreshVisuals();starPixels.Add(RenderedPixels(g,r));}
        Check("Rendered: invincible Mario cycles four palettes",starPixels.Count==4,"pixelFrames="+starPixels.Count);
        g.star=0;g.phase=GamePhase.Flag;
        var climbPixels=new HashSet<string>();for(int f=0;f<16;f++){g.frame=f;g.RefreshVisuals();climbPixels.Add(RenderedPixels(g,r));}
        Check("Rendered: climbing alternates two poses",climbPixels.Count==2,"pixelFrames="+climbPixels.Count);
        g.NewGame("2-2");g.LoadLocation(1);g.player.X=100;g.player.Y=80;g.cameraX=0;
        var swimPixels=new HashSet<string>();for(int f=0;f<32;f++){g.frame=f;g.RefreshVisuals();swimPixels.Add(RenderedPixels(g,r));}
        Check("Rendered: swimming includes all four stroke poses",swimPixels.Count>=4,"pixelFrames="+swimPixels.Count);
        g.NewGame("1-4");g.player.X=562;g.player.Y=64;g.cameraX=450;g.invulnerable=1000;g.frame=0;
        Actor flame=null;var flamePixels=new HashSet<string>();var positions=new HashSet<Vector2>();var flameDetails=new List<string>();
        for(int f=0;f<24;f++)
        {
            g.Step(default);g.RefreshVisuals();
            flame=flame??g.actors.FirstOrDefault(a=>a.data.type=="CastleFireball"&&a.state==2);
            if(flame==null)continue;
            var flameRenderer=flame.visual.GetComponentInChildren<SpriteRenderer>();
            string hash=RenderedPixels(g,flameRenderer);
            flamePixels.Add(hash);positions.Add(new Vector2(flame.x,flame.y));flameDetails.Add(f+":"+flameRenderer.sprite.name+":"+hash);
        }
        Check("Rendered: 1-4 firebar uses four distinct pixel frames",flamePixels.Count==4,"pixelFrames="+flamePixels.Count+" "+string.Join(",",flameDetails));
        Check("Rendered: 1-4 firebar rotates",positions.Count==24,"positions="+positions.Count);
        if(flame!=null)
        {
            g.phase=GamePhase.Paused;int age=flame.age;var pos=new Vector2(flame.x,flame.y);string pose=flame.visual.CurrentSignature;
            for(int i=0;i<24;i++){g.Step(default);g.RefreshVisuals();}
            Check("Rendered: firebar pauses without advancing",age==flame.age&&pos==new Vector2(flame.x,flame.y)&&pose==flame.visual.CurrentSignature);
            g.phase=GamePhase.Playing;g.Step(default);g.RefreshVisuals();
            Check("Rendered: firebar resumes",flame.age==age+1&&pos!=new Vector2(flame.x,flame.y));
        }
    }
    static void InteractionChecks(MarioGame g)
    {
        g.NewGame("1-1");g.validationMode=true;
        for(int i=0;i<12;i++)g.Step(default);
        Check("Spawn lands on ground",g.player.grounded&&Mathf.Abs(g.player.Y)<.01f);
        // Initial question block is at x=256, top=64. Strike its underside.
        g.player.X=258;g.player.Y=16;g.player.grounded=true;g.player.vx4096=0;
        for(int i=0;i<30;i++)g.Step(new Controls{jump=true,jumpPressed=i==0});
        Check("Question block awards coin",g.coins==1,"coins="+g.coins);
        g.NewGame("1-1");g.invulnerable=1000;g.player.X=338;g.player.Y=16;g.player.grounded=true;
        for(int i=0;i<60;i++)g.Step(new Controls{jump=true,jumpPressed=i==0});
        var mushroom=g.actors.FirstOrDefault(a=>a.alive&&a.data.type=="Mushroom");
        Check("Powerup block spawns mushroom without iterator invalidation",mushroom!=null);
        if(mushroom!=null)
        {
            float mushroomStart=mushroom.x;
            g.player.X=mushroom.x;g.player.Y=mushroom.y;g.Step(default);Check("Mushroom increases player size",g.power==2);
            g.NewGame("1-1");g.player.X=338;g.player.Y=16;g.player.grounded=true;g.invulnerable=1000;
            Actor walkingMushroom=null;
            for(int i=0;i<60&&walkingMushroom==null;i++)
            {g.Step(new Controls{jump=true,jumpPressed=i==0});walkingMushroom=g.actors.FirstOrDefault(a=>a.alive&&a.data.type=="Mushroom");}
            if(walkingMushroom!=null) { mushroomStart=walkingMushroom.x;g.player.X=0;for(int i=0;i<72;i++)g.Step(default); }
            Check("Uncollected mushroom walks away",walkingMushroom!=null&&walkingMushroom.alive&&walkingMushroom.x>mushroomStart+8,walkingMushroom==null?"missing":$"start={mushroomStart} x={walkingMushroom.x}");
        }
        g.NewGame("1-1");g.player.X=1026;g.player.Y=32;g.player.grounded=true;g.invulnerable=1000;
        for(int i=0;i<45;i++)g.Step(new Controls{jump=true,jumpPressed=i==0});
        Check("Hidden 1UP is revealed by hitting from below",g.actors.Any(a=>a.data.type=="Block"&&Mathf.Abs(a.x-1024)<1&&!a.data.hidden&&a.used));
        g.NewGame("1-1");g.player.X=1506;g.player.Y=0;g.player.grounded=true;g.invulnerable=1000;
        for(int i=0;i<100;i++)g.Step(new Controls{jump=i%45<20,jumpPressed=i%45==0});
        Check("Multi-coin brick pays out repeatedly",g.coins>=2,"coins="+g.coins);
        g.LoadLevel("1-1");g.player.X=430;g.player.Y=0;g.player.grounded=true;g.invulnerable=1000;
        for(int i=0;i<60;i++)g.Step(new Controls{horizontal=1,run=true});
        Check("Solid pipe blocks horizontal movement",g.player.X<=436.01f,"x="+g.player.X);
        g.LoadLevel("1-1");var pipe=g.actors.First(a=>a.data.targetLocation==2&&a.data.type=="Pipe");g.player.X=pipe.x+10;g.player.Y=pipe.Top;g.player.grounded=true;
        for(int i=0;i<45;i++)g.Step(new Controls{down=true});
        Check("Down enters underground bonus room",g.location==2&&g.area.setting.Contains("Underworld"),"location="+g.location);
        g.LoadLevel("2-1",3);g.player.Y=-70;g.Step(default);Check("Sky fall returns through defined exit",g.location==1,"location="+g.location);
        g.NewGame("1-1");g.power=3;g.Hurt();Check("Fire Mario damage returns to small",g.power==1&&g.invulnerable>0);g.invulnerable=0;g.Hurt();Check("Small Mario damage enters death",g.phase==GamePhase.Dying);
        g.NewGame("1-1");var flag=g.actors.First(a=>a.data.type=="FlagPole");g.player.X=flag.x-8;g.player.Y=64;g.Step(default);Check("Flagpole starts finish sequence",g.phase==GamePhase.Flag);
        for(int i=0;i<1200&&g.level.id=="1-1";i++)g.Step(default);Check("Course completion advances to 1-2",g.level.id=="1-2",g.level.id);
        g.NewGame("8-4");g.LoadLocation(5);var axe=g.actors.First(a=>a.data.type=="CastleAxe");g.player.X=axe.x;g.player.Y=axe.y;g.Step(default);Check("Final axe opens bridge",g.phase==GamePhase.Castle);
        for(int i=0;i<1200&&g.phase!=GamePhase.Victory;i++)g.Step(default);Check("8-4 reaches victory",g.phase==GamePhase.Victory,g.phase.ToString());
        g.LoadLevel("4-4");for(int i=0;i<2;i++)g.Step(default);Check("4-4 maze section loads",g.actors.Count>g.area.nodes.Length);
        g.LoadLevel("7-4");g.cameraX=300;g.player.X=400;g.Step(default);Check("7-4 maze section loads",g.actors.Count>g.area.nodes.Length);
    }
    static void ReferenceMotionChecks()
    {
        var lines=File.ReadAllLines("Validation/physics-reference.csv").Skip(1).ToArray();
        var names=new[]{"walk right","run right","release and brake","reverse on ground","tap jump","hold jump","running jump","air reversal","run left","release B while moving","jump + run from rest","release then repress jump"};
        int previousScenario=-1,previousInput=0,count=0;ClassicMotor motor=null;
        double[] errors=new double[12];int[] frames=new int[12];
        foreach(string line in lines)
        {
            var v=line.Split(',');int scenario=int.Parse(v[0]),input=int.Parse(v[2]);
            if(previousScenario!=scenario){motor=new ClassicMotor{grounded=true,X=40};previousInput=0;previousScenario=scenario;}
            motor.Step(new Controls{horizontal=(input&1)!=0?1:(input&2)!=0?-1:0,run=(input&64)!=0,jump=(input&128)!=0,jumpPressed=(input&128)!=0&&(previousInput&128)==0});
            if(motor.Y<0){motor.Y=0;motor.vy256=0;motor.grounded=true;}
            double x=double.Parse(v[3],CultureInfo.InvariantCulture),y=double.Parse(v[4],CultureInfo.InvariantCulture);
            errors[scenario]=Math.Max(errors[scenario],Math.Max(Math.Abs(x-motor.X),Math.Abs(y-motor.Y)));
            frames[scenario]++;count++;previousInput=input;
        }
        for(int s=0;s<12;s++)Check("Reference motion: "+names[s],frames[s]==180&&errors[s]==0,"frames="+frames[s]+" maxPositionError="+errors[s]);
        string result="MARIO_PHYSICS_REFERENCE_"+(errors.All(e=>e==0)&&count==2160?"PASS":"FAIL")+" scenarios=12 frames="+count+" maxPositionError="+errors.Max();
        File.WriteAllText("Validation/physics-summary.txt",result);Debug.Log(result);
    }
}
