using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunRestartAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Restart: "+n,p,d);
            foreach(string id in new[]{"1-1","J-1-1"})
            {
                foreach(int form in new[]{1,2,3})
                {
                    NewGame(id);power=form;star=123;invulnerable=45;score=1200;coins=7;
                    player.X=400;player.Y=16;player.vx4096=4096;checkpointX=320;checkpointLocation=0;
                    PauseGame();RequestRestart();int before=frame;Step(new Controls{horizontal=1});
                    Check(id+" modal freezes gameplay form="+form,menuRestart&&phase==GamePhase.Paused&&frame==before&&player.X==400);
                    Check("default Enter cancels and consumes input",HandleRestartInput(true,false,false,false)&&!menuRestart&&phase==GamePhase.Paused);
                    Check("cancel preserves state",power==form&&star==123&&invulnerable==45&&score==1200&&coins==7&&lives==StartingLives&&player.X==400&&player.vx4096==4096);
                    RequestRestart();Check("Escape cancels",HandleRestartInput(false,true,false,false)&&!menuRestart&&phase==GamePhase.Paused);
                    RequestRestart();HandleRestartInput(false,false,true,false);
                    Check("selection leaves dialog open",menuRestart&&restartConfirmSelected);
                    Check("confirm consumes input",HandleRestartInput(true,false,false,false));
                    Check("confirmed restart starts small at course entrance",level.id==id&&phase==GamePhase.Playing&&power==1&&PlayerHeight==16&&location==0&&checkpointX==0&&checkpointLocation==0&&player.X<100);
                    Check("restart clears temporary state",star==0&&invulnerable==0&&powerFreeze==0&&playerFirePose==0&&!inTransport&&standing==null&&climbing==null&&!player.crouched);
                    Check("restart resets timer and motion",timeLeft==level.timeLimit&&timerFrames==0&&player.vx4096==0&&player.vy256==0&&queuedInput.Equals(default(Controls))&&accumulator==0);
                    Check("restart preserves totals and closes menus",score==1200&&coins==7&&lives==StartingLives&&!menuRestart&&!menuLevels&&!menuSettings);
                    player.X=90;ConfirmRestart();Check("duplicate confirmation is ignored",player.X==90&&lives==StartingLives);
                }
                foreach(int tick in new[]{0,30,149,150})
                {
                    NewGame(id);power=3;star=100;invulnerable=42;checkpointX=320;checkpointLocation=0;score=900;coins=2;
                    Die();phaseFrames=tick;
                    Check("death skip consumed at tick "+tick,HandleRestartInput(false,false,false,true));
                    Check(id+" quick respawn uses checkpoint and small form",phase==GamePhase.Playing&&lives==StartingLives-1&&power==1&&player.X==320&&player.Y==176&&cameraX==256&&star==0&&invulnerable==0);
                    Check("respawn preserves score and resets timer",score==900&&coins==2&&timeLeft==level.timeLimit&&phaseFrames==0);
                    CompleteDeath();Check("repeated death completion cannot deduct life",lives==StartingLives-1&&phase==GamePhase.Playing);
                }
                NewGame(id);Die();for(int i=0;i<150;i++)Step(default);
                Check("normal death animation still plays",phase==GamePhase.Dying&&lives==StartingLives);
                Step(default);CompleteDeath();Check("automatic completion deducts exactly once",phase==GamePhase.Playing&&lives==StartingLives-1&&power==1);
                NewGame(id);lives=1;power=3;Die();HandleRestartInput(true,false,false,false);CompleteDeath();Step(default);
                Check("last life skip ends game without new campaign",phase==GamePhase.GameOver&&lives==0&&power==1);
                NewGame(id);lives=1;Die();phaseFrames=150;Step(default);
                Check("last life automatic completion ends game",phase==GamePhase.GameOver&&lives==0);
                NewGame(id);power=3;LoadLevel(id);
                Check("normal level loading retains power",power==3);
                PauseGame();RequestRestart();LoadLevel(id);Check("new level clears stale confirmation",!menuRestart&&phase==GamePhase.Playing);
            }
            foreach(char c in "立即复活跳过动画确认重玩本关从起点以小马里奥重新开始保留分数金币和生命取消左右选择回车")
                Check("UI glyph "+c,PixelLibrary.Glyph(c)!=null);
            NewGame("1-1");
        }

        IEnumerator RestartSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunRestartAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            NewGame("1-1");for(int i=0;i<40;i++)Step(default);power=3;Die();RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"restart-death-prompt");
            HandleRestartInput(true,false,false,false);
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"restart-quick-respawn");
            for(int i=0;i<40;i++)Step(default);power=3;RefreshVisuals();PauseGame();RequestRestart();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"restart-confirmation");
            ConfirmRestart();for(int i=0;i<40;i++)Step(default);RefreshVisuals();
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"restart-small-mario");
            string result="MARIO_RESTART_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"restart-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"restart-result.txt"),result);Debug.Log(result);
        }
    }
}
