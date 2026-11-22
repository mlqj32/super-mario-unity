using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        [Tooltip("Responsive acceleration, jump buffering and ledge grace. Disable for the legacy movement profile.")]
        public bool responsiveControls=true;
        const int JumpBufferTicks=9,CoyoteTicks=6;
        int jumpBufferTicks,coyoteTicks,suppressGameplayInputFrame=-1;

        void ClearJumpAssist(){jumpBufferTicks=coyoteTicks=0;}
        void CaptureJumpIntent(Controls input)
        {
            if(!responsiveControls||phase!=GamePhase.Playing||inTransport||autoWalk||climbing!=null){ClearJumpAssist();return;}
            if(jumpBufferTicks>0)jumpBufferTicks--;
            if(input.jumpPressed)jumpBufferTicks=JumpBufferTicks;
        }
        bool TryAssistedJump()
        {
            if(jumpBufferTicks==0||phase!=GamePhase.Playing||inTransport||autoWalk||climbing!=null||springRider!=null)return false;
            if(!player.TryJump(coyoteTicks>0&&!player.swimming&&player.vy256<=0))return false;
            ClearJumpAssist();standing=null;
            Sound(player.swimming?"Kick":power>1?"Jump Super":"Jump Small");return true;
        }
        void FinishAssistedMovement(bool wasGrounded,bool jumped)
        {
            if(!responsiveControls)return;
            if(springRider!=null){ClearJumpAssist();return;}
            if(inTransport||autoWalk||phase!=GamePhase.Playing){ClearJumpAssist();return;}
            if(player.grounded)
            {
                coyoteTicks=CoyoteTicks;
                // Collision has just established support: consume a queued jump
                // now, rather than requiring another press or another grounded tick.
                TryAssistedJump();
            }
            else if(player.vy256>0){coyoteTicks=0;if(!jumped&&standing!=null)ClearJumpAssist();}
            else if(wasGrounded&&!jumped&&!player.swimming)coyoteTicks=CoyoteTicks;
            else if(coyoteTicks>0)coyoteTicks--;
        }
    }
}
