using Mario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MarioFinishAudioValidation
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Finish audio validation");var game=root.AddComponent<MarioGame>();game.validationMode=true;game.Initialize();
        int count=0,failed=0;
        try{game.RunFinishAudioAudit((n,p,d)=>{count++;if(!p)failed++;Debug.Log((p?"PASS ":"FAIL ")+n+" "+d);});}
        finally{Object.DestroyImmediate(root);}
        Debug.Log($"MARIO_FINISH_AUDIO_{(failed==0?"PASS":"FAIL")} checks={count} errors={failed}");EditorApplication.Exit(failed==0?0:1);
    }
}
