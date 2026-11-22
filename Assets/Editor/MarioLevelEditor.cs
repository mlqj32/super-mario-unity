using System.Collections.Generic;
using System.Linq;
using Mario;
using UnityEditor;
using UnityEngine;

public sealed class MarioLevelEditor : EditorWindow
{
    LevelData selected,previewLevel; int areaIndex,world,previewArea,campaign; GameObject previewRoot; Vector2 scroll;
    [MenuItem("Mario/Level Editor")]
    public static void Open(){GetWindow<MarioLevelEditor>("马里奥 · 64关");}
    void OnGUI()
    {
        EditorGUILayout.LabelField("超级马里奥 — 初代32关 + 日版二代32关",EditorStyles.boldLabel);
        campaign=GUILayout.Toolbar(campaign,new[]{"初代","日版二代"});
        world=GUILayout.Toolbar(world,Enumerable.Range(1,8).Select(i=>"世界 "+i).ToArray());
        using(new EditorGUILayout.HorizontalScope())for(int s=1;s<=4;s++)if(GUILayout.Button((world+1)+"-"+s))
        {selected=AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Resources/Levels/"+(campaign==1?"J-":"")+(world+1)+"-"+s+".asset");areaIndex=0;Selection.activeObject=selected;}
        selected=(LevelData)EditorGUILayout.ObjectField("可编辑关卡",selected,typeof(LevelData),false);
        if(selected==null)return;
        areaIndex=EditorGUILayout.Popup("区域",Mathf.Clamp(areaIndex,0,selected.areas.Length-1),selected.areas.Select((a,i)=>i+" · "+a.setting).ToArray());
        EditorGUILayout.HelpBox("生成场景预览后，可以移动对象，在 Inspector 中编辑 Node 属性。应用编辑会写回当前关卡资源。坐标单位为原版像素。日版二代的地形格子也可逐个编辑。",MessageType.Info);
        if(GUILayout.Button("生成场景预览"))Preview();
        using(new EditorGUI.DisabledScope(previewRoot==null||previewLevel!=selected||previewArea!=areaIndex))
            if(GUILayout.Button("应用场景编辑到关卡"))Apply();
        if(GUILayout.Button("试玩本关"))
        {ClearPreview();var game=Object.FindAnyObjectByType<MarioGame>();if(game!=null){game.startingLevel=selected;game.startImmediately=true;EditorUtility.SetDirty(game);}EditorApplication.isPlaying=true;}
        if(GUILayout.Button("选中关卡资源"))Selection.activeObject=selected;
        EditorGUILayout.LabelField("对象数量",selected.areas[areaIndex].nodes.Length.ToString());
        if(GUILayout.Button("在场景视图中心添加砖块"))
        {
            var n=new NodeData{type="Brick",group="Solid",width=16,height=16,x=SceneView.lastActiveSceneView!=null?Mathf.Round(SceneView.lastActiveSceneView.pivot.x/16)*16:128,y=64,entrance=-1,targetLocation=-1};
            Undo.RecordObject(selected,"Add brick");var list=selected.areas[areaIndex].nodes.ToList();list.Add(n);selected.areas[areaIndex].nodes=list.ToArray();EditorUtility.SetDirty(selected);Preview();
        }
    }
    void Preview()
    {
        if(EditorApplication.isPlaying)return;
        ClearPreview();previewLevel=selected;previewArea=areaIndex;
        previewRoot=new GameObject("EDITOR PREVIEW · "+selected.id+" · area "+areaIndex){hideFlags=HideFlags.DontSave};
        var data=selected.areas[areaIndex];
        if(data.NeedsWaterBackdrop)
        {
            var water=new GameObject("Underwater background");water.transform.SetParent(previewRoot.transform,false);
            water.transform.localPosition=new Vector3(0,-32,0);
            water.AddComponent<PixelVisual>().Set("Scenery","Water",data.setting,Mathf.Max(256,data.width),208,-10);
        }
        for(int i=0;i<data.nodes.Length;i++)
        {
            var n=data.nodes[i];var go=new GameObject(i.ToString("D3")+" · "+n.type);go.transform.SetParent(previewRoot.transform);go.transform.position=new Vector3(n.x,n.y-n.height,0);
            var handle=go.AddComponent<LevelNodeHandle>();handle.node=n.Copy();handle.sourceIndex=i;
            var visual=go.AddComponent<PixelVisual>();
            // Keep the editable trigger handle without painting over the map's pole.
            if(data.HasFlagPoleArtwork(n))continue;
            if(n.type=="CustomText")visual.SetText(n.text,n.height);
            else if(!string.IsNullOrEmpty(n.spriteOverride))visual.SetTile(n.spriteOverride,n.group=="Scenery"?-5:0,n.width,n.height);
            else visual.Set(n.group,n.type,data.setting+" one",n.width,n.height,n.group=="Scenery"?-5:0,false,n.hidden?new Color(1,1,1,.3f):Color.white);
        }
        if(SceneView.lastActiveSceneView!=null){SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(new Vector3(128,80,0),Quaternion.identity,150,true);}
    }
    void Apply()
    {
        // Changing the selected level must never apply an older area's objects
        // into the newly selected asset.
        if(previewRoot==null||selected==null||selected!=previewLevel||areaIndex!=previewArea)return;
        Undo.RecordObject(selected,"Apply Mario scene edits");
        var nodes=new List<NodeData>();
        foreach(var handle in previewRoot.GetComponentsInChildren<LevelNodeHandle>().OrderBy(n=>n.sourceIndex))
        {var n=handle.node.Copy();n.x=Mathf.Round(handle.transform.position.x);n.y=Mathf.Round(handle.transform.position.y+n.height);nodes.Add(n);}
        selected.areas[areaIndex].nodes=nodes.ToArray();EditorUtility.SetDirty(selected);AssetDatabase.SaveAssets();
        Debug.Log("Saved "+selected.id+" area "+areaIndex+" / "+nodes.Count+" editable nodes");
    }
    void ClearPreview(){if(previewRoot!=null)DestroyImmediate(previewRoot);previewRoot=null;previewLevel=null;}
    void OnEnable(){EditorApplication.playModeStateChanged+=OnPlayMode;}
    void OnPlayMode(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode)ClearPreview();}
    void OnDisable(){EditorApplication.playModeStateChanged-=OnPlayMode;ClearPreview();}
}
