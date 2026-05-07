#if RESOURCE_DEBUG
using System.Collections.Generic;
using UnityEngine;

public class ResourceDebugUI : MonoBehaviour
{
    private Vector2 _scrollPosition;
    private bool _showBundles = true;
    private bool _showAssets = true;
    private Dictionary<string, bool> _bundleExpanded = new Dictionary<string, bool>();

    private void OnGUI()
    {
        if (!ResourceManager.Instance) return;

        GUI.Box(new Rect(10, 10, 500, 600), "ResourceManager Debug");

        GUILayout.BeginArea(new Rect(15, 30, 490, 575));
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        DrawProviderInfo();
        DrawMemorySummary();
        DrawBundleList();
        DrawAssetList();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawProviderInfo()
    {
        var provider = ResourceManager.Instance.CurrentProvider;
        if (provider != null)
        {
            GUILayout.Label($"Provider: {provider.GetType().Name}");
            GUILayout.Label($"Debug: {provider.GetDebugInfo()}");
        }
        GUILayout.Space(5);
    }

    private void DrawMemorySummary()
    {
        var summary = ResourceManager.Instance.GetMemorySummary();
        GUILayout.Label($"已加载 AB 数: {summary.totalBundles}");
        GUILayout.Label($"总内存占用: {summary.GetMemoryString()}");
        GUILayout.Space(5);
    }

    private void DrawBundleList()
    {
        _showBundles = GUILayout.Toggle(_showBundles, "已加载 AB 列表");
        if (!_showBundles) return;

        var bundles = ResourceManager.Instance.GetLoadedBundles();
        foreach (var bundle in bundles)
        {
            bool expanded = false;
            if (!_bundleExpanded.ContainsKey(bundle.bundleName))
                _bundleExpanded[bundle.bundleName] = false;

            expanded = GUILayout.Toggle(_bundleExpanded[bundle.bundleName],
                $"{bundle.bundleName} | ref:{bundle.refCount} inst:{bundle.instanceCount} | {(bundle.isPermanent ? "永久" : "临时")} | {bundle.estimatedSize / 1024}KB");

            _bundleExpanded[bundle.bundleName] = expanded;

            if (expanded)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label($"加载耗时: {bundle.loadDuration:F3}s");
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.Space(5);
    }

    private void DrawAssetList()
    {
        _showAssets = GUILayout.Toggle(_showAssets, "已缓存资源列表");
        if (!_showAssets) return;

        var assets = ResourceManager.Instance.GetLoadedAssets();
        foreach (var asset in assets)
        {
            GUILayout.Label($"  {asset.assetName} | type:{asset.type} | ref:{asset.refCount} | state:{asset.state}");
        }
    }
}
#endif
