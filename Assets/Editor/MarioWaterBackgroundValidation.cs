using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioWaterBackgroundValidation
{
    [MenuItem("Mario/Validate underwater backgrounds")]
    public static void Run()
    {
        const string output="Validation/WaterBackground";Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Water background validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        int failed=0,reference=0,native=0;var lines=new List<string>();
        void Check(string name,bool pass,string detail="")
        {lines.Add((pass?"PASS ":"FAIL ")+name+" "+detail);if(!pass){failed++;Debug.LogError(name+" "+detail);}}
        var originals=game.Levels.ToDictionary(l=>l.id,l=>JsonUtility.ToJson(l));
        try
        {
            foreach(var course in game.Levels)
            for(int loc=0;loc<course.locations.Length;loc++)
            {
                game.NewGame(course.id);game.LoadLocation(loc);
                var backgrounds=game.GetComponentsInChildren<PixelVisual>().Where(v=>v.name=="Underwater background").ToArray();
                if(!game.area.underwater)
                {Check(course.id+"/"+loc+" dry area has no water overlay",backgrounds.Length==0);continue;}
                if(course.Japanese)
                {native++;Check(course.id+"/"+loc+" native tile background retained",backgrounds.Length==0);continue;}
                reference++;
                Check(course.id+"/"+loc+" exactly one water backdrop",backgrounds.Length==1);
                if(backgrounds.Length!=1)continue;
                var backdrop=backgrounds[0];
                Check(course.id+"/"+loc+" water stays behind scenery",backdrop.GetComponentsInChildren<SpriteRenderer>().All(r=>r.sortingOrder<-5));
                foreach(float camera in new[]{0f,127.5f,game.area.width-256})
                {
                    game.cameraX=Mathf.Max(0,camera);game.RefreshVisuals();
                    if(camera==127.5f)
                    {
                        var shot=Capture(game);File.WriteAllBytes(output+"/"+course.id+"-"+loc+".png",shot.EncodeToPNG());UnityEngine.Object.DestroyImmediate(shot);
                    }
                    foreach(var v in game.GetComponentsInChildren<PixelVisual>())if(v!=backdrop)v.Hide(true);
                    var image=Capture(game);
                    var art=PixelLibrary.Find("Scenery","Water",game.area.setting);
                    var top=PixelLibrary.Sprite(art.parts.First(p=>p.name=="top")).texture;
                    var middle=PixelLibrary.Sprite(art.parts.First(p=>p.name=="middle")).texture;
                    int mismatches=0;
                    for(int y=0;y<240;y++)for(int x=0;x<256;x++)
                    {
                        Color32 expected=game.gameCamera.backgroundColor;
                        if(y<208-top.height)expected=middle.GetPixel(x%middle.width,y%middle.height);
                        else if(y<208)
                        {
                            Color32 c=top.GetPixel(x%top.width,y-(208-top.height));
                            if(c.a!=0)expected=c;
                        }
                        Color32 actual=image.GetPixel(x,y);
                        if(actual.r!=expected.r||actual.g!=expected.g||actual.b!=expected.b)mismatches++;
                    }
                    Check(course.id+"/"+loc+" full frame water pixels camera="+camera,mismatches==0,"mismatches="+mismatches);
                    UnityEngine.Object.DestroyImmediate(image);
                    foreach(var v in game.GetComponentsInChildren<PixelVisual>(true))v.Hide(false);
                }
            }
            Check("all underwater areas covered",reference==5&&native==6,"reference="+reference+" native="+native);
            game.RunWaterSurfaceAudit(Check);
            foreach(var level in game.Levels)Check(level.id+" editable data unchanged",originals[level.id]==JsonUtility.ToJson(level));
        }
        catch(Exception e){failed++;lines.Add(e.ToString());throw;}
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            string result=$"MARIO_WATER_BACKGROUND_{(failed==0?"PASS":"FAIL")} checks={lines.Count} errors={failed}";
            lines.Add(result);File.WriteAllLines(output+"/checks.txt",lines);Debug.Log(result);
        }
        if(failed!=0)throw new Exception("Underwater background validation failed: "+failed);
    }
    static Texture2D Capture(MarioGame game)
    {
        game.gameCamera.Render();var previous=RenderTexture.active;RenderTexture.active=game.gameCamera.targetTexture;
        var image=new Texture2D(256,240,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,240),0,0);image.Apply();RenderTexture.active=previous;return image;
    }
}
