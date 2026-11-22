using System;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        int finishFireworks,finishEmitted,finishClock;
        bool finishPending;
        Actor Effect(string type,float x,float top,float width,float height,string text=null)
        {
            var a=Spawn(new NodeData{type=type,group="Effect",x=x,y=top,width=width,height=height,text=text});a.active=true;return a;
        }
        void ShowScore(string text,float center,float top)
        {Effect("ScorePop",center-text.Length*4,top+8,text.Length*8,8,text);}
        void ExplodeFireball(Actor shot)
        {
            if(!shot.alive)return;
            shot.alive=false;
            Effect("Firework",shot.x+shot.w/2-8,shot.y+shot.h/2+8,16,16);
            Sound("Firework");
        }
        Actor BowserBreath(Actor bowser)
        {
            bool right=player.X>bowser.x+bowser.w/2;
            var fire=Spawn(new NodeData{type="BowserFire",group="Character",x=right?bowser.x+bowser.w:bowser.x-24,y=bowser.y+24,width=24,height=8});
            fire.vx=right?1.5f:-1.5f;fire.active=true;Sound("Bowser Fires");return fire;
        }
        void AdvanceTransientEffects()
        {
            // Finish/death phases suspend enemy AI, but existing visual effects
            // must still animate and expire instead of freezing indefinitely.
            for(int i=0;i<actors.Count;i++)if(actors[i].alive&&(actors[i].data.group=="Effect"||actors[i].state>=900))TickActor(actors[i],default,player.Y);
        }
        static int FireworkCount(bool japanese,int time,int coinCount)
        {
            int digit=time%10;
            if(japanese)return digit==coinCount%10?(digit%2==0?6:3):0;
            return digit==1||digit==3||digit==6?digit:0;
        }
        void BeginFinishEffects()
        {finishFireworks=FireworkCount(level.Japanese,timeLeft,coins);finishEmitted=finishClock=0;finishPending=finishFireworks>0;}
        void TickFinishEffects()
        {
            if(!finishPending||timeLeft>0||phaseFrames<100||!FinishEntranceReady)return;
            if(finishFireworks>0&&finishClock++%28==0)
            {
                float[] dx={0,-32,32,-32,0,32};float[] y={144,120,120,96,144,96};
                var burst=Effect("Firework",Mathf.Clamp(player.X+dx[finishEmitted]-8,cameraX+8,cameraX+232),y[finishEmitted],16,16);
                burst.data.action="finish";finishFireworks--;finishEmitted++;score+=500;Sound("Firework");
            }
            if(finishFireworks==0&&!actors.Any(a=>a.alive&&a.data.type=="Firework"&&a.data.action=="finish"))finishPending=false;
        }
    }
}
