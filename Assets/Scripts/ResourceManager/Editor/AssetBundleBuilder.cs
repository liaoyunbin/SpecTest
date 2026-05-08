using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

public class AssetBundleBuilder
{
    [MenuItem("Tools/AssetBundle/Build AssetBundles")]
    public static void BuildAll()
    {
        string configPath = "Assets/Scripts/ResourceManager/Editor/ab-package-config.json";
        string configJson = File.ReadAllText(configPath);
        var config = JsonUtility.FromJson<BundleBuildConfig>(configJson);

        string outputPath = config.outputPath;
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        CheckResourcesDuplication(config);

        AssignAssetBundleNames(config);

        BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        if (config.compression == "lzma")
            options = BuildAssetBundleOptions.None;

        BuildPipeline.BuildAssetBundles(outputPath, options, EditorUserBuildSettings.activeBuildTarget);

        CheckShaderVariantCollection();

        AssetInfoConfig assetInfoConfig = GenerateAssetInfoConfig(config, outputPath);

        string resourcesPath = "Assets/Resources/AssetInfoConfig.asset";
        AssetDatabase.CreateAsset(assetInfoConfig, resourcesPath);

        string abConfigPath = outputPath + "/asset_config.ab";
        BuildPipeline.BuildAssetBundles(outputPath, new[] {
            new AssetBundleBuild { assetBundleName = "asset_config", assetNames = new[] { resourcesPath } }
        }, options, EditorUserBuildSettings.activeBuildTarget);

        CalculateCRCAndUpdateConfig(assetInfoConfig, outputPath);

        EditorUtility.SetDirty(assetInfoConfig);
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            Debug.LogWarning("[AssetBundleBuilder] Android 平台：请确保 mainTemplate.gradle 中配置 aaptOptions { noCompress '.ab', '.json' }，避免 APK 二次压缩 .ab 文件导致加载性能下降。");
        }

        Debug.Log($"[AssetBundleBuilder] AB 打包完成！输出目录: {outputPath}");
    }

    private static void CheckResourcesDuplication(BundleBuildConfig config)
    {
        string resourcesDir = Application.dataPath + "/Resources";
        if (!Directory.Exists(resourcesDir)) return;

        var resourcesFiles = Directory.GetFiles(resourcesDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .Select(f => "Assets/Resources/" + f.Substring(resourcesDir.Length + 1).Replace("\\", "/"))
            .ToHashSet();

        var abFiles = new HashSet<string>();
        foreach (var bundle in config.bundles)
        {
            foreach (var asset in bundle.assets)
                abFiles.Add(asset);
        }

        var duplicates = resourcesFiles.Intersect(abFiles).ToList();
        if (duplicates.Any())
        {
            foreach (var dup in duplicates)
                Debug.LogError($"[AssetBundleBuilder] 资源 {dup} 同时存在于 Resources 和 AB 中，将导致包体膨胀！");
            EditorApplication.Exit(1);
        }
    }

    private static void AssignAssetBundleNames(BundleBuildConfig config)
    {
        foreach (var bundle in config.bundles)
        {
            foreach (var asset in bundle.assets)
            {
                var importer = AssetImporter.GetAtPath(asset);
                if (importer != null)
                {
                    importer.assetBundleName = bundle.name;
                    importer.assetBundleVariant = null;
                }
            }
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
    }

    private static void CheckShaderVariantCollection()
    {
        var svc = ShaderVariantCollectionHelper.GetRegisteredSVC();
        if (svc == null || svc.variantCount == 0)
        {
            Debug.LogWarning("[AssetBundleBuilder] GraphicsSettings 中未注册 ShaderVariantCollection 或 SVC 为空，可能导致 Shader 变体缺失（粉色材质）。");
        }
    }

    private static AssetInfoConfig GenerateAssetInfoConfig(BundleBuildConfig config, string outputPath)
    {
        var manifest = AssetDatabase.LoadAssetAtPath<AssetBundleManifest>(outputPath + "/AssetBundleManifest");
        var configSO = ScriptableObject.CreateInstance<AssetInfoConfig>();

        foreach (var bundle in config.bundles)
        {
            foreach (var assetPath in bundle.assets)
            {
                var info = new AssetInfo();
                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                info.assetName = assetName;
                info.bundleName = bundle.name;
                info.assetPath = assetPath;
                info.assetType = GetAssetType(assetPath);

                string resourcesPath = TryGetResourcesPath(assetPath);
                if (resourcesPath != null)
                    info.resourcesPath = resourcesPath;

                if (manifest != null)
                {
                    string[] deps = manifest.GetAllDependencies(bundle.name);
                    info.dependencies = deps.ToList();
                }

                info.referencedBundles = CalculateReferencedBundles(assetPath, config, manifest);

                info.subAssets = CollectSubAssets(assetPath);

                configSO.assetInfos.Add(info);
            }
        }

        return configSO;
    }

    private static List<string> CalculateReferencedBundles(string assetPath, BundleBuildConfig config, AssetBundleManifest manifest)
    {
        var result = new HashSet<string>();
        var dependencies = AssetDatabase.GetDependencies(assetPath, true);

        foreach (var dep in dependencies)
        {
            if (dep == assetPath) continue;
            var importer = AssetImporter.GetAtPath(dep);
            if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
            {
                result.Add(importer.assetBundleName);
            }
        }

        return result.ToList();
    }

    private static List<string> CollectSubAssets(string assetPath)
    {
        var subAssets = new List<string>();
        var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

        if (mainType == typeof(GameObject) || mainType == typeof(Texture2D) || mainType == typeof(SpriteAtlas))
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.name))
                    subAssets.Add(asset.name);
            }
        }

        return subAssets;
    }

    private static string GetAssetType(string assetPath)
    {
        var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
        return type != null ? type.Name : "Unknown";
    }

    private static string TryGetResourcesPath(string assetPath)
    {
        string resourcesMarker = "/Resources/";
        int idx = assetPath.IndexOf(resourcesMarker);
        if (idx >= 0)
        {
            string relative = assetPath.Substring(idx + resourcesMarker.Length);
            return Path.GetFileNameWithoutExtension(relative);
        }
        return null;
    }

    private static void CalculateCRCAndUpdateConfig(AssetInfoConfig config, string outputPath)
    {
        var bundleFiles = Directory.GetFiles(outputPath, "*.ab");
        var crcMap = new Dictionary<string, uint>();

        foreach (var file in bundleFiles)
        {
            string bundleName = Path.GetFileNameWithoutExtension(file);
            uint crc = CRC32.Compute(File.ReadAllBytes(file));
            crcMap[bundleName] = crc;
        }

        foreach (var info in config.assetInfos)
        {
            if (crcMap.TryGetValue(info.bundleName, out uint crc))
            {
                info.crc = crc;
            }
        }
    }

    [MenuItem("Tools/AssetBundle/Clear AssetBundle Names")]
    public static void ClearAllAssetBundleNames()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        Debug.Log("[AssetBundleBuilder] 已清除所有 AssetBundle 名称。");
    }
}

[Serializable]
public class BundleBuildConfig
{
    public string outputPath;
    public string buildTarget;
    public string compression;
    public List<BundleConfigEntry> bundles;
}

[Serializable]
public class BundleConfigEntry
{
    public string name;
    public bool isPermanent;
    public List<string> assets;
}

public static class ShaderVariantCollectionHelper
{
    public static ShaderVariantCollection GetRegisteredSVC()
    {
        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        if (graphicsSettings == null) return null;

        var svcField = typeof(GraphicsSettings).GetField("m_ShaderVariantCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (svcField != null)
        {
            return svcField.GetValue(graphicsSettings) as ShaderVariantCollection;
        }

        return null;
    }
}

public static class CRC32
{
    private static readonly uint[] Table = GenerateTable();

    private static uint[] GenerateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    public static uint Compute(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return ~crc;
    }
}
