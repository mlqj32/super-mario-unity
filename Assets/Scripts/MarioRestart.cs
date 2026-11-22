using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        bool menuRestart,restartConfirmSelected;

        void ClearRestartInput()
        {
            accumulator=0;queuedInput=default;
            syobonAccumulator=0;syobonAccept=false;
            ClearJumpAssist();suppressGameplayInputFrame=Time.frameCount;
            // A held death-skip button must not also become a fresh jump/fire.
            var input=ReadInputFrame();
            previousJump=input.jump;previousFire=input.fire;
            blockJumpUntilReleased=input.jump;blockFireUntilReleased=input.fire;
        }

        bool HandleRestartInput(bool accept,bool cancel,bool changeSelection,bool skip)
        {
            if(menuRestart)
            {
                if(cancel)CancelRestart();
                else if(changeSelection)restartConfirmSelected=!restartConfirmSelected;
                else if(accept){if(restartConfirmSelected)ConfirmRestart();else CancelRestart();}
                return true; // Consume modal input, including the closing key.
            }
            if(phase==GamePhase.Dying&&(accept||skip)){CompleteDeath();return true;}
            return false;
        }

        void CompleteDeath()
        {
            if(phase!=GamePhase.Dying)return;
            if(SyobonActive){RetrySyobon(true);return;}
            lives=Mathf.Max(0,lives-1);sounds.Stop();
            power=1;star=invulnerable=powerFreeze=playerFirePose=0;
            phaseFrames=0;ClearRestartInput();
            if(lives==0){phase=GamePhase.GameOver;Sound("Game Over");return;}
            timeLeft=level.timeLimit;timerFrames=0;
            LoadLocation(checkpointLocation);
            if(checkpointX>0){player.X=checkpointX;player.Y=176;cameraX=Mathf.Max(0,checkpointX-64);}
            phase=GamePhase.Playing;UpdateCamera();RefreshVisuals();
        }

        void RequestRestart()
        {
            if(phase!=GamePhase.Paused||menuSettings||menuLevels)return;
            menuRestart=true;restartConfirmSelected=false;ClearRestartInput();
        }
        void CancelRestart(){menuRestart=false;ClearRestartInput();}
        void ConfirmRestart()
        {
            if(!menuRestart)return;
            if(SyobonActive){RetrySyobon(false);return;}
            menuRestart=menuLevels=menuSettings=false;
            power=1;star=invulnerable=powerFreeze=playerFirePose=0;
            sounds.Stop();LoadLevel(level.id);ClearRestartInput();
        }
        void DrawRestartConfirmation()
        {
            Fill(new Rect(0,34,256,206),new Color32(0,0,0,180));
            Fill(new Rect(12,65,232,134),new Color32(12,16,30,255));
            Text("确认重玩本关",56,78);
            Text(SyobonActive?"从本关起点重新开始":"从起点以小马里奥重新开始",50,104);
            Text(SyobonActive?"重置本关机关和检查点":"保留分数 金币和生命",32,123);

        }
    }
}
