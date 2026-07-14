#if UNITY_EDITOR
using UnityEditor;

namespace Fjordfall.EditorTools
{
    /// <summary>Consistent mobile import settings for the bundled standalone OBJ/PNG asset pack.</summary>
    public sealed class FjordfallExternalAssetImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/External/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (assetPath.EndsWith("_normal.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.convertToNormalmap = false;
            }
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root + "Models/")) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
        }
    }
}
#endif
