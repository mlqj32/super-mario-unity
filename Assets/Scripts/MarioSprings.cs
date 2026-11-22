using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        Actor springRider;
        const int SpringCycleTicks=12;
        void SpringHeight(Actor spring)
        {
            float compression=1-Mathf.Abs(spring.springFrame-6)/6f;
            spring.h=spring.data.height-Mathf.Round(Mathf.Min(16,Mathf.Max(0,spring.data.height-10))*compression);
        }
        void BeginSpring(Actor spring,Controls input)
        {
            if(springRider==spring)return;
            if(spring.springFrame==0){spring.springFrame=1;spring.springBoost=false;}
            spring.springBoost|=input.jump||input.jumpPressed||jumpBufferTicks>0;
            springRider=spring;standing=spring;ClearJumpAssist();
            player.vx4096=player.vy256=0;player.grounded=true;
            SpringHeight(spring);player.Y=spring.Top;
        }
        void AdvanceSpringboards(Controls input)
        {
            foreach(var spring in actors)
            {
                if(!spring.alive||spring.data.type!="Springboard"||spring.springFrame==0)continue;
                bool riding=springRider==spring&&standing==spring&&player.grounded&&
                    player.X+12>spring.x&&player.X<spring.x+spring.w;
                if(springRider==spring&&!riding)springRider=null;
                if(riding){spring.springBoost|=input.jump||input.jumpPressed||jumpBufferTicks>0;ClearJumpAssist();}
                spring.springFrame++;SpringHeight(spring);
                if(riding)player.Y=spring.Top;
                if(spring.springFrame<SpringCycleTicks)continue;
                spring.springFrame=0;spring.h=spring.data.height;
                if(riding)
                {
                    player.Y=spring.Top;player.LaunchSpring(spring.springBoost);
                    springRider=standing=null;ClearJumpAssist();Sound("Jump Super");
                }
                spring.springBoost=false;
            }
        }
        void CancelSpringRide()
        {
            if(springRider==null)return;
            springRider.h=springRider.data.height;springRider.springFrame=0;springRider.springBoost=false;
            if(standing==springRider)standing=null;
            springRider=null;
        }
    }
}
