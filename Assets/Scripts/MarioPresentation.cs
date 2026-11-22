using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        int runAnimationUnits;
        bool wasAnimatingRun;

        // Quantize the relative position once. Rounding Mario and flooring the
        // camera separately alternated his screen position by a pixel even when
        // the camera followed at an exactly constant distance.
        float PixelWorldX(float worldX)
        {
            return Mathf.Floor(cameraX)+Mathf.Floor(worldX-cameraX+.5f);
        }

        void ResetPlayerRunAnimation(){runAnimationUnits=0;wasAnimatingRun=false;}
        void AdvancePlayerRunAnimation()
        {
            bool running=(phase==GamePhase.Playing||phase==GamePhase.Clear)&&
                player.grounded&&!player.crouched&&!area.underwater&&climbing==null&&
                !inTransport&&Mathf.Abs(player.VX)>.05f&&player.VX*player.facing>=0;
            if(!running){ResetPlayerRunAnimation();return;}
            // Keep phase through walk/run speed changes. Dividing the global
            // frame by a changing period skipped/reversed poses at the threshold.
            if(wasAnimatingRun)runAnimationUnits=(runAnimationUnits+(Mathf.Abs(player.VX)>1.75f?2:1))%18;
            wasAnimatingRun=true;
        }
    }
}
