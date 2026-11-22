using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        bool lakituEncounter,lakituStopped;
        int lakituRespawnFrames;
        void MoveLakituHorizontal(Actor a,float desiredVelocity)
        {
            // Fixed 60 Hz simulation: retain velocity and brake through zero.
            // Keep the last facing while nearly stationary to avoid sprite flicker.
            a.vx=Mathf.MoveTowards(a.vx,desiredVelocity,.1f);a.x+=a.vx;
            if(Mathf.Abs(a.vx)>.15f)a.lakituFacing=a.vx>0?1:-1;
        }
        void StopLakituEncounter()
        {
            lakituStopped=true;lakituRespawnFrames=0;
            foreach(var a in actors.Where(a=>a.alive&&a.data.type=="Lakitu"&&a.state<900))a.fleeing=true;
        }
        bool TickLakitu(Actor a)
        {
            // Stop before the throw check even when the map marker is later in
            // the actor list. Japanese maps use the goal as their final boundary.
            if(lakituStopped||actors.Any(b=>b.data.type=="LakituStop"&&player.X>=b.x)||
                (level.Japanese&&actors.Any(b=>b.data.type=="FlagPole"&&player.X+12>=b.x)))StopLakituEncounter();
            if(a.fleeing)
            {
                MoveLakituHorizontal(a,-2.5f);
                if(a.x+a.w<cameraX-32)a.alive=false;
                return true;
            }
            // Repeated enemy-stream markers must not create a stack of clouds.
            if(actors.Any(b=>b!=a&&b.alive&&b.active&&b.state<900&&b.data.type=="Lakitu"&&!b.fleeing&&b.age>=a.age))
            {a.alive=false;return true;}
            lakituEncounter=true;lakituRespawnFrames=0;
            // The motor can retain horizontal speed while terrain or the left
            // camera boundary blocks Mario. Track displacement, not that intent.
            float playerVelocity=a.lakituTrackingPlayer?Mathf.Clamp(player.X-a.lakituPlayerX,-2.5f,2.5f):player.VX;
            a.lakituPlayerX=player.X;a.lakituTrackingPlayer=true;
            float movingWeight=Mathf.Clamp01(Mathf.Abs(playerVelocity)/1.5f);
            // Blend a small idle patrol into a lead based on actual travel,
            // not the instantly changing input-facing or a speed threshold.
            float lead=Mathf.Lerp(Mathf.Sin(a.age*.012f)*24,Mathf.Clamp(playerVelocity*18,-40,40),movingWeight);
            float targetX=player.X+lead;
            float speedLimit=Mathf.Min(3.1f,Mathf.Max(1.1f,Mathf.Abs(playerVelocity)+.6f));
            float desiredVelocity=Mathf.Clamp(playerVelocity+(targetX-a.x)*.025f,-speedLimit,speedLimit);
            MoveLakituHorizontal(a,desiredVelocity);
            a.y=Mathf.MoveTowards(a.y,144+Mathf.Sin(a.age*.025f)*6,.2f);
            if(a.age%150==0&&a.x+a.w>=cameraX&&a.x<=cameraX+256)
            {
                var egg=Spawn(new NodeData{type="SpinyEgg",group="Character",x=a.x,y=a.y,width=14,height=16});
                egg.active=true;egg.vy=2;
            }
            return false;
        }
        void UpdateLakituEncounter()
        {
            if(phase!=GamePhase.Playing||!lakituEncounter||lakituStopped)return;
            if(actors.Any(a=>a.alive&&a.active&&a.data.type=="Lakitu"&&a.state<900)){lakituRespawnFrames=0;return;}
            if(++lakituRespawnFrames<350)return;
            var replacement=Spawn(new NodeData{type="Lakitu",group="Character",x=cameraX+256,y=168,width=16,height=24});
            replacement.active=true;lakituRespawnFrames=0;
        }
    }
}
