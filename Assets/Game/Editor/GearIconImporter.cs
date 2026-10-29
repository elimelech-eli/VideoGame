using UnityEditor;

namespace VideoGame.Editor
{
    public sealed class GearIconImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.EndsWith("/UI/GearIcons.png"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.GUI;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode=UnityEngine.FilterMode.Bilinear;
        }
    }
}
