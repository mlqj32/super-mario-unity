using UnityEditor;
using UnityEngine;

public sealed class SyobonAssetImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Syobon/"))return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Default;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;
        t.npotScale=TextureImporterNPOTScale.None;t.alphaIsTransparency=true;t.isReadable=true;
    }
}
