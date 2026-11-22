using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Mario
{
    public sealed partial class MarioGame
    {
        // Virtual devices exercise the production input reader, not just menu
        // callbacks. Existing physical devices are preserved and restored.
        public void RunMenuAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Menus: "+n,p,d);
            var oldSettings=InputSystem.settings;var testSettings=UnityEngine.Object.Instantiate(oldSettings);
            var oldPad=Gamepad.current;var oldKeyboard=Keyboard.current;
            string oldKey=settingsKey;float volume=masterVolume;bool mute=muted,full=desiredFullscreen,fill=fillScreen,oldAuto=autoRun;int preset=windowPreset;
            Gamepad pad=null;Keyboard keyboard=null;float now=0;
            try
            {
                settingsKey="Mario.Tests.Menu.";autoRun=false;InputSystem.settings=testSettings;
                InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
#if UNITY_EDITOR
                // Edit-mode updates do not maintain ButtonControl's player-frame
                // edges. Run actual player input updates for this scoped fixture.
                InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE",true);
#endif
                pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();pad.MakeCurrent();keyboard.MakeCurrent();
                MarioInputFrame Read(GamepadState state=default,params Key[] keys)
                {
                    InputSystem.QueueStateEvent(pad,state);InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();
                    return ReadInputFrame();
                }
                void Send(GamepadState state=default,params Key[] keys)
                {var input=Read(state,keys);now+=.016f;HandleMenuInput(input,now);}
                void Press(Key key){Send();Send(default,key);Send();}
                void Pad(GamepadButton button){Send();Send(new GamepadState().WithButton(button));Send();}
                void TabTo(string id)
                {
                    Send();for(int i=0;i<45&&menuFocus!=id;i++)Press(Key.Tab);
                    Check("focus reachable "+CurrentMenu+"/"+id,menuFocus==id,"focus="+menuFocus);
                }
                void StartTitle(){NewGame("1-1");ReturnToTitle();focusPage=MenuPage.None;menuFocusMemory.Clear();Send();}
                StartTitle();Check("title starts on start button",menuFocus=="start");
                Press(Key.DownArrow);Check("keyboard down selects courses",menuFocus=="courses");
                Press(Key.Enter);Check("keyboard opens course menu",CurrentMenu==MenuPage.Courses);
                Press(Key.Escape);Check("course back returns to title",phase==GamePhase.Title&&menuFocus=="courses");
                Press(Key.Tab);Press(Key.Enter);Check("keyboard opens settings",CurrentMenu==MenuPage.Settings);
                float before=masterVolume;Press(Key.LeftArrow);Check("keyboard lowers volume",Mathf.Abs(masterVolume-(before-.05f))<.001f);
                Press(Key.Escape);Check("settings back restores title focus",phase==GamePhase.Title&&menuFocus=="settings");
                StartTitle();Pad(GamepadButton.RightShoulder);Check("shoulder switches campaign",menuCampaign==1);
                Pad(GamepadButton.DpadDown);Pad(GamepadButton.South);Check("pad opens courses",CurrentMenu==MenuPage.Courses);
                Pad(GamepadButton.DpadRight);Check("dpad moves right in course grid",menuFocus=="course1-2",menuFocus);
                Pad(GamepadButton.DpadDown);Check("dpad moves down in course grid",menuFocus=="course1-4",menuFocus);
                var accept=Read(new GamepadState().WithButton(GamepadButton.South));HandleMenuInput(accept,now+=.016f);
                Check("pad confirms Japanese course",level.id=="J-1-4"&&phase==GamePhase.Playing);
                var gameplay=GameplayControls(accept);Check("menu confirm cannot jump in new course",!gameplay.jump&&!gameplay.jumpPressed);
                GameplayControls(Read());gameplay=GameplayControls(Read(new GamepadState().WithButton(GamepadButton.South)));
                Check("released and repressed south jumps",gameplay.jump&&gameplay.jumpPressed);
                foreach(var button in new[]{GamepadButton.West,GamepadButton.East})
                {GameplayControls(Read());var c=GameplayControls(Read(new GamepadState().WithButton(button)));Check(button+" runs and fires",c.run&&c.firePressed);}
                foreach(int sign in new[]{-1,1})
                {
                    var c=GameplayControls(Read(new GamepadState{leftStick=new Vector2(sign,0)}));Check("stick movement "+sign,c.horizontal==sign);
                    c=GameplayControls(Read(new GamepadState().WithButton(sign>0?GamepadButton.DpadRight:GamepadButton.DpadLeft)));
                    Check("dpad movement "+sign,c.horizontal==sign);
                }
                Check("dpad down crouches / enters pipe",GameplayControls(Read(new GamepadState().WithButton(GamepadButton.DpadDown))).down);
                Check("dpad up climbs",GameplayControls(Read(new GamepadState().WithButton(GamepadButton.DpadUp))).up);
                Pad(GamepadButton.Start);Check("start pauses",phase==GamePhase.Paused);
                Pad(GamepadButton.DpadDown);Pad(GamepadButton.DpadDown);Pad(GamepadButton.South);
                Check("pad opens restart with cancel selected",menuRestart&&!restartConfirmSelected&&menuFocus=="cancel");
                Pad(GamepadButton.South);Check("default restart accept cancels",!menuRestart&&phase==GamePhase.Paused);
                Pad(GamepadButton.South);Pad(GamepadButton.DpadRight);Check("pad selects restart confirmation",restartConfirmSelected);
                Pad(GamepadButton.East);Check("east cancels restart and restores focus",!menuRestart&&menuFocus=="restart");
                power=3;Pad(GamepadButton.South);Pad(GamepadButton.DpadRight);Pad(GamepadButton.South);
                Check("pad confirms restart as small Mario",phase==GamePhase.Playing&&power==1&&location==0);
                Pad(GamepadButton.Start);Pad(GamepadButton.DpadDown);Pad(GamepadButton.DpadDown); // focus remembered at restart
                TabTo("settings");Pad(GamepadButton.South);Check("pad opens settings",menuSettings);
                TabTo("volume");SetVolume(.5f);Send();
                Send(new GamepadState{leftStick=Vector2.right});float first=masterVolume;
                for(int i=0;i<10;i++)Send(new GamepadState{leftStick=Vector2.right});
                Check("held stick does not repeat every frame",Mathf.Abs(first-.55f)<.001f&&masterVolume==first);
                now+=.4f;Send(new GamepadState{leftStick=Vector2.right});Check("held stick repeats after delay",masterVolume>first);
                Send();before=masterVolume;for(int i=0;i<30;i++)Send(new GamepadState{leftStick=new Vector2(.1f,.1f)});
                Check("stick drift does not navigate or adjust",masterVolume==before&&menuFocus=="volume");
                SetVolume(0);Pad(GamepadButton.DpadLeft);Check("menu volume clamps at zero",masterVolume==0);
                SetVolume(1);Pad(GamepadButton.DpadRight);Check("menu volume clamps at one",masterVolume==1);
                TabTo("mute");bool wasMuted=muted;Pad(GamepadButton.South);Check("pad toggles mute",muted!=wasMuted);
                desiredFullscreen=true;TabTo("display");Pad(GamepadButton.South);Check("pad selects window mode",!desiredFullscreen);
                TabTo("aspect");bool wasFill=fillScreen;Pad(GamepadButton.South);Check("pad toggles presentation",fillScreen!=wasFill);
                TabTo("autoRun");Pad(GamepadButton.South);Check("pad enables automatic running",autoRun);
                Pad(GamepadButton.DpadLeft);Check("pad left disables automatic running",!autoRun);
                Press(Key.RightArrow);Check("keyboard right enables automatic running",autoRun);
                Press(Key.Enter);Check("keyboard confirm toggles automatic running off",!autoRun);
                for(int i=0;i<3;i++){TabTo("size"+i);Pad(GamepadButton.South);Check("pad selects window preset "+i,windowPreset==i);}
                TabTo("display");Pad(GamepadButton.South);Check("pad selects fullscreen",desiredFullscreen);
                TabTo("autoRun");Press(Key.Tab);Check("disabled window sizes are skipped",menuFocus=="back");
                Pad(GamepadButton.East);Check("settings back stays paused",phase==GamePhase.Paused&&!menuSettings);
                Pad(GamepadButton.Start);Check("start resumes pause",phase==GamePhase.Playing);
                // Every course is confirmable with the same keyboard menu input.
                foreach(int campaign in new[]{0,1})for(int w=1;w<=8;w++)for(int s=1;s<=4;s++)
                {
                    PauseGame(true);menuCampaign=campaign;TabTo("course"+w+"-"+s);Press(Key.Enter);
                    string id=(campaign==1?"J-":"")+w+"-"+s;
                    Check("keyboard launches "+id,phase==GamePhase.Playing&&level.id==id);
                }
                // Arrow graph must reach all controls, not merely Tab traversal.
                foreach(MenuPage page in new[]{MenuPage.Title,MenuPage.Pause,MenuPage.Courses,MenuPage.Settings,MenuPage.Restart,MenuPage.End,MenuPage.Exit})
                {
                    StartTitle();
                    if(page==MenuPage.Pause||page==MenuPage.Courses||page==MenuPage.Restart){NewGame("1-1");PauseGame(page==MenuPage.Courses);}
                    if(page==MenuPage.Settings){OpenSettings();desiredFullscreen=false;}
                    if(page==MenuPage.Restart)RequestRestart();if(page==MenuPage.End)phase=GamePhase.Victory;if(page==MenuPage.Exit)RequestExit(true);
                    Send();var visited=new HashSet<string>{menuFocus};var queue=new Queue<string>();queue.Enqueue(menuFocus);
                    while(queue.Count>0)
                    {
                        string from=queue.Dequeue();
                        foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                        {
                            FocusMenuItem(from);HandleMenuInput(default,now+=.016f);HandleMenuInput(new MarioInputFrame{direction=direction},now+=.016f);
                            if(visited.Add(menuFocus))queue.Enqueue(menuFocus);
                        }
                    }
                    var missing=MenuItems().Where(i=>i.enabled&&!visited.Contains(i.id)).Select(i=>i.id).ToArray();
                    Check("arrows reach all controls on "+page,missing.Length==0,string.Join(",",missing));
                }
                StartTitle();TabTo("exit");Pad(GamepadButton.South);Check("exit confirmation defaults to cancel",menuExit&&menuFocus=="cancel");
                Pad(GamepadButton.East);Check("exit cancel returns to title",!menuExit&&phase==GamePhase.Title);
                NewGame("1-1");PauseGame();TabTo("title");Pad(GamepadButton.South);Pad(GamepadButton.DpadRight);Pad(GamepadButton.South);
                Check("pad returns to title",phase==GamePhase.Title&&!menuExit);
                foreach(var ending in new[]{GamePhase.GameOver,GamePhase.Victory})
                {
                    phase=ending;Send();TabTo("courses");Pad(GamepadButton.South);Pad(GamepadButton.East);
                    Check(ending+" course back returns to ending",phase==ending);
                    TabTo("again");Pad(GamepadButton.South);Check(ending+" replay",phase==GamePhase.Playing);
                }
                NewGame("1-1");Die();int livesBefore=lives;Pad(GamepadButton.South);
                Check("pad skips death exactly once",phase==GamePhase.Playing&&lives==livesBefore-1);
                NewGame("1-1");usingGamepad=true;padWasConnected=true;InputSystem.RemoveDevice(pad);pad=null;
                TrackInputDevice(new MarioInputFrame());Check("active controller disconnect pauses gameplay",phase==GamePhase.Paused);
                pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();Pad(GamepadButton.Start);
                Check("reconnected controller resumes",phase==GamePhase.Playing);
                var hotkeys=Read(default,Key.F11,Key.M);Check("fullscreen / mute keyboard mapping",hotkeys.fullscreen&&hotkeys.mute);
                foreach(bool automatic in new[]{false,true})
                foreach(bool controller in new[]{false,true})
                foreach(int direction in new[]{-1,1})
                {
                    AuditArena();player.X=200;autoRun=automatic;power=3;GameplayControls(Read());
                    for(int i=0;i<60;i++)
                    {
                        var input=controller?Read(new GamepadState{leftStick=new Vector2(direction,0)}):Read(default,direction>0?Key.RightArrow:Key.LeftArrow);
                        Step(GameplayControls(input));
                    }
                    Check($"automatic={automatic} controller={controller} direction={direction} speed",Mathf.Abs(player.VX-direction*(automatic?2.5f:1.5f))<.01f,"vx="+player.VX);
                    Check("movement does not auto-fire",!actors.Any(a=>a.alive&&a.data.type=="Fireball"));
                    if(automatic)
                    {
                        var jump=GameplayControls(Read(new GamepadState().WithButton(GamepadButton.South)));
                        Check("automatic running still permits jump",jump.run&&jump.jumpPressed&&!jump.firePressed);
                        GameplayControls(Read());Step(GameplayControls(Read(new GamepadState().WithButton(GamepadButton.West))));
                        Check("automatic running keeps explicit fire button",actors.Count(a=>a.alive&&a.data.type=="Fireball")==1);
                        var held=GameplayControls(Read(new GamepadState().WithButton(GamepadButton.West)));
                        Check("held fire is not repeated by automatic running",held.run&&!held.firePressed);
                        for(int i=0;i<12;i++)Step(GameplayControls(Read()));
                        Check("automatic running stops with neutral direction",player.VX==0);
                    }
                }
                autoRun=true;NewGame("1-1");Check("automatic running persists across new game",autoRun);
                Die();CompleteDeath();Check("automatic running persists across respawn",autoRun);
                StartTitle();Pad(GamepadButton.LeftShoulder);
                Check("left shoulder wraps to Cat Mario",menuCampaign==2);
                Pad(GamepadButton.South);Check("pad launches Cat Mario",SyobonActive&&syobon.Stage==1&&phase==GamePhase.Playing);
                GameplayControls(Read());float catX=syobon.X;
                for(int i=0;i<15;i++)StepSyobon(GameplayControls(Read(new GamepadState{leftStick=Vector2.right})));
                Check("Cat Mario moves with stick",syobon.X>catX+10);
                Pad(GamepadButton.Start);int catTicks=syobon.TickCount;
                StepSyobon(new Controls{horizontal=1});
                Check("Cat Mario pause freezes simulation",phase==GamePhase.Paused&&syobon.TickCount==catTicks);
                TabTo("settings");Pad(GamepadButton.South);Check("Cat Mario shares settings",menuSettings&&SyobonActive);
                Check("Cat Mario fixed speed disables automatic run setting",!MenuItems().Find(i=>i.id=="autoRun").enabled);
                Pad(GamepadButton.East);TabTo("restart");Pad(GamepadButton.South);
                Check("Cat Mario restart defaults to cancel",menuRestart&&!restartConfirmSelected);
                Pad(GamepadButton.DpadRight);Pad(GamepadButton.South);
                Check("Cat Mario confirmed restart",SyobonActive&&phase==GamePhase.Playing&&syobon.X<150&&syobon.Checkpoint==0);
                foreach(int catStage in new[]{1,2,3,4})
                {
                    Pad(GamepadButton.Select);
                    Check("Cat Mario course focus points to a course",menuFocus.StartsWith("cat"),menuFocus);
                    TabTo("cat"+catStage);Pad(GamepadButton.South);
                    Check("pad selects Cat Mario "+catStage,SyobonActive&&syobon.Stage==catStage);
                }
                usingGamepad=padWasConnected=true;TrackInputDevice(default);
                Check("Cat Mario controller disconnect pauses",phase==GamePhase.Paused);
                ResumeGame();ReturnToTitle();Check("Cat Mario exit releases runtime",!SyobonActive&&phase==GamePhase.Title);
                Pad(GamepadButton.RightShoulder);Pad(GamepadButton.South);
                Check("switching back to original restores Mario",!SyobonActive&&phase==GamePhase.Playing&&level.id=="1-1");
            }
            finally
            {
                if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(oldPad!=null&&oldPad.added)oldPad.MakeCurrent();if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
                InputSystem.settings=oldSettings;UnityEngine.Object.DestroyImmediate(testSettings);
                foreach(string key in new[]{"volume","muted","fill","fullscreen","window","aspectVersion","autoRun"})PlayerPrefs.DeleteKey("Mario.Tests.Menu."+key);
                settingsKey=oldKey;masterVolume=volume;muted=mute;desiredFullscreen=full;fillScreen=fill;autoRun=oldAuto;windowPreset=preset;ApplyVolume();
                menuExit=false;menuFocusMemory.Clear();focusPage=MenuPage.None;heldMenuDirection=0;menuNeutralRequired=false;
                usingGamepad=padWasConnected=false;NewGame("1-1");
            }
        }
        IEnumerator MenuSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunMenuAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            SetDisplayMode(false);windowPreset=1;ApplyDisplayMode();yield return WaitForDisplay(false);
            ReturnToTitle();usingGamepad=true;RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"menu-title-pad");
            PauseGame(true);yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"menu-courses-pad");
            CloseCourses();OpenSettings();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"menu-settings-pad");
            CloseSettings();NewGame("1-1");PauseGame();RequestRestart();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"menu-restart-pad");
            string result=$"MARIO_MENU_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"menu-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"menu-result.txt"),result);Debug.Log(result);NewGame("1-1");
        }
    }
}
