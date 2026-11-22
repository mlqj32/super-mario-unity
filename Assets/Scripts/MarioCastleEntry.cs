using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        bool finishOutside,finishEntered,finishDoorKnown;
        float flagBaseY=16,finishWalkTarget,finishDoorY;
        bool FinishEntranceReady=>!finishOutside||!finishDoorKnown||finishEntered;

        void ResetCastleEntry()
        {finishOutside=finishEntered=finishDoorKnown=false;flagBaseY=16;finishWalkTarget=finishDoorY=0;}

        void PrepareFlagEntry(Actor pole)
        {
            finishOutside=true;finishEntered=false;flagBaseY=pole.y;
            // Large castles also have upper windows/doors. Only consider an
            // entrance at the foot of the flag. Japanese maps retain original
            // metatiles: 4a is the arch, 4b is its lower door (CastleMetatiles).
            var door=actors.Where(a=>a.alive&&a.x>pole.x&&a.y<=flagBaseY&&
                (a.data.type=="CastleDoor"||(a.data.spriteOverride??"").EndsWith("/4b")))
                .OrderBy(a=>a.x).FirstOrDefault();
            finishDoorKnown=door!=null;
            finishWalkTarget=door!=null?door.x+door.w/2-6:pole.x+112;
            finishDoorY=door!=null?door.y:flagBaseY-16;
            player.crouched=false;player.vx4096=player.vy256=0;
            climbing=standing=null;playerFirePose=0;invulnerable=0;ClearJumpAssist();
        }

        void BeginFinishWalk()
        {
            player.facing=1;player.crouched=false;player.vy256=0;climbing=standing=null;
            playerFirePose=0;ResetPlayerRunAnimation();
            if(!finishOutside)finishWalkTarget=player.X+123.75f;
        }

        void TickFinishWalk()
        {
            if(finishEntered){player.vx4096=player.vy256=0;return;}
            bool walking=player.X<finishWalkTarget;
            player.facing=1;player.crouched=false;
            player.vx4096=walking?5120:0; // 1.25 pixels per logic step.
            float oldX=player.X,oldY=player.Y;
            player.X=Mathf.Min(finishWalkTarget,player.X+(walking?1.25f:0));
            player.vy256=Mathf.Max(-1024,player.vy256-112);player.Y+=player.VY;
            // Normal terrain landing maintains grounded, velocity and support
            // state. Merely moving X/Y left Mario in his airborne sprite.
            MovePlayer(oldX,oldY,default);
            if(finishOutside&&player.grounded&&player.X>=finishWalkTarget-.01f&&Mathf.Abs(player.Y-finishDoorY)<.1f)
            {
                finishEntered=true;player.vx4096=player.vy256=0;ResetPlayerRunAnimation();
            }
            cameraX=Mathf.Max(cameraX,player.X-176);
            if(area.sections.Length==0)cameraX=Mathf.Min(cameraX,Mathf.Max(0,areaWidth-256));
            UpdateCamera();
        }
    }
}
