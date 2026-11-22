using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public enum GamePhase { Title, Playing, Paused, Dying, Flag, Castle, Clear, GameOver, Victory }
    public sealed class Actor
    {
        public NodeData data; public float x,y,w,h,vx,vy,originX,originY,previousX,previousY;
        public bool alive=true,active,grounded,used,triggered; public int age,state,health=5,cooldown,coinCount,coinTimer,idleFrames;
        public PixelVisual visual;
        public Actor scaleRope;
        public bool fleeing,platformWrapped;
        public int lakituFacing=-1;
        public float lakituPlayerX;public bool lakituTrackingPlayer;
        public int springFrame;public bool springBoost;
        public Rect Bounds => new Rect(x,y,w,h);
        public float Top => y+h;
        public bool IsPowerup => data.type=="Mushroom"||data.type=="Mushroom1Up"||data.type=="FireFlower"||data.type=="Star"||data.type=="PoisonMushroom";
        // Character is a rendering group shared by enemies, items and effects.
        // Combat must not use that group as an enemy classification.
        public bool IsEnemy => data.type=="Goomba"||data.type=="Koopa"||data.type=="Beetle"||
            data.type=="Spiny"||data.type=="SpinyEgg"||data.type=="Piranha"||data.type=="Blooper"||
            data.type=="CheepCheep"||data.type=="BulletBill"||data.type=="Lakitu"||data.type=="HammerBro"||
            data.type=="Bowser"||data.type=="Shell"||data.type=="BeetleShell";
        public bool Solid => state<900 && data.group=="Solid" && !data.type.StartsWith("Detect") &&
            data.type!="CastleAxe" && data.type!="CastleChain" && !data.type.StartsWith("Scroll");
        public bool Platform => data.type=="Platform" || data.type=="TreeTop" || data.type=="ShroomTop" || data.type=="BridgeBase";
    }
    [DefaultExecutionOrder(-100)]
    public sealed partial class MarioGame : MonoBehaviour
    {
        public LevelData startingLevel;
        public bool startImmediately;
        public ClassicMotor player = new ClassicMotor();
        public GamePhase phase;
        public LevelData level;
        public AreaData area;
        public List<Actor> actors = new List<Actor>();
        public const int StartingLives=10;
        public int location, frame, score, coins, lives=StartingLives, power=1, timeLeft=400, invulnerable, star;
        public float cameraX;
        public Camera gameCamera;
        public bool validationMode;
        public string Error { get; private set; }
        public int World => level.World;
        public int Stage => level.Stage;
        public float PlayerHeight => power>1 && !player.crouched ? 32 : 16;
        public Rect PlayerBounds => new Rect(player.X,player.Y,12,PlayerHeight);
        public LevelData[] Levels { get; private set; }
        public string PlayerVisualSignature => playerVisual==null?string.Empty:playerVisual.CurrentSignature;
        bool SimulationActive => phase==GamePhase.Playing||phase==GamePhase.Dying||phase==GamePhase.Flag||phase==GamePhase.Castle||phase==GamePhase.Clear;
        Transform stageRoot; PixelVisual playerVisual,waterBackdrop;
        AudioSource music,sounds; RenderTexture target;
        double accumulator; Controls queuedInput; bool previousJump,previousFire,sectionPassed,cheeps,bills,autoWalk;
        int phaseFrames,areaFrame,powerFreeze,checkpointLocation,playerFirePose,timerFrames;
        GamePhase phaseBeforePause=GamePhase.Playing;
        float checkpointX,areaWidth,flagX; Actor standing,climbing;
        string nextMap; int nextLocation; bool inTransport; int transportTimer; Vector2 transportVelocity;
        bool menuLevels,muted; float masterVolume=.7f;
        int mazePasses,mazeCorrect;
        public Action<string> trace;

        void Awake() { if(!validationMode) Initialize(); }
        public void Initialize()
        {
            if(gameCamera!=null) return;
            // Present one stable 256x240 frame per v-synced repaint.
            Application.targetFrameRate=60; QualitySettings.vSyncCount=1;
            Levels=Resources.LoadAll<LevelData>("Levels").OrderBy(l=>l.id).ToArray();
            if(Levels.Length!=64) { Error="关卡加载失败：应有 64 关，实际 "+Levels.Length; Debug.LogError(Error); return; }
            gameCamera=new GameObject("Pixel camera").AddComponent<Camera>();gameCamera.transform.SetParent(transform);
            gameCamera.orthographic=true;gameCamera.orthographicSize=120;gameCamera.nearClipPlane=.1f;gameCamera.farClipPlane=100;
            gameCamera.clearFlags=CameraClearFlags.SolidColor;gameCamera.allowHDR=false;gameCamera.allowMSAA=false;
            target=new RenderTexture(256,240,16,RenderTextureFormat.ARGB32){filterMode=FilterMode.Point,antiAliasing=1,useMipMap=false,autoGenerateMips=false};target.Create();gameCamera.targetTexture=target;
            // OnGUI presents this texture. Disable automatic camera rendering so
            // a repaint cannot observe a partially updated target.
            gameCamera.enabled=false;
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.4f;
            sounds=gameObject.AddComponent<AudioSource>();sounds.volume=.65f;
            InitializeCourseAudio();
            InitializeSettings();
            if(FindAnyObjectByType<AudioListener>()==null)gameCamera.gameObject.AddComponent<AudioListener>();
            var pv=new GameObject("Mario / native fixed-point motor");pv.transform.SetParent(transform);playerVisual=pv.AddComponent<PixelVisual>();
            // Older scenes serialized the original three-life field value.
            // Reset it at session startup, including editor direct-play mode.
            lives=StartingLives;
            level=startingLevel!=null?startingLevel:Levels[0]; LoadLevel(level.id,0,false);
            phase=startImmediately?GamePhase.Playing:GamePhase.Title;
            if(phase==GamePhase.Title)music.Stop();
            RefreshVisuals();
        }
        void Update()
        {
            WindowsWindowPolicy.EnsureInstalled();
            if(validationMode || Error!=null)return;
            var input=ReadInputFrame();TrackInputDevice(input);
            if(input.fullscreen){SetDisplayMode(!Screen.fullScreen);ClearRestartInput();return;}
            if(input.mute)ToggleMute();
            Vector2 mouse=Input.mousePosition;
            if((mouse-previousMouse).sqrMagnitude>1){menuPointer=true;usingGamepad=false;}
            previousMouse=mouse;
            if(HandleMenuInput(input,Time.unscaledTime))return;
            if(suppressGameplayInputFrame==Time.frameCount)return;
            if(SyobonActive){UpdateSyobon(input);return;}
            var gameplay=GameplayControls(input);
            gameplay.jumpPressed|=queuedInput.jumpPressed;gameplay.firePressed|=queuedInput.firePressed;
            queuedInput=gameplay;
            accumulator+=Math.Min(Time.unscaledDeltaTime,.133);
            int steps=0;
            while(accumulator>=1/ClassicMotor.Frequency&&steps++<8)
            {accumulator-=1/ClassicMotor.Frequency;Step(queuedInput);queuedInput.jumpPressed=false;queuedInput.firePressed=false;}
            RefreshVisuals();
        }
        void PauseGame(bool courses=false)
        {if(phase!=GamePhase.Paused)phaseBeforePause=phase;phase=GamePhase.Paused;menuLevels=courses;music.Pause();sounds.Pause();courseJingle.Pause();syobon?.Pause();ClearRestartInput();}
        void ResumeGame()
        {phase=phaseBeforePause;menuLevels=menuSettings=false;ClearRestartInput();if(phase!=GamePhase.Title){music.UnPause();sounds.UnPause();courseJingle.UnPause();syobon?.Resume();}}
        void OnApplicationFocus(bool focus)
        { if(!focus&&SimulationActive&&!validationMode)PauseGame();ClearRestartInput();heldMenuDirection=0;menuNeutralRequired=true; }
        public void NewGame(string id)
        {score=coins=0;lives=StartingLives;power=1;star=invulnerable=0;checkpointX=0;checkpointLocation=0;LoadLevel(id);menuExit=menuLevels=menuSettings=false;}
        public void LoadLevel(string id,int destination=0,bool resetTimer=true)
        {
            StopSyobon();
            level=Levels.FirstOrDefault(l=>l.id==id)??throw new ArgumentException("Unknown level "+id);
            menuCampaign=level.Japanese?1:0;
            if(resetTimer){timeLeft=level.timeLimit;timerFrames=0;checkpointX=0;checkpointLocation=0;star=invulnerable=0;}
            menuRestart=false;powerFreeze=0;accumulator=0;queuedInput=default;
            LoadLocation(destination);phase=GamePhase.Playing;phaseFrames=0;
        }
        void LateUpdate()
        {
            if(SyobonActive){syobon.Render();return;}
            if(gameCamera!=null&&!gameCamera.enabled)gameCamera.Render();
        }
        public void LoadLocation(int index)
        {
            if(index<0||index>=level.locations.Length)throw new ArgumentOutOfRangeException(nameof(index),level.id+" location "+index);
            StopCourseAudio();
            CancelSpringRide();
            ResetPlayerRunAnimation();
            ResetCastleEntry();
            if(stageRoot!=null){stageRoot.gameObject.SetActive(false);if(Application.isPlaying)Destroy(stageRoot.gameObject);else DestroyImmediate(stageRoot.gameObject);}
            stageRoot=new GameObject(level.id+" / area "+level.locations[index].area).transform;stageRoot.SetParent(transform);
            actors.Clear();location=index;area=level.areas[level.locations[index].area];areaWidth=area.width;areaFrame=0;
            waterBackdrop=null;
            if(area.NeedsWaterBackdrop)
            {
                var water=new GameObject("Underwater background");water.transform.SetParent(stageRoot,false);
                waterBackdrop=water.AddComponent<PixelVisual>();
                waterBackdrop.Set("Scenery","Water",area.setting,256,208,-10);
            }
            sectionPassed=cheeps=bills=false;standing=climbing=null;inTransport=false;playerFirePose=0;
            mazePasses=mazeCorrect=0;
            lakituEncounter=lakituStopped=false;lakituRespawnFrames=0;
            finishPending=false;finishFireworks=finishEmitted=finishClock=0;
            foreach(var n in area.nodes)Spawn(n);
            ConnectScaleRopes();
            player=new ClassicMotor{swimming=area.underwater,responsive=responsiveControls,
                jumpGravity=area.underwater?13:32,fallGravity=area.underwater?10:112};ClearRestartInput();
            var loc=level.locations[index];player.X=loc.x>0?loc.x:40;player.Y=loc.entry=="Castle"?96:0;
            if(loc.entry=="Normal")player.Y=176;
            if(loc.positioned){player.X=loc.x;player.Y=loc.y;}
            var entrance=actors.FirstOrDefault(a=>a.data.entrance==index);
            if(entrance!=null){player.X=entrance.x+10;player.Y=entrance.Top;}
            ConstrainSwimmingSurface();
            autoWalk=loc.entry=="Walking";
            cameraX=Mathf.Max(0,player.X-64);
            gameCamera.backgroundColor=!area.underwater&&(area.setting.Contains("Underworld")||area.setting.Contains("Castle")||area.setting.Contains("Night"))?Color.black:new Color32(92,148,252,255);
            PlayMusic();UpdateCamera();RefreshVisuals();trace?.Invoke("Location "+level.id+"/"+index);
        }
        public Actor Spawn(NodeData source,float offset=0)
        {
            var n=source.Copy();n.x+=offset;if(n.sliding){n.begin+=offset;n.end+=offset;}
            var a=new Actor{data=n,x=n.x,y=n.y-n.height,w=n.width,h=n.height,originX=n.x,originY=n.y-n.height,vx=n.vx,vy=n.vy};
            if(n.type=="Piranha"&&n.action=="upside-down")
            {
                // Imported inverted plants use the cap tile's top as their
                // anchor. The opening is its bottom, one tile lower. Resolve
                // against actual terrain without rewriting editable maps.
                var cap=actors.FirstOrDefault(b=>b.Solid&&Mathf.Abs(b.Top-n.y)<.01f&&
                    b.x<n.x+n.width&&b.x+b.w>n.x&&b.data.spriteOverride!=null&&
                    (b.data.spriteOverride.EndsWith("/10")||b.data.spriteOverride.EndsWith("/11")));
                if(cap!=null){a.y-=cap.h;a.originY-=cap.h;}
            }
            if(n.type=="Goomba"||n.type=="Koopa"||n.type=="Beetle"||n.type=="Spiny")a.vx=-.5f;
            if(n.type=="Bowser")
            {
                a.vx=-.5f;
                // Japanese enemy Y encodes a shorter body anchor. Resolve the
                // 8px foot overlap before horizontal collision can push Bowser
                // through an entire row of bridge tiles and offscreen.
                if(level.Japanese)
                {
                    var support=actors.Where(b=>b.alive&&b.Solid&&!b.data.hidden&&b.x<a.x+a.w&&b.x+b.w>a.x&&b.Top>a.y&&b.Top<=a.y+8.01f).OrderByDescending(b=>b.Top).FirstOrDefault();
                    if(support!=null)a.y=a.originY=support.Top;
                }
            }
            if(n.type=="CheepCheep")a.vx=-.5f;
            if(n.type=="BulletBill"&&a.vx==0)a.vx=-1.5f;
            var go=new GameObject(n.type);go.transform.SetParent(stageRoot,false);a.visual=go.AddComponent<PixelVisual>();
            actors.Add(a);if(n.group!="Character"&&n.group!="Effect")areaWidth=Mathf.Max(areaWidth,a.x+a.w);
            return a;
        }
        public void Step(Controls input)
        {
            if(Error!=null||phase==GamePhase.Title||phase==GamePhase.Paused||phase==GamePhase.GameOver||phase==GamePhase.Victory)return;
            frame++;areaFrame++;phaseFrames++;
            CaptureJumpIntent(input);
            if(playerFirePose>0)playerFirePose--;
            if(powerFreeze>0){powerFreeze--;return;}
            AdvancePlayerRunAnimation();
            if(phase!=GamePhase.Playing){CancelSpringRide();AdvanceTransientEffects();}
            if(phase==GamePhase.Dying)
            {
                if(phaseFrames>30){player.Y+=player.VY;player.vy256-=40;}
                if(phaseFrames>150)CompleteDeath();
                return;
            }
            if(phase==GamePhase.Flag)
            {
                player.vx4096=player.vy256=0;player.X=flagX-10;player.Y=Mathf.Max(flagBaseY,player.Y-2);
                foreach(var a in actors)if(a.data.type=="Flag")a.y=Mathf.Max(flagBaseY,a.y-2);
                if(player.Y<=flagBaseY&&phaseFrames>70)BeginClearSequence("Stage Clear");
                return;
            }
            if(phase==GamePhase.Castle)
            {
                if(phaseFrames%5==0){var bridge=actors.FirstOrDefault(a=>a.alive&&a.data.type=="CastleBridge");if(bridge!=null){bridge.w=Mathf.Max(0,bridge.w-8);if(bridge.w==0)bridge.alive=false;Sound("Break Block");}}
                foreach(var b in actors)if(b.alive&&b.state<900&&b.data.type=="Bowser")b.y-=3;
                if(phaseFrames>100)BeginClearSequence("World Clear");return;
            }
            if(phase==GamePhase.Clear)
            {
                TickFinishWalk();
                if(timeLeft>0){int count=Math.Min(timeLeft,5);timeLeft-=count;score+=count*50;}
                TickFinishEffects();
                if(ClearSequenceReady){if(World==8&&Stage==4)BeginVictory();else{LoadLevel((level.Japanese?"J-":"")+(Stage==4?World+1:World)+"-"+(Stage==4?1:Stage+1));}}return;
            }
            if(inTransport)
            {player.X+=transportVelocity.x;player.Y+=transportVelocity.y;if(--transportTimer<=0){if(!string.IsNullOrEmpty(nextMap))LoadLevel(nextMap,Math.Max(0,nextLocation));else LoadLocation(Math.Max(0,nextLocation));}return;}
            if(invulnerable>0)invulnerable--;if(star>0&&--star==0)PlayMusic();
            bool clockTick=++timerFrames%24==0;
            if(clockTick&&--timeLeft<=0){Die();return;}
            if(timeLeft==100&&clockTick){Sound("Hurry");music.pitch=1.15f;}
            if(autoWalk)input=new Controls{horizontal=1};
            bool onVine=false;
            if(climbing!=null)
            {
                player.X=climbing.x;player.Y+=(input.up?1.5f:0)-(input.down?1.5f:0);
                player.vx4096=player.vy256=0;player.grounded=false;
                if(input.jumpPressed){player.vy256=768;climbing=null;input.jumpPressed=false;ClearJumpAssist();}
                else if(player.Y>176){BeginTransport(climbing,Vector2.zero);return;}
                else onVine=true;
            }
            // Advance support surfaces first, then carry their rider by the
            // same displacement. This avoids a persistent one-frame gap.
            if(standing==null&&player.grounded)
                standing=actors.FirstOrDefault(a=>a.alive&&a.data.type=="Platform"&&Mathf.Abs(player.Y-a.Top)<.05f&&player.X+12>a.x&&player.X<a.x+a.w);
            foreach(var a in actors){a.previousX=a.x;a.previousY=a.y;a.platformWrapped=false;}
            foreach(var a in actors)
            {
                if(!a.alive||a.data.type!="Platform")continue;
                if(a.x<cameraX+272&&a.x+a.w>cameraX-48)a.active=true;
                if(!a.active)continue;
                TickActor(a,input,player.Y);
                if(standing==a&&a.platformWrapped){standing=null;player.grounded=false;}
            }
            AdvanceSpringboards(input);
            float oldY=player.Y;
            if(!onVine)
            {
            if(responsiveControls&&input.horizontal!=0)player.facing=input.horizontal;
            if(input.firePressed&&power==3&&actors.Count(a=>a.alive&&a.data.type=="Fireball")<2)
            {var f=Spawn(new NodeData{type="Fireball",group="Character",x=player.X+8,y=player.Y+PlayerHeight-6,width=8,height=8});f.vx=player.facing*4;f.vy=-2;f.active=true;playerFirePose=8;Sound("Fireball");}
            bool wasGrounded=player.grounded;
            if(standing!=null&&standing.alive){player.X+=standing.x-standing.previousX;player.Y+=standing.y-standing.previousY;}
            float oldX=player.X;oldY=player.Y;
            player.crouched=power>1&&input.down&&player.grounded;
            if(!player.crouched&&power>1&&SolidOverlaps(new Rect(player.X,player.Y+16,12,16)))player.crouched=true;
            player.responsive=responsiveControls;
            bool assistedJump=responsiveControls&&TryAssistedJump();
            if(responsiveControls||springRider!=null)input.jumpPressed=false;
            player.Step(input);
            if(level.Japanese&&actors.Any(a=>a.data.type=="Wind"&&a.Bounds.Overlaps(PlayerBounds))&&(frame&((frame&128)!=0?1:3))==0)player.X+=1;
            if(!responsiveControls&&wasGrounded&&!player.grounded)Sound(area.underwater?"Kick":power>1?"Jump Super":"Jump Small");
            MovePlayer(oldX,oldY,input);
            if(springRider!=null&&standing!=springRider)springRider=null;
            FinishAssistedMovement(wasGrounded,assistedJump);
            }
            if(phase!=GamePhase.Playing)return;
            cameraX=Mathf.Max(cameraX,player.X-112);if(area.sections.Length==0)cameraX=Mathf.Min(cameraX,Mathf.Max(0,areaWidth-256));
            player.X=Mathf.Max(player.X,cameraX+1);
            ConstrainSwimmingSurface();
            if(player.Y<-64){if(area.exit>=0)LoadLocation(area.exit);else Die();return;}
            if(location==0&&Stage!=4&&player.X>areaWidth*.52f&&checkpointX==0&&player.grounded){checkpointX=player.X;checkpointLocation=location;}
            for(int i=0;i<actors.Count;i++)
            {
                var a=actors[i];if(!a.alive||a.data.type=="Platform")continue;
                a.previousX=a.x;a.previousY=a.y;
                if(a.x<cameraX+272&&a.x+a.w>cameraX-48)a.active=true;
                if(!a.active)continue;
                TickActor(a,input,oldY);
                if(phase!=GamePhase.Playing||inTransport)break;
            }
            if(cheeps&&frame%110==0){var f=Spawn(new NodeData{type="CheepCheep",group="Character",x=cameraX+96+(frame%113),y=-24,width=16,height=16,jumping=true});f.active=true;f.vx=1.5f;f.vy=5.5f;}
            UpdateLakituEncounter();
            if(area.underwater&&frame%96==0&&player.Y+PlayerHeight<176)Effect("Bubble",player.X+(player.facing>0?10:0),player.Y+PlayerHeight-2,4,4);
            if(bills&&frame%145==0){var b=Spawn(new NodeData{type="BulletBill",group="Character",x=cameraX+256,y=48+(frame%96),width=16,height=14});b.active=true;b.vx=-2;}
            if(frame%120==0)
            {
                for(int i=actors.Count-1;i>=0;i--)
                {
                    var a=actors[i];if(a.alive)continue;
                    a.visual.gameObject.SetActive(false);
                    if(Application.isPlaying)Destroy(a.visual.gameObject);else DestroyImmediate(a.visual.gameObject);
                    actors.RemoveAt(i);
                }
            }
            // Pickups can increase the player's height after movement, followed
            // by a power-up freeze. Keep that first enlarged frame underwater.
            if(phase==GamePhase.Playing)ConstrainSwimmingSurface();
            UpdateCamera();
        }
        void ConstrainSwimmingSurface()
        {
            // Y is the feet position. The upper boundary must constrain the
            // head, including when an imported entry starts above the water.
            // Pipe travel has its own trajectory and bypasses this boundary.
            if(!area.underwater||inTransport||player.Y+PlayerHeight<176)return;
            player.Y=176-PlayerHeight;
            if(player.vy256>0)player.vy256=0;
        }
        bool SolidOverlaps(Rect r) => actors.Any(a=>a.alive&&a.Solid&&!a.Platform&&!a.data.hidden&&r.Overlaps(a.Bounds));
        void MovePlayer(float oldX,float oldY,Controls input)
        {
            float targetY=player.Y;player.Y=oldY;
            int collisionCount=actors.Count;
            for(int index=0;index<collisionCount;index++)
            {
                var a=actors[index];
                if(!a.alive||!a.Solid||a.Platform||a.data.hidden||a==springRider)continue;
                if(!PlayerBounds.Overlaps(a.Bounds))continue;
                if(player.X>oldX){player.X=a.x-12;player.vx4096=0;if(a.data.type=="PipeHorizontal"&&(input.horizontal>0||autoWalk)&&(a.data.targetLocation>=0||!string.IsNullOrEmpty(a.data.targetMap))){BeginTransport(a,Vector2.right*.5f);return;}}
                else if(player.X<oldX){player.X=a.x+a.w;player.vx4096=0;}
            }
            player.Y=targetY;player.grounded=false;standing=null;
            for(int index=0;index<collisionCount;index++)
            {
                var a=actors[index];
                // Recycling an elevator is not a swept movement through the shaft.
                // Excluding it for this tick also prevents reacquiring a detached
                // rider or stealing the rider of another platform in the same column.
                if(!a.alive||!a.Solid||a.platformWrapped)continue;
                if(player.X+12<=a.x||player.X>=a.x+a.w)continue;
                float top=a.Top;
                float approachTop=a.data.type=="Platform"?Mathf.Min(top,a.previousY+a.h):top;
                if(targetY<=oldY&&oldY>=approachTop-.05f&&player.Y<=top+.01f&&!a.data.hidden)
                {player.Y=top;player.vy256=0;player.grounded=true;standing=a;if(a.data.type=="Springboard")BeginSpring(a,input);}
                else if(!a.Platform&&targetY>oldY&&oldY+PlayerHeight<=a.y+.1f&&player.Y+PlayerHeight>=a.y)
                {player.Y=a.y-PlayerHeight;player.vy256=0;Bump(a);}
            }
            if(standing!=null&&input.down&&standing.data.type.StartsWith("Pipe")&&(standing.data.targetLocation>=0||!string.IsNullOrEmpty(standing.data.targetMap))&&Mathf.Abs(player.X+6-(standing.x+standing.w/2))<8)
                BeginTransport(standing,Vector2.down*.5f);
        }
        void Bump(Actor a)
        {
            if(a.used){Sound("Bump");return;}
            var type=a.data.type;
            if(type!="Brick"&&type!="Block"){Sound("Bump");return;}
            BumpOccupants(a);
            if(type=="Brick"&&string.IsNullOrEmpty(a.data.contents)&&power>1)
            {
                a.alive=false;score+=50;Sound("Break Block");
                for(int j=0;j<4;j++){var b=Effect("BrickShard",a.x+(j%2)*8,a.Top-(j/2)*8,8,8);b.vx=j%2==0?-1.5f:1.5f;b.vy=3+(j/2);}
                return;
            }
            a.state=12;a.data.hidden=false;
            string contents=a.data.contents;
            if(type=="Brick"&&string.IsNullOrEmpty(contents)){Sound("Bump");return;}
            if(type=="Brick"&&contents=="Coin")
            {if(a.coinCount==0)a.coinTimer=11*21;else if(a.coinTimer==0)a.used=true;a.coinCount++;PopCoin(a);return;}
            a.used=true;Sound("Bump");
            if(string.IsNullOrEmpty(contents)||contents=="Coin"){PopCoin(a);return;}
            if(contents=="Mushroom"&&power>1)contents="FireFlower";
            var item=Spawn(new NodeData{type=contents,group="Character",x=a.x,y=a.Top+16,width=16,height=16,targetLocation=a.data.targetLocation,targetMap=a.data.targetMap});
            item.active=true;item.cooldown=32;item.state=-32;item.y=a.Top-16;item.originY=a.Top;
            if(contents=="Vine"){item.h=0;item.y=a.Top;}
            else item.vx=contents=="FireFlower"?0:1;
            Sound(contents=="Vine"?"Vine Emerging":"Powerup Appears");
        }
        void BumpOccupants(Actor block)
        {
            int count=actors.Count;for(int i=0;i<count;i++)
            {
                var occupant=actors[i];
                if(!occupant.alive||occupant.state>=900||Mathf.Abs(occupant.y-block.Top)>=4||
                    occupant.x+occupant.w<=block.x||occupant.x>=block.x+block.w)continue;
                if(occupant.IsPowerup)
                {
                    if(occupant.state<0||occupant.data.type=="FireFlower")continue;
                    occupant.vy=3;occupant.grounded=false;
                    occupant.vx=occupant.x+occupant.w/2<player.X+6?-1:1;
                }
                else if(occupant.IsEnemy)Kill(occupant,true);
            }
        }
        public void BeginTransport(Actor a,Vector2 velocity)
        { if(a.data.targetLocation<0&&string.IsNullOrEmpty(a.data.targetMap))return;CancelSpringRide();ClearJumpAssist();inTransport=true;transportTimer=40;transportVelocity=velocity;nextMap=a.data.targetMap;nextLocation=a.data.targetLocation;player.vx4096=player.vy256=0;Sound("Pipe"); }
        void ExpandSection(Actor trigger,int index)
        {
            trigger.triggered=true;if(index<0||index>=area.sections.Length)throw new InvalidOperationException("Invalid castle section");
            var sec=area.sections[index];float offset=trigger.x;sectionPassed=false;
            foreach(var part in sec.parts)
            {
                if(part.nodes.Length==0)continue;
                foreach(var original in part.nodes)
                {var n=original.Copy();if(part.kind=="stretch"){n.x=0;n.width=256;}Spawn(n,offset);}
                offset+=part.kind=="stretch"?256:part.width;
            }
            trace?.Invoke("Section "+index+" at "+trigger.x);
        }
        void PopCoin(Actor block)
        {
            Coin();
            var effect=Spawn(new NodeData{type="CoinPop",group="Effect",x=block.x+4,y=block.Top+16,width=8,height=16});
            effect.active=true;effect.vy=5;effect.originY=block.Top;
        }
        public void Coin(){coins++;score+=200;Sound("Coin");if(coins>=100){coins-=100;lives++;Sound("Gain Life");}}
        public void Hurt()
        {
            if(invulnerable>0||star>0||phase!=GamePhase.Playing)return;
            if(power>1){power=1;invulnerable=120;powerFreeze=30;Sound("Power Down");}else Die();
        }
        public void Die(){if(phase!=GamePhase.Playing)return;CancelSpringRide();ClearJumpAssist();phase=GamePhase.Dying;phaseFrames=0;star=invulnerable=powerFreeze=playerFirePose=0;standing=climbing=null;inTransport=false;player.crouched=false;player.vy256=1024;StopCourseAudio();Sound("Player Dies");trace?.Invoke("Death");}
        void Kill(Actor a,bool flip=false)
        {
            if(!a.alive||!a.IsEnemy||a.state>=900)return;score+=100;ShowScore("100",a.x+a.w/2,a.Top);
            bool falling=flip||a.data.type!="Goomba";
            a.state=falling?1000:900;a.age=0;a.vy=flip?3:0;a.vx=flip?1:0;a.active=true;
            if(!flip&&a.data.type=="Goomba"){a.data.type="DeadGoomba";a.data.group="Solid";a.h=8;}
            Sound(flip?"Kick":"Kick");
        }
        void PlayMusic()
        {if(music==null||!AudioPlaybackEnabled)return;string name=star>0?"Star":area.underwater?"Underwater":area.setting.Contains("Castle")?"Castle":area.setting.Contains("Underworld")?"Underworld":area.setting.Contains("Sky")?"Sky":"Overworld";music.clip=Resources.Load<AudioClip>("Audio/"+name);music.pitch=timeLeft<=100?1.15f:1;music.Play();}
        void Sound(string name){if(AudioPlaybackEnabled&&sounds!=null){var clip=Resources.Load<AudioClip>("Audio/"+name);if(clip!=null)sounds.PlayOneShot(clip);}}
        void UpdateCamera(){if(gameCamera!=null)gameCamera.transform.position=new Vector3(Mathf.Floor(cameraX)+128,88,-10);}
        public void RefreshVisuals()
        {
            if(gameCamera==null)return;
            if(waterBackdrop!=null)waterBackdrop.transform.localPosition=new Vector3(Mathf.Floor(cameraX),-32,0);
            foreach(var platform in actors)
                if(platform.data.inScale)UpdateScaleRope(platform);
            foreach(var a in actors)
            {
                bool hide=!a.alive||a.data.hidden||a.data.type.StartsWith("Section")||a.data.type.StartsWith("Scroll")||a.data.type.StartsWith("Detect")||a.data.type.EndsWith("Start")||a.data.type.EndsWith("Stop")||a.x+a.w<cameraX-64||a.x>cameraX+320;
                if(a.data.type=="Podoboo"&&a.y<=-32&&a.vy==0)hide=true;
                if(a.h<=0&&(a.data.type=="String"||a.data.type=="ScaleRope"))hide=true;
                if(area.HasFlagPoleArtwork(a.data))hide=true;
                a.visual.Hide(hide);if(hide)continue;
                int animationTick=a.data.group=="Character"||a.data.group=="Effect"?a.age:frame;
                string state=area.setting+" "+(a.used?"used":animationTick/10%2==0?"one":"two")+(a.data.smart?" smart":"")+(a.data.red?" red":"")+(a.data.small?" small":"")+(a.data.jumping||a.data.floating?" jumping":"");
                // Four-cell sprites use semantic catalog names. The previous
                // generic one/two state left Fireballs, Stars, Hammers and
                // Fire Flowers on only part of their animation.
                int animationCell=(animationTick/4)%4;
                string fourFrame=animationCell==0?"normal":animationCell==1?"two":animationCell==2?"three":"four";
                if(a.data.type=="Fireball"||a.data.type=="FireFlower"||a.data.type=="Hammer"||a.data.type=="Star"||a.data.type=="CastleFireball")
                    state=area.setting+" "+fourFrame+(a.data.smart?" smart":"")+(a.data.red?" red":"")+(a.data.small?" small":"")+(a.data.jumping||a.data.floating?" jumping":"");
                else if(a.data.type=="CoinPop")
                {int cell=a.age/2%4;state="anim "+(cell==0?"normal":"anim"+(cell+1));}
                else if(a.data.type=="Coin")
                {int cell=a.age/8%6;state=area.setting+" "+(cell<3?"normal":cell==4?"three":"two");}
                else if(a.data.type=="Firework")state=a.age<7?"normal":a.age<14?"n2":"n3";
                else if(a.data.type=="CheepCheep")
                    state=area.setting+" "+(a.data.jumping?"flying":"normal")+" "+(a.data.red?"red ":"")+(animationTick/10%2==0?"normal":"two");
                else if(a.data.type=="Blooper"&&a.age%100>=45)
                    state+=" squeeze";
                if(a.data.type=="Bowser"&&a.age%110<12)state+=" firing";
                if(a.data.type=="Lakitu"&&!a.fleeing&&a.age%150>=129)state+=" hiding";
                if(a.data.type=="HammerBro"&&a.age%40<8&&a.age%180<120)state+=" throwing";
                bool flip=a.vx>0&&a.data.group=="Character"&&a.data.type!="PoisonMushroom";
                // Bullet Bill's source art faces right; walking enemies face left.
                if(a.data.type=="BulletBill")flip=a.vx<0;
                if(a.data.type=="Bowser")flip=player.X>a.x+a.w/2;
                if(a.data.type=="HammerBro"&&a.state<900)flip=HammerBroDirection(a)>0;
                if(a.data.type=="Lakitu"&&a.state<900)flip=a.lakituFacing>0;
                if(a.data.type=="BrickShard")flip=a.age/4%2!=0;
                // The reference Goomba walk consists of one asymmetric sprite
                // alternating with its mirror. It has no "two" texture entry.
                if(a.data.type=="Goomba"&&a.active&&a.state<900)flip=a.age/8%2!=0;
                bool bump=(a.data.type=="Brick"||a.data.type=="Block")&&a.state>0&&a.state<=12;
                a.visual.transform.localPosition=new Vector3(PixelWorldX(a.x)+(flip?a.w:0),Mathf.Round(a.y)+(bump?Mathf.Round(Mathf.Sin(a.state/12f*Mathf.PI)*4):0),0);
                if(a.data.type=="CustomText"||a.data.type=="ScorePop"){a.visual.SetText(a.data.text,a.h,a.data.type=="ScorePop"?5:3);continue;}
                if(!string.IsNullOrEmpty(a.data.spriteOverride))
                {string file=a.data.spriteOverride;if(a.used)file=file.Substring(0,file.LastIndexOf('/')+1)+"c5";else if(file.EndsWith("/c0")||file.EndsWith("/c1")||file.EndsWith("/c2"))file+="-"+(frame/8%6);a.visual.SetTile(file,a.data.group=="Scenery"?-5:0,a.w,a.h,a.data.type=="ScaleRope");continue;}
                if(a.data.type=="PoisonMushroom")
                {int palette=area.underwater?0:area.setting.Contains("Underworld")?2:area.setting.Contains("Castle")?3:1;a.visual.SetTile("JapanTiles/"+palette+"/poison",a.state<0?-1:1);continue;}
                string group=a.data.group=="Effect"?"Character":a.data.group,type=a.data.type;
                if(type=="CoinPop"){group="Character";type="Coin";}
                // Actors must draw above terrain; equal sort orders allowed
                // castle masonry to cover moving flames. Pipes stay in front
                // so emerging plants/items remain properly occluded.
                int order=group=="Scenery"?-5:type.StartsWith("Pipe")?2:group=="Character"?1:0;
                // Japanese pipes are raw terrain tiles at order 0, not Pipe
                // actors. Plants must sit behind solid terrain in both games,
                // including the ground around short and inverted pipes.
                if(type=="Piranha")order=-1;
                if(a.data.group=="Effect")order=4;
                if(a.IsPowerup&&a.state<0)order=-1;
                // Defeated bodies fall through terrain; living plant/pipe
                // occlusion must not hide their death animation mid-flight.
                if(a.state==1000)order=3;
                if(a.data.action=="horizontal-flight")state+=" jumping";
                bool flipVertical=a.state==1000||a.data.action=="upside-down"||(type=="BowserFire"&&a.age/8%2!=0)||(type=="Podoboo"&&a.vy<0);
                a.visual.Set(group,type,state,a.w,a.h,order,flip,flipVertical:flipVertical);
            }
            string ps=power==3?"fiery":power>1?"large":"";
            if(phase==GamePhase.Dying)ps="dead";
            else if(phase==GamePhase.Flag||climbing!=null)ps+=" climbing "+(frame/8%2==0?"normal":"two");
            else if(player.crouched)ps+=" crouching";
            else if(playerFirePose>0)ps+=" firing"+(!player.grounded?" jumping":"");
            else if(area.underwater)ps+=" paddling "+(frame/8%4==0?"normal":"paddle"+(frame/8%4))+(frame/32%2==0?"":" swim2");
            else if(!player.grounded&&phase!=GamePhase.Title)ps+=" jumping";
            else if(Mathf.Abs(player.VX)>.05f)
            {
                int runCell=runAnimationUnits/6;
                ps+=" running "+(player.VX*player.facing<0?"skidding":runCell==1?"two":runCell==2?"three":"normal");
            }
            if(star>0)ps+=" star "+((frame/4)%4==0?"normal":"star"+((frame/4)%4+1));
            bool flipP=player.facing<0;playerVisual.Hide(finishEntered||invulnerable>0&&frame/3%2==0);
            playerVisual.transform.localPosition=new Vector3(PixelWorldX(player.X)-2+(flipP?16:0),Mathf.Round(player.Y),0);
            float ph=phase==GamePhase.Dying?14:PlayerHeight;
            playerVisual.Set("Character","Player",ps,phase==GamePhase.Dying?15:16,ph,1,flipP);
            UpdateCamera();
        }
        void OnDestroy(){StopSyobon();WindowsWindowPolicy.Shutdown();if(target!=null){if(gameCamera!=null)gameCamera.targetTexture=null;target.Release();DestroyImmediate(target);}}
    }
}
