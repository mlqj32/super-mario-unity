using System;
using System.IO;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PixelImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Pixels/")&&!assetPath.StartsWith("Assets/Resources/Font/")&&!assetPath.StartsWith("Assets/Resources/JapanTiles/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;
        t.spritePixelsPerUnit=1;t.spriteImportMode=SpriteImportMode.Single;
        t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.isReadable=true;
    }
    void OnPreprocessAudio()
    {var a=(AudioImporter)assetImporter;var s=a.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.PCM;a.defaultSampleSettings=s;}
}
public static class ProjectSetup
{
    // Add only the new campaign; preserve all existing user-edited assets.
    public static void ImportJapanese()
    {
        int count=0;
        foreach(string file in Directory.GetFiles("Assets/Resources/LevelData","J-*.json"))
        {
            var document=JsonUtility.FromJson<LevelDocument>(File.ReadAllText(file));
            string path="Assets/Resources/Levels/"+document.id+".asset";
            var level=AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if(level!=null&&!Environment.GetCommandLineArgs().Contains("-mario-refresh-japan-reference"))continue;
            bool create=level==null;if(create)level=ScriptableObject.CreateInstance<LevelData>();
            level.id=document.id;level.timeLimit=document.timeLimit;level.locations=document.locations;level.areas=document.areas;
            if(create)AssetDatabase.CreateAsset(level,path);else EditorUtility.SetDirty(level);count++;
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("MARIO_JAPANESE_IMPORT_PASS added="+count);
    }
    [MenuItem("Mario/Restore reference levels (overwrites edits)")]
    public static void RestoreReference()
    {if(EditorUtility.DisplayDialog("Restore 32 levels","This replaces all edited level assets with the bundled reference data. Continue?","Restore","Cancel"))Import();}
    public static void Import()
    {
        Directory.CreateDirectory("Assets/Resources/Levels");
        for(int world=1;world<=8;world++)for(int stage=1;stage<=4;stage++)
        {
            string id=world+"-"+stage,path="Assets/Resources/Levels/"+id+".asset";
            var document=JsonUtility.FromJson<LevelDocument>(File.ReadAllText("Assets/Resources/LevelData/"+id+".json"));
            var level=AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if(level==null){level=ScriptableObject.CreateInstance<LevelData>();AssetDatabase.CreateAsset(level,path);}
            level.id=document.id;level.timeLimit=document.timeLimit;level.locations=document.locations;level.areas=document.areas;EditorUtility.SetDirty(level);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("MARIO_IMPORT_PASS levels=32");
    }
    public static void Configure()
    {
        Import();
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Pixels"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var t=(TextureImporter)AssetImporter.GetAtPath(path);
            if(t.textureType!=TextureImporterType.Sprite||t.filterMode!=FilterMode.Point||t.textureCompression!=TextureImporterCompression.Uncompressed)t.SaveAndReimport();
        }
        PlayerSettings.companyName="Local Projects";PlayerSettings.productName="超级马里奥";
        PlayerSettings.defaultScreenWidth=1024;PlayerSettings.defaultScreenHeight=960;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        QualitySettings.antiAliasing=0;QualitySettings.vSyncCount=0;
        Time.fixedDeltaTime=(float)(1/ClassicMotor.Frequency);
        EditorSettings.serializationMode=SerializationMode.ForceText;
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Super Mario Bros. / Native Unity");var game=root.AddComponent<MarioGame>();
        game.startingLevel=AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Resources/Levels/1-1.asset");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
        AssetDatabase.SaveAssets();Debug.Log("MARIO_SETUP_PASS Unity="+Application.unityVersion);
    }
    [MenuItem("Mario/Build Windows player")]
    public static void Build()
    {
        // Keep maximize available. Manual sizing is blocked by the runtime
        // Windows policy, instead of removing the window's maximize controls.
        PlayerSettings.defaultScreenWidth=768;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow=true;PlayerSettings.allowFullscreenSwitch=false;
        AssetDatabase.SaveAssets();
        // An alternate destination allows validation while the existing player
        // is open, without closing the user's game or replacing loaded files.
        var args=Environment.GetCommandLineArgs();int outputArg=Array.IndexOf(args,"-mario-build-output");
        string output=outputArg>=0&&outputArg+1<args.Length?args[outputArg+1]:"Builds/Windows";
        Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName=Path.Combine(output,"超级马里奥.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        string result="MARIO_BUILD_"+(report.summary.result==BuildResult.Succeeded?"PASS":"FAIL")+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" bytes="+report.summary.totalSize;
        Directory.CreateDirectory("Validation");File.WriteAllText("Validation/build-result.txt",result);Debug.Log(result);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception(result);
    }
}
