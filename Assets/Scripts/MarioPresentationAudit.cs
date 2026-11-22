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
        static int RunningCell(string signature)
        {
            string key=signature.Split('/')[0];
            return key.EndsWith(" two")?1:key.EndsWith(" three")?2:0;
        }
        public void RunPresentationAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Presentation: "+n,p,d);
            foreach(int size in new[]{1,2,3})foreach(int direction in new[]{-1,1})
            {
                EffectsArena("Castle");power=size;player.facing=direction;player.grounded=false;
                var positions=new HashSet<float>();
                for(int i=0;i<32;i++)
                {
                    player.X=300+i/8f;cameraX=player.X-112;player.Y=80;RefreshVisuals();
                    positions.Add(gameCamera.WorldToScreenPoint(playerVisual.transform.position).x);
                }
                Check("camera tracking holds player at one pixel size="+size+" facing="+direction,
                    positions.Count==1,"screenX="+string.Join(",",positions));
            }
            foreach(int start in new[]{0,13,39,100})
            {
                EffectsArena();frame=start;int previous=-1,dwell=0,changes=0,backwards=0,shortFrames=0;
                var sequence=new List<int>();
                for(int i=0;i<180;i++)
                {
                    // Repeated walk/run transitions traverse the speed threshold
                    // through real movement, independent of the global frame phase.
                    Step(new Controls{horizontal=1,run=i%32>=16});RefreshVisuals();
                    if(Mathf.Abs(player.VX)<=.05f)continue;
                    int cell=RunningCell(PlayerVisualSignature);sequence.Add(cell);
                    if(previous>=0&&cell!=previous)
                    {
                        if(cell!=(previous+1)%3)backwards++;
                        if(changes>0&&dwell<3)shortFrames++;
                        changes++;dwell=0;
                    }
                    previous=cell;dwell++;
                }
                Check("walk/run changes preserve ordered cycle start="+start,backwards==0&&changes>6,
                    "backwards="+backwards+" changes="+changes+" cells="+string.Join("",sequence));
                Check("walk/run changes never flash a one-tick pose start="+start,shortFrames==0,"shortPoses="+shortFrames);
                string frozen=PlayerVisualSignature;phase=GamePhase.Paused;
                for(int i=0;i<10;i++){Step(default);RefreshVisuals();}
                Check("pause and repeated rendering preserve pose start="+start,PlayerVisualSignature==frozen);
            }
            string eager=null;
            foreach(bool renderEachStep in new[]{true,false})
            {
                EffectsArena();
                for(int i=0;i<53;i++)
                {
                    Step(new Controls{horizontal=1,run=i>=20});
                    if(renderEachStep)RefreshVisuals();
                }
                RefreshVisuals();
                if(renderEachStep)eager=PlayerVisualSignature;
                else Check("catch-up steps have the same animation phase as separate repaints",eager==PlayerVisualSignature);
                string pose=PlayerVisualSignature;for(int i=0;i<12;i++)RefreshVisuals();
                Check("extra high-refresh repaints do not advance animation "+renderEachStep,pose==PlayerVisualSignature);
            }
        }

        // Full-frame comparison also checks transparent texels and the old sprite
        // footprint. Checking only the new opaque texels cannot detect ghost trails.
        bool CheckPlayerFrame(out int mismatches)
        {
            // Read the frame actually produced by LateUpdate. Rendering here
            // would hide stale/partial presentation bugs in the regular path.
            var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(256,240,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,256,240),0,0);image.Apply();
            var actual=image.GetPixels32();Destroy(image);RenderTexture.active=old;
            var expected=Enumerable.Repeat((Color32)gameCamera.backgroundColor,256*240).ToArray();
            var visible=playerVisual.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).ToArray();
            foreach(var r in visible)
            {
                var origin=gameCamera.WorldToScreenPoint(r.bounds.min);
                int x0=Mathf.RoundToInt(origin.x),y0=Mathf.RoundToInt(origin.y),w=r.sprite.texture.width,h=r.sprite.texture.height;
                var source=r.sprite.texture.GetPixels32();
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    var c=source[y*w+(r.transform.lossyScale.x<0?w-1-x:x)];
                    if(c.a==0||x0+x<0||x0+x>=256||y0+y<0||y0+y>=240)continue;
                    expected[(y0+y)*256+x0+x]=c;
                }
            }
            mismatches=0;
            for(int i=0;i<actual.Length;i++)
                if(actual[i].r!=expected[i].r||actual[i].g!=expected[i].g||actual[i].b!=expected[i].b)mismatches++;
            return visible.Length==1&&mismatches==0;
        }
        IEnumerator PresentationSmoke(string output)
        {
            int before=smokeErrors,checks=0;var lines=new List<string>();
            RunPresentationAudit((n,p,d)=>{checks++;lines.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            foreach(int size in new[]{1,2,3})foreach(int direction in new[]{-1,1})
            {
                EffectsArena("Castle");power=size;player.X=800;player.facing=direction;cameraX=688;
                for(int f=0;f<96;f++)
                {
                    Step(new Controls{horizontal=direction,run=f%36>=12,jump=f>=24&&f<44,jumpPressed=f==24});
                    // Exercise the same relative camera movement in both directions.
                    cameraX=player.X-112;invulnerable=0;RefreshVisuals();
                    foreach(var a in actors)a.visual.Hide(true);
                    yield return null;yield return new WaitForEndOfFrame();
                    checks++;SmokeCheck(CheckPlayerFrame(out int errors),"whole moving frame size="+size+" facing="+direction+" tick="+f+" mismatches="+errors);
                    if(!player.grounded){checks++;SmokeCheck(PlayerVisualSignature.Contains("jumping"),"airborne movement holds jump pose");}
                    if(size==2&&direction==1&&(f==18||f==25||f==34||f==60))SaveScreen(output,"motion-"+f);
                }
            }
            string result=$"MARIO_PRESENTATION_{(smokeErrors==before?"PASS":"FAIL")} checks={checks} renderedFrames=576 errors={smokeErrors-before}";
            lines.Add(result);File.WriteAllLines(Path.Combine(output,"presentation-checks.txt"),lines);Debug.Log(result);
        }
        IEnumerator PresentationOnlySmoke(string output)
        {
            Directory.CreateDirectory(output);yield return PresentationSmoke(output);
            Application.logMessageReceived-=SmokeLog;Application.Quit(smokeErrors==0?0:1);
        }
    }
}
