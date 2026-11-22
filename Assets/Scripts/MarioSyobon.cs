using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        Syobon.Engine syobon;
        double syobonAccumulator;
        bool syobonAccept;
        bool SyobonActive=>syobon!=null;
        void StartCampaign()=>StartCampaign(menuCampaign,1);
        void StartCampaign(int campaign,int stage)
        {if(campaign==2)StartSyobon(stage);else NewGame((campaign==1?"J-":"")+"1-1");}
        void StartSyobon(int stage)
        {
            StopSyobon();StopCourseAudio();
            syobon=new Syobon.Engine(gameObject);syobon.SetAudio(!validationMode);
            syobon.Start(stage,StartingLives);syobon.Render();
            menuCampaign=2;menuExit=menuLevels=menuSettings=menuRestart=false;
            if(stageRoot!=null)stageRoot.gameObject.SetActive(false);
            if(playerVisual!=null)playerVisual.Hide(true);
            phase=GamePhase.Playing;syobonAccumulator=0;ClearRestartInput();
        }
        void StopSyobon()
        {
            if(syobon==null)return;
            syobon.Dispose();syobon=null;syobonAccumulator=0;syobonAccept=false;
            if(stageRoot!=null)stageRoot.gameObject.SetActive(true);
            if(playerVisual!=null)playerVisual.Hide(false);
        }
        void UpdateSyobon(MarioInputFrame input)
        {
            if(!SyobonActive||phase==GamePhase.Paused||phase==GamePhase.Victory)return;
            var controls=GameplayControls(input);
            queuedInput.jumpPressed|=controls.jumpPressed;
            syobonAccept|=input.accept;
            syobonAccumulator+=System.Math.Min(Time.unscaledDeltaTime,.133);
            while(syobonAccumulator>=1/30d)
            {
                syobonAccumulator-=1/30d;
                controls.jump|=queuedInput.jumpPressed;queuedInput.jumpPressed=false;
                StepSyobon(controls,syobonAccept);syobonAccept=false;
                if(phase==GamePhase.Victory)break;
            }
        }
        void StepSyobon(Controls input,bool accept=false)
        {
            if(syobon==null||phase==GamePhase.Paused||phase==GamePhase.Victory)return;
            syobon.Step(new Syobon.KeyState{horizontal=input.horizontal,down=input.down,jump=input.jump,accept=accept});
            lives=syobon.Remaining;
            phase=syobon.Won?GamePhase.Victory:syobon.Dead?GamePhase.Dying:GamePhase.Playing;
        }
        void RetrySyobon(bool checkpoint)
        {
            syobon.Retry(checkpoint);syobon.Render();lives=syobon.Remaining;
            phase=GamePhase.Playing;menuRestart=menuSettings=menuLevels=false;ClearRestartInput();
        }
        static Rect SyobonPresentationRect(int width,int height,bool fill)
        {
            if(fill)return new Rect(0,0,width,height);
            float scale=Mathf.Min(width/480f,height/420f);
            return new Rect((width-480*scale)/2,(height-420*scale)/2,480*scale,420*scale);
        }
    }
}
