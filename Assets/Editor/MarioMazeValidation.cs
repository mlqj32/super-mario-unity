using Mario;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioMazeValidation
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Maze validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        int count=0,failed=0;
        try{game.RunMazeAudit((name,pass,detail)=>{count++;if(!pass)failed++;Debug.Log((pass?"PASS ":"FAIL ")+name+" "+detail);});}
        finally{Object.DestroyImmediate(root);}
        Debug.Log($"MARIO_MAZE_{(failed==0?"PASS":"FAIL")} checks={count} errors={failed}");
        UnityEditor.EditorApplication.Exit(failed==0?0:1);
    }
}
