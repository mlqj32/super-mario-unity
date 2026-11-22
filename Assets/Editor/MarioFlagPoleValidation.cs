using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioFlagPoleValidation
{
    [MenuItem("Mario/Validate flagpole rendering")]
    public static void Run()
    {
        const string output="Validation/FlagPole";
        bool renderOnly=Environment.GetCommandLineArgs().Contains("-mario-flagpole-render-only");
        Directory.CreateDirectory(output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Flagpole validation");
        var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        var lines=new List<string>();int failed=0,tiled=0,classic=0;
        void Check(string name,bool pass,string detail="")
        {
            lines.Add((pass?"PASS ":"FAIL ")+name+" "+detail);
            if(!pass){failed++;Debug.LogError(name+" "+detail);}
        }
        var originals=game.Levels.ToDictionary(l=>l.id,l=>JsonUtility.ToJson(l));
        try
        {
            foreach(var level in game.Levels)
            for(int ai=0;ai<level.areas.Length;ai++)
            {
                if(!level.areas[ai].nodes.Any(n=>n.type=="FlagPole"))continue;
                int location=Array.FindIndex(level.locations,l=>l.area==ai);
                if(location<0)continue;
                game.NewGame(level.id);game.LoadLocation(location);
                foreach(var pole in game.actors.Where(a=>a.data.type=="FlagPole").ToArray())
                {
                    game.cameraX=Mathf.Floor(pole.x)-120;
                    game.gameCamera.transform.position=new Vector3(game.cameraX+128,88,-10);
                    game.RefreshVisuals();
                    bool native=game.area.HasFlagPoleArtwork(pole.data);
                    Check(level.id+" goal trigger stays alive",pole.alive);
                    Check(level.id+" pole rendering",pole.visual.gameObject.activeSelf!=native);
                    Check(level.id+" flag remains visible",game.actors.Any(a=>a.data.type=="Flag"&&a.visual.gameObject.activeSelf));
                    if(native)
                    {
                        tiled++;
                        var cap=game.actors.First(a=>a.data.type=="JTile"&&a.data.spriteOverride.EndsWith("/21")&&a.x<=pole.x&&a.x+16>pole.x);
                        var frame=Capture(game);
                        var tex=Resources.Load<Texture2D>(cap.data.spriteOverride);
                        int mismatches=0;
                        for(int y=0;y<16;y++)for(int x=0;x<16;x++)
                        {
                            Color32 expected=tex.GetPixel(x,y);
                            if(expected.a==0)expected=game.gameCamera.backgroundColor;
                            Color32 actual=frame.GetPixel((int)(cap.x-game.cameraX)+x,(int)cap.y+32+y);
                            if(actual.r!=expected.r||actual.g!=expected.g||actual.b!=expected.b)mismatches++;
                        }
                        Check(level.id+" cap matches original map pixels",mismatches==0,"mismatches="+mismatches);
                        if(level.id=="J-3-1")
                        {
                            File.WriteAllBytes(output+"/J-3-1-after.png",frame.EncodeToPNG());
                            // Reproduce the old duplicate layer for a reviewable before image.
                            pole.visual.Hide(false);
                            pole.visual.transform.localPosition=new Vector3(pole.x,pole.y,0);
                            pole.visual.Set("Scenery","FlagPole",game.area.setting,pole.w,pole.h,-5);
                            var before=Capture(game);File.WriteAllBytes(output+"/J-3-1-before.png",before.EncodeToPNG());
                            Check("Before image reproduces the duplicate pole",!before.GetPixels32().SequenceEqual(frame.GetPixels32()));
                            UnityEngine.Object.DestroyImmediate(before);game.RefreshVisuals();
                            var moved=pole.data.Copy();moved.x+=32;
                            var custom=game.Spawn(moved);game.RefreshVisuals();
                            Check("Moved custom pole uses standalone art",custom.visual.gameObject.activeSelf);
                            custom.alive=false;
                        }
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                    else classic++;
                }
            }
            Check("Japanese and classic goal coverage",tiled>=24&&classic==24,"tiled="+tiled+" classic="+classic);
            // Covers grabbing, lowering the flag, walking to the castle and changing levels.
            if(!renderOnly)game.RunCastleEntryAudit(Check);
            foreach(var level in game.Levels)
                Check(level.id+" editable level unchanged",originals[level.id]==JsonUtility.ToJson(level));
        }
        catch(Exception error){failed++;lines.Add(error.ToString());throw;}
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            string result=$"MARIO_FLAGPOLE_{(failed==0?"PASS":"FAIL")} checks={lines.Count} errors={failed}";
            lines.Add(result);File.WriteAllLines(output+(renderOnly?"/render-checks.txt":"/checks.txt"),lines);Debug.Log(result);
        }
        if(failed!=0)throw new Exception("Flagpole validation failed: "+failed);
    }

    static Texture2D Capture(MarioGame game)
    {
        game.gameCamera.Render();var previous=RenderTexture.active;
        RenderTexture.active=game.gameCamera.targetTexture;
        var image=new Texture2D(256,240,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,256,240),0,0);image.Apply();
        RenderTexture.active=previous;return image;
    }
}
