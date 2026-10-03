using UnityEditor;
using UnityEngine;
namespace Ascension.Editor
{
    // Game-owned import policy. Executes during the public UnityClient build.
    public sealed class PortfolioAssetImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.Contains("Ascension/Presentation/Resources/Art/Cultivator"))return;
            var m=(ModelImporter)assetImporter;m.animationType=ModelImporterAnimationType.Legacy;
            m.importAnimation=true;m.isReadable=true;m.addCollider=false;m.materialImportMode=ModelImporterMaterialImportMode.None;
        }
        void OnPreprocessAnimation()
        {
            if(!assetPath.Contains("Ascension/Presentation/Resources/Art/Cultivator"))return;
            var m=(ModelImporter)assetImporter;var clips=m.defaultClipAnimations;
            foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}m.clipAnimations=clips;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("Ascension/Presentation/Resources/Art/"))return;
            var t=(TextureImporter)assetImporter;t.maxTextureSize=2048;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
