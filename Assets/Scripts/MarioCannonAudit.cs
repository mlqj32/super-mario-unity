using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        Actor CannonFixture(int height=32)
        {
            AuditArena();player.X=40;player.Y=0;
            return Spawn(new NodeData{type="Cannon",group="Solid",x=160,y=height,width=16,height=height});
        }
        public void RunCannonAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Cannon: "+n,p,d);
            foreach(int direction in new[]{-1,1})
            {
                var cannon=CannonFixture();player.X=cannon.x+direction*80;cannon.age=149;TickActor(cannon,default,player.Y);
                var bill=actors.FirstOrDefault(a=>a.data.type=="BulletBill");
                Check("fires toward player "+direction,bill!=null&&bill.vx==direction*1.5f);
                if(bill==null)continue;
                Check("projectile aligns with muzzle",bill.x==cannon.x&&bill.Top==cannon.Top&&bill.h==14);
                float x=bill.x,y=bill.y;TickActor(bill,default,player.Y);RefreshVisuals();
                Check("flies horizontally "+direction,bill.x==x+direction*1.5f&&bill.y==y);
                Check("nose faces direction of travel "+direction,bill.visual.transform.localScale.x==(direction<0?-1:1));
                // Cross behind the shot: it must not home or reverse in flight.
                player.X=cannon.x-direction*80;TickActor(bill,default,player.Y);
                Check("crossing player does not reverse flight",bill.vx==direction*1.5f);
                CheckEffectPixels(bill,"Cannon projectile "+direction,Check);
                bill.x=80;bill.y=48;bill.vx=0;player.X=84;player.Y=61;player.vy256=-512;
                TickActor(bill,default,64);Check("stomp defeats bill",bill.state==1000&&player.vy256>0);
            }
            foreach(int offset in new[]{-39,-24,0,24,39})
            {
                var cannon=CannonFixture();player.X=cannon.x+offset;player.Y=160;
                for(int i=0;i<450;i++)TickActor(cannon,default,player.Y);
                Check("near cannon cannot fire "+offset,!actors.Any(a=>a.data.type=="BulletBill"));
            }
            foreach(int offset in new[]{-40,40})
            {var cannon=CannonFixture();player.X=cannon.x+offset;cannon.age=149;TickActor(cannon,default,player.Y);Check("fires outside reference safe range "+offset,actors.Count(a=>a.data.type=="BulletBill")==1);}
            foreach(int x in new[]{-32,256,280})
            {var cannon=CannonFixture();cannon.x=x;cannon.age=149;TickActor(cannon,default,player.Y);Check("offscreen cannon does not fire "+x,!actors.Any(a=>a.data.type=="BulletBill"));}
            var placed=Spawn(new NodeData{type="BulletBill",group="Character",x=160,y=64,width=16,height=14});float start=placed.x;
            TickActor(placed,default,player.Y);Check("map-placed bill moves without explicit speed",placed.x<start);
            placed.x=300;TickActor(placed,default,player.Y);Check("offscreen bill is cleaned up",!placed.alive);
            foreach(int height in new[]{16,32,48})
            {
                var cannon=CannonFixture(height);RefreshVisuals();
                var top=cannon.visual.GetComponentsInChildren<SpriteRenderer>()[0];var rect=top.sprite.rect;
                Check("muzzle top remains aligned at height "+height,Mathf.Abs(top.bounds.max.y-cannon.Top)<.001f&&rect.yMax==top.sprite.texture.height&&rect.height==Math.Min(height,32));
                Check("cannon remains solid at height "+height,cannon.Solid&&cannon.Bounds.height==height);
                var image=PiranhaPixels();var origin=gameCamera.WorldToScreenPoint(top.bounds.min);var pixels=top.sprite.texture.GetPixels32();int mismatch=0,painted=0;
                for(int y=0;y<(int)rect.height;y++)for(int x=0;x<16;x++)
                {
                    var expected=pixels[((int)rect.y+y)*top.sprite.texture.width+x];if(expected.a==0)continue;painted++;
                    var actual=image[(Mathf.RoundToInt(origin.y)+y)*256+Mathf.RoundToInt(origin.x)+x];
                    if(actual.r!=expected.r||actual.g!=expected.g||actual.b!=expected.b)mismatch++;
                }
                Check("visible cannon cap pixels height "+height,painted>0&&mismatch==0,"pixels="+painted+" mismatches="+mismatch);
            }
            foreach(var course in Levels)
            for(int loc=0;loc<course.locations.Length;loc++)
            {
                var nodes=course.areas[course.locations[loc].area].nodes;
                if(!nodes.Any(n=>n.type=="Cannon"))continue;
                NewGame(course.id);LoadLocation(loc);
                foreach(var cannon in actors.Where(a=>a.data.type=="Cannon").ToArray())
                {
                    cameraX=cannon.x-128;player.X=cannon.x-80;player.Y=160;cannon.age=149;
                    int before=actors.Count(a=>a.data.type=="BulletBill");TickActor(cannon,default,player.Y);
                    Check(course.id+"/"+loc+" cannon "+cannon.x+","+cannon.Top,actors.Count(a=>a.data.type=="BulletBill")==before+1&&actors.Last().vx<0);
                }
            }
            NewGame("1-1");
        }
        IEnumerator CannonSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunCannonAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            foreach(int direction in new[]{-1,1})
            {
                var cannon=CannonFixture();player.X=cannon.x+direction*80;cannon.age=149;TickActor(cannon,default,player.Y);
                var bill=actors.Last();for(int i=0;i<24;i++)TickActor(bill,default,player.Y);
                RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"cannon-fire-"+(direction<0?"left":"right"));
            }
            CannonFixture(16);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"cannon-short-muzzle");
            string result="MARIO_CANNON_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"cannon-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"cannon-result.txt"),result);Debug.Log(result);
        }
    }
}
