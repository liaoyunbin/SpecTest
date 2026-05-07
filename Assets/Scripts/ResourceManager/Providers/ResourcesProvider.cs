using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesProvider : IResourceProvider
{
    private AssetInfoConfig _config;
    private Dictionary<string, AssetRef> _assetRefs = new Dictionary<string, AssetRef>();

    public void Initialize()
    {
        _config = Resources.Load<AssetInfoConfig>("AssetInfoConfig");
        _assetRefs.Clear();
        Debug.Log("[ResourcesProvider] 初始化完成（Resources 模式）");
    }

    public void Cleanup()
    {
        _assetRefs.Clear();
    }

    public T LoadAsset<T>(string assetName, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_config == null)
        {
            Debug.LogError("[ResourcesProvider] AssetInfoConfig 未加载");
            return null;
        }

        var info = _config.GetByName(assetName);
        if (info == null)
        {
            Debug.LogError($"[ResourcesProvider] 未找到资源 {assetName} 的配置信息");
            return null;
        }

        if (_assetRefs.TryGetValue(assetName, out var existingRef) && existingRef.state == LoadState.Loaded)
        {
            existingRef.refCount++;
            return existingRef.asset as T;
        }

        string path = !string.IsNullOrEmpty(info.resourcesPath) ? info.resourcesPath : assetName;
        var asset = Resources.Load<T>(path);
        if (asset == null)
        {
            Debug.LogError($"[ResourcesProvider] Resources.Load 失败: {path}");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[ResourcesProvider] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                return LoadAsset<T>(info.fallbackAssetName, subAssetName);
            }

            return null;
        }

        var assetRef = new AssetRef(assetName);
        assetRef.asset = asset;
        assetRef.state = LoadState.Loaded;
        assetRef.refCount = 1;
        _assetRefs[assetName] = assetRef;

        return asset as T;
    }

    public void LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress = null, string subAssetName = null) where T : UnityEngine.Object
    {
        var result = LoadAsset<T>(assetName, subAssetName);
        onProgress?.Invoke(1f);
        onComplete?.Invoke(result);
    }

    public void ReleaseAsset(string assetName)
    {
        if (!_assetRefs.TryGetValue(assetName, out var assetRef))
        {
            Debug.LogWarning($"[ResourcesProvider] 尝试释放未加载的资源: {assetName}");
            return;
        }

        assetRef.refCount--;
        if (assetRef.refCount <= 0)
        {
            if (assetRef.asset != null)
            {
                Resources.UnloadAsset(assetRef.asset);
            }
            assetRef.state = LoadState.Unloaded;
            _assetRefs.Remove(assetName);
        }
    }

    public GameObject InstantiateAsset(string assetName, Transform parent = null)
    {
        var prefab = LoadAsset<GameObject>(assetName);
        if (prefab == null) return null;
        return UnityEngine.Object.Instantiate(prefab, parent);
    }

    public void DestroyAsset(GameObject instance)
    {
        if (instance != null)
            UnityEngine.Object.Destroy(instance);
    }

    public bool IsLoaded(string assetName)
    {
        return _assetRefs.ContainsKey(assetName) && _assetRefs[assetName].state == LoadState.Loaded;
    }

    public void CleanupForSceneChange()
    {
    }

    public string GetDebugInfo()
    {
        return $"[ResourcesProvider] 已加载资源: {_assetRefs.Count}";
    }
}
