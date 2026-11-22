using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioScaleRopeValidation
{
    [MenuItem("Mario/Validate balance platform ropes")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/ScaleRopes");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Scale rope validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        var originals=game.Levels.ToDictionary(l=>l.id,l=>JsonUtility.ToJson(l));
        var lines=new List<string>();int failed=0;
        void Check(string name,bool pass,string detail="")
        {lines.Add((pass?"PASS ":"FAIL ")+name+" "+detail);if(!pass){failed++;Debug.LogError(name+" "+detail);}}
        try
        {
            game.RunScaleRopeAudit(Check);
            game.RunPlatformAudit(Check);
            game.RunBoundaryAudit(Check);
            RenderCheck(game,Check);
            foreach(var level in game.Levels)Check(level.id+" editable level unchanged",originals[level.id]==JsonUtility.ToJson(level));
        }
        catch(Exception e){failed++;lines.Add(e.ToString());throw;}
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            string result=$"MARIO_SCALE_ROPES_{(failed==0?"PASS":"FAIL")} checks={lines.Count} errors={failed}";
            lines.Add(result);File.WriteAllLines("Validation/ScaleRopes/checks.txt",lines);Debug.Log(result);
        }
        if(failed!=0)throw new Exception("Scale rope validation failed: "+failed);
    }

    static void RenderCheck(MarioGame game,Action<string,bool,string> check)
    {
        foreach(string id in new[]{"3-3","J-1-3"})
        {
            game.NewGame(id);
            var pair=game.actors.Where(a=>a.data.inScale).OrderBy(a=>a.x).Take(2).ToArray();
            game.cameraX=pair[0].x-48;
            game.gameCamera.transform.position=new Vector3(game.cameraX+128,88,-10);
            game.RefreshVisuals();Save(game,id+"-initial");
            float oldRaisedTop=pair[1].Top;
            pair[0].y-=32.25f;pair[1].y+=32.25f;game.RefreshVisuals();
            Save(game,id+"-moved");
            // Read only the ropes/platforms and original rope tiles. An old
            // fixed column must not leave a tail under the ascending platform.
            foreach(var a in game.actors)
            {
                bool keep=pair.Contains(a)||pair.Any(p=>p.scaleRope==a)||
                    (a.data.spriteOverride??"").EndsWith("/40");
                if(!keep)foreach(var r in a.visual.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
            }
            var image=Capture(game);var raised=pair[1];
            int x=(int)(raised.scaleRope.x-game.cameraX)+(id.StartsWith("J-")?7:0);
            int y=(int)oldRaisedTop+16+32;
            Color32 pixel=image.GetPixel(x,y),sky=game.gameCamera.backgroundColor;
            check(id+" no old rope tail below raised platform",pixel.r==sky.r&&pixel.g==sky.g&&pixel.b==sky.b,"");
            int downX=(int)(pair[0].scaleRope.x-game.cameraX)+(id.StartsWith("J-")?7:0);
            int downY=(int)pair[0].scaleRope.y+16+32;
            pixel=image.GetPixel(downX,downY);
            check(id+" rope extends into newly exposed span",pixel.r!=sky.r||pixel.g!=sky.g||pixel.b!=sky.b,"");
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
    static void Save(MarioGame game,string name)
    {var image=Capture(game);File.WriteAllBytes("Validation/ScaleRopes/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
    static Texture2D Capture(MarioGame game)
    {
        game.gameCamera.Render();var old=RenderTexture.active;RenderTexture.active=game.gameCamera.targetTexture;
        var image=new Texture2D(256,240,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,240),0,0);image.Apply();RenderTexture.active=old;return image;
    }
}
