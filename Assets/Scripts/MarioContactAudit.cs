using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        Actor ContactFixture(int size=1,bool crouch=false)
        {
            // Private AreaData prevents the water fixture from changing the
            // shared 1-1 resource used by later fixtures and screenshots.
            EffectsArena();power=size;player.crouched=crouch;player.X=160;player.Y=64;
            player.grounded=false;player.vy256=512;
            var enemy=AuditActor("Goomba",160,64);enemy.vx=enemy.vy=0;return enemy;
        }
        bool ContactUnhurt(int size)=>phase==GamePhase.Playing&&power==size&&invulnerable==0;
        public void RunContactAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Contact: "+n,p,d);
            foreach(int size in new[]{1,2,3})
            foreach(bool crouch in new[]{false,true})
            foreach(int direction in new[]{-1,1})
            foreach(float overlap in new[]{.25f,2f,3.75f,4f,4.25f,8f})
            {
                var a=ContactFixture(size,crouch);
                player.X=direction<0?a.x-12+overlap:a.x+a.w-overlap;
                TickActor(a,default,player.Y-2);
                Check($"rising side size={size} crouch={crouch} side={direction} overlap={overlap}",
                    overlap<=4?ContactUnhurt(size):!ContactUnhurt(size),"phase="+phase+" power="+power);
            }
            foreach(int size in new[]{1,2,3})
            foreach(bool crouch in new[]{false,true})
            foreach(float overlap in new[]{.25f,2f,3.75f,4f,4.25f,8f})
            foreach(bool above in new[]{false,true})
            {
                var a=ContactFixture(size,crouch);
                player.Y=above?a.Top-overlap:a.y-PlayerHeight+overlap;
                TickActor(a,default,player.Y-2);
                Check($"rising vertical size={size} crouch={crouch} above={above} overlap={overlap}",
                    overlap<=4?ContactUnhurt(size):!ContactUnhurt(size),"phase="+phase+" power="+power);
                Check("rising contact never stomps",a.state<900);
            }
            foreach(int direction in new[]{-1,1})
            foreach(int size in new[]{1,2,3})
            foreach(string type in new[]{"Goomba","Koopa","Beetle","Shell","BeetleShell","BulletBill","HammerBro"})
            {
                ContactFixture(size);var a=AuditActor(type,160,64,type=="Koopa"||type=="HammerBro"?24:16);a.vx=a.vy=0;
                player.X=direction<0?a.x-11:a.x+a.w-1;player.Y=a.Top-1;player.vy256=-512;
                TickActor(a,default,a.Top+1);
                Check($"edge stomp remains generous {type} size={size} side={direction}",ContactUnhurt(size)&&player.VY>0&&score>0);
            }
            // Verify gameplay steps, not only direct collision calls.
            foreach(int direction in new[]{-1,1})
            {
                ContactFixture();var bill=AuditActor("BulletBill",160,64,14);bill.vx=0;
                player.X=direction<0?149:175;player.Y=60;player.vy256=1024;player.jumpOrigin=player.y256;
                bool hurt=false;for(int i=0;i<12;i++){Step(new Controls{jump=true});hurt|=phase!=GamePhase.Playing;}
                Check("jump glances along enemy edge "+direction,!hurt);
                ContactFixture();var enemy=actors[actors.Count-1];enemy.vx=0;player.X=direction<0?154:170;player.vy256=512;Step(new Controls{jump=true});
                Check("deep ascending side collision still hurts "+direction,phase==GamePhase.Dying&&enemy.state<900);
            }
            foreach(string type in new[]{"Spiny","SpinyEgg","Piranha","Bowser","Podoboo","Hammer","BowserFire","CastleFireball"})
            {
                ContactFixture();var a=AuditActor(type,160,64);a.vx=a.vy=0;a.age=100;
                if(type=="Piranha")a.originY=64;
                if(type=="CastleFireball"){a.originX=168;a.originY=72;a.state=0;}
                player.X=162;player.Y=65;player.vy256=-512;TickActor(a,default,96);
                Check(type+" deep hazard contact remains dangerous",phase==GamePhase.Dying&&a.state<900);
            }
            ContactFixture();area.underwater=true;var water=actors[actors.Count-1];player.Y=water.Top-1;player.vy256=-512;
            TickActor(water,default,water.Top+1);Check("water edge contact is forgiving but not a stomp",ContactUnhurt(1)&&water.state<900&&player.VY<0);
            player.Y=water.Top-8;TickActor(water,default,water.Top+1);Check("water deep contact hurts instead of stomping",phase==GamePhase.Dying&&water.state<900);
            ContactFixture();var starTarget=actors[actors.Count-1];star=300;player.X=149;TickActor(starTarget,default,62);
            Check("star keeps generous attack box",starTarget.state==1000&&phase==GamePhase.Playing);
            ContactFixture(2);var immune=actors[actors.Count-1];invulnerable=60;TickActor(immune,default,62);
            Check("existing damage invulnerability preserved",power==2&&phase==GamePhase.Playing);
            foreach(int direction in new[]{-1,1})
            foreach(float overlap in new[]{2f,3f,3.25f,6f})
            {
                ContactFixture();var flame=AuditActor("CastleFireball",160,64,8);flame.w=8;flame.originX=164;flame.originY=68;flame.state=0;
                player.X=direction<0?148+overlap:168-overlap;TickActor(flame,default,62);
                Check($"8px firebar keeps dangerous core side={direction} overlap={overlap}",overlap<=3?ContactUnhurt(1):phase==GamePhase.Dying);
            }
            foreach(string type in new[]{"Shell","BeetleShell"})
            foreach(int direction in new[]{-1,1})
            foreach(float overlap in new[]{2f,4f,6f})
            {
                ContactFixture();var shell=AuditActor(type,160,64);shell.vx=direction*3.5f;
                player.X=direction<0?shell.x+shell.vx-12+overlap:shell.x+shell.vx+shell.w-overlap;
                TickActor(shell,default,62);
                Check($"moving {type} side={direction} overlap={overlap}",overlap<=4?ContactUnhurt(1):phase==GamePhase.Dying);
            }
            ContactFixture();var coin=AuditActor("Coin",160,64);player.X=149;TickActor(coin,default,62);
            Check("coin pickup retains wider reach",coins==1&&!coin.alive);
            NewGame("1-1");
        }
        IEnumerator ContactSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            void Check(string n,bool p,string d=""){count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);}
            RunContactAudit(Check);
            var a=ContactFixture();player.X=150;player.Y=70;TickActor(a,default,68);RefreshVisuals();
            Check("screenshot rising graze is unharmed",ContactUnhurt(1)&&a.state<900);
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"contact-rising-graze-safe");
            a=ContactFixture();player.X=149;player.Y=a.Top-1;player.vy256=-512;TickActor(a,default,a.Top+1);RefreshVisuals();
            Check("screenshot edge stomp actually defeats enemy",!area.underwater&&a.state==900&&player.VY>0&&score==100&&ContactUnhurt(1));
            yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"contact-edge-stomp");
            string result=$"MARIO_CONTACT_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"contact-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"contact-result.txt"),result);Debug.Log(result);
        }
    }
}
