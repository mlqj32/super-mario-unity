using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SyobonValidation
{
    public static void ValidateAndBuild(){MarioValidation.Run();ProjectSetup.Build();}
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Syobon validation");var checks=new List<string>();int failed=0;
        try
        {
            using(var game=new Mario.Syobon.Engine(root))
            {
                game.RunAudit((n,p,d)=>{checks.Add((p?"PASS ":"FAIL ")+n+" "+d);if(!p){failed++;Debug.LogError(n+" "+d);}},"Validation/Syobon");
            }
        }
        finally{Object.DestroyImmediate(root);Directory.CreateDirectory("Validation/Syobon");File.WriteAllLines("Validation/Syobon/checks.txt",checks);}
        string result=$"SYOBON_VALIDATION_{(failed==0?"PASS":"FAIL")} checks={checks.Count} errors={failed}";
        File.WriteAllText("Validation/Syobon/result.txt",result);Debug.Log(result);EditorApplication.Exit(failed==0?0:1);
    }
}
