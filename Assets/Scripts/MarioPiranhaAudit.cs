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
        Actor PiranhaFixture(int variant)
        {
            bool japanese=variant>0,down=variant==2;
            var course=Levels.First(l=>l.Japanese==japanese&&l.locations.Any(loc=>
                l.areas[loc.area].nodes.Any(n=>n.type=="Piranha"&&(n.action=="upside-down")==down)));
            int locIndex=Array.FindIndex(course.locations,loc=>course.areas[loc.area].nodes.Any(n=>n.type=="Piranha"&&(n.action=="upside-down")==down));
            NewGame(course.id);LoadLocation(locIndex);
            var plant=actors.First(a=>a.data.type=="Piranha"&&(a.data.action=="upside-down")==down);
            foreach(var other in actors.Where(a=>a.IsEnemy&&a!=plant))other.alive=false;
            player.X=plant.x-80;player.Y=0;cameraX=Mathf.Max(0,plant.x-128);plant.active=true;
            return plant;
        }
        void PiranhaPose(Actor plant,int pose)
        {
            // Use the actual movement code at hidden, halfway, and extended ages.
            plant.age=pose==0?0:pose==1?(level.Japanese?47:63):(level.Japanese?79:95);
            TickActor(plant,default,player.Y);RefreshVisuals();
        }
        Color32[] PiranhaPixels()
        {
            gameCamera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(256,240,TextureFormat.RGBA32,false);
            texture.ReadPixels(new Rect(0,0,256,240),0,0);texture.Apply();var pixels=texture.GetPixels32();
            RenderTexture.active=previous;
            if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);
            return pixels;
        }
        public void RunPiranhaAudit(Action<string,bool,string> record)
        {
            for(int variant=0;variant<3;variant++)
            {
                var plant=PiranhaFixture(variant==0?0:1);plant.data.red=variant==2;
                int range=variant==2?19:33;
                foreach(int side in new[]{-1,1})
                {
                    plant.age=0;player.X=plant.x+side*(range-1);player.Y=plant.originY-80;
                    bool hidden=true;
                    for(int i=0;i<900;i++){TickActor(plant,default,player.Y);hidden&=Mathf.Abs(plant.y-(plant.originY-plant.h))<.001f;}
                    record("Piranha: nearby player blocks multiple cycles "+variant+"/"+side,hidden,"age="+plant.age);
                    player.X=plant.x+side*range;float previous=plant.y;bool smooth=true;
                    for(int i=0;i<17;i++){TickActor(plant,default,player.Y);smooth&=plant.y>=previous&&plant.y-previous<=1;previous=plant.y;}
                    record("Piranha: departure at reference boundary rises smoothly "+variant+"/"+side,smooth&&plant.y>plant.originY-plant.h&&plant.y<plant.originY,"");
                    player.X=plant.x+side*(range-1);float before=plant.y;TickActor(plant,default,player.Y);
                    record("Piranha: approaching an emerging plant does not teleport it "+variant+"/"+side,plant.y>=before&&plant.y-before<=1,"");
                    for(int i=0;i<360;i++)TickActor(plant,default,player.Y);
                    record("Piranha: completes cycle then stays hidden nearby "+variant+"/"+side,Mathf.Abs(plant.y-(plant.originY-plant.h))<.001f,"");
                }
            }
            var inverted=PiranhaFixture(2);player.X=inverted.x;player.Y=inverted.originY-100;inverted.age=0;
            for(int i=0;i<80;i++)TickActor(inverted,default,player.Y);
            record("Piranha: Japanese inverted plant emerges even with player beneath",Mathf.Abs(inverted.y-inverted.originY)<.001f,"");
            for(int variant=0;variant<3;variant++)
            {
                var plant=PiranhaFixture(variant);bool down=variant==2;
                float mouth=down?plant.originY+plant.h:plant.originY;
                string label=level.id+(down?" inverted":" upright");
                record("Piranha: "+label+" anchor matches pipe opening",actors.Any(a=>a.Solid&&a.x<plant.x+plant.w&&a.x+a.w>plant.x&&Mathf.Abs((down?a.y:a.Top)-mouth)<.01f),"mouth="+mouth);
                for(int pose=0;pose<3;pose++)
                {
                    PiranhaPose(plant,pose);var shown=PiranhaPixels();plant.visual.Hide(true);var absent=PiranhaPixels();plant.visual.Hide(false);
                    int leaked=0,exposed=0;
                    for(int i=0;i<shown.Length;i++)
                    {
                        if(shown[i].Equals(absent[i]))continue;
                        float worldY=gameCamera.transform.position.y-120+i/256+.5f;
                        if(down?worldY>=mouth:worldY<mouth)leaked++;else exposed++;
                    }
                    record("Piranha: "+label+" pose="+pose+" pipe occlusion",leaked==0,"leakedPixels="+leaked);
                    record("Piranha: "+label+" pose="+pose+" exposed artwork",pose==0?exposed==0:exposed>0,"exposedPixels="+exposed);
                }
            }
            NewGame("1-1");
        }
        IEnumerator PiranhaSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunPiranhaAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            for(int variant=0;variant<3;variant++)
            {
                var plant=PiranhaFixture(variant);
                for(int pose=0;pose<3;pose++)
                {PiranhaPose(plant,pose);yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"piranha-"+variant+"-"+pose);}
            }
            var nearby=PiranhaFixture(0);nearby.age=0;player.X=nearby.x-28;player.Y=nearby.originY-48;
            for(int i=0;i<900;i++)TickActor(nearby,default,player.Y);
            RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"piranha-nearby-stays-hidden");
            string result="MARIO_PIRANHA_"+(before==smokeErrors?"PASS":"FAIL")+" checks="+count+" errors="+(smokeErrors-before);
            File.WriteAllLines(Path.Combine(output,"piranha-checks.txt"),checks);File.WriteAllText(Path.Combine(output,"piranha-result.txt"),result);Debug.Log(result);
        }
    }
}
