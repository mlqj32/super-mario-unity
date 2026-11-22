using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioWaterValidation
{
    [MenuItem("Mario/Validate swimming surface")]
    public static void Run()
    {
        const string output="Validation/WaterSurface";Directory.CreateDirectory(output);
        bool before=Environment.GetCommandLineArgs().Contains("-mario-water-before");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Water surface validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        var originals=game.Levels.ToDictionary(l=>l.id,l=>JsonUtility.ToJson(l));
        int failed=0;var lines=new List<string>();
        void Check(string name,bool pass,string detail="")
        {lines.Add((pass?"PASS ":"FAIL ")+name+" "+detail);if(!pass){failed++;Debug.LogWarning(name+" "+detail);}}
        try
        {
            game.RunWaterSurfaceAudit(Check);
            if(!before)game.RunBoundaryAudit(Check);
            foreach(string id in new[]{"2-2","J-3-2"})
            foreach(int size in new[]{1,2})
            {
                game.NewGame(id);int loc=Array.FindIndex(game.level.locations,l=>game.level.areas[l.area].underwater);game.LoadLocation(loc);
                foreach(var a in game.actors)if(a.IsEnemy)a.alive=false;
                game.power=size;game.player.X=160;game.cameraX=64;game.player.Y=176-game.PlayerHeight-2;game.player.grounded=false;game.player.vy256=0;
                for(int f=0;f<180;f++)game.Step(new Controls{jump=f%8<4,jumpPressed=f%8==0});
                game.RefreshVisuals();game.gameCamera.Render();
                var previous=RenderTexture.active;RenderTexture.active=game.gameCamera.targetTexture;
                var image=new Texture2D(256,240,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,256,240),0,0);image.Apply();
                File.WriteAllBytes(output+"/"+id+"-power"+size+(before?"-before":"-after")+".png",image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
            }
            foreach(var level in game.Levels)Check(level.id+" editable level unchanged",originals[level.id]==JsonUtility.ToJson(level));
        }
        catch(Exception e){failed++;lines.Add(e.ToString());throw;}
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            string result=$"MARIO_WATER_SURFACE_{(failed==0?"PASS":"FAIL")} checks={lines.Count} errors={failed}";
            lines.Add(result);File.WriteAllLines(output+(before?"/before-checks.txt":"/checks.txt"),lines);Debug.Log(result);
        }
        if(failed!=0)throw new Exception("Water surface validation failed: "+failed);
    }
}
