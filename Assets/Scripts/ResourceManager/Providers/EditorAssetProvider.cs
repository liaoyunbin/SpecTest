#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorAssetProvider : IResourceProvider
{
    private AssetInfoConfig _config;
    private Dictionary<string, AssetRef> _assetRefs = new Dictionary<string, AssetRef>();
    private Dictionary<GameObject, string> _instanceAssetMap = new Dictionary<GameObject, string>();
    private Dictionary<string, int> _instanceCounts = new Dictionary<string, int>();

    public void Initialize()
    {
        _config = Resources.Load<AssetInfoConfig>("AssetInfoConfig");
        if (_config == null)
        {
            Debug.LogError("[EditorAssetProvider] 无法加载 AssetInfoConfig，请确保 Resources 目录中存在该配置。");
        }
        _assetRefs.Clear();
        _instanceAssetMap.Clear();
        _instanceCounts.Clear();
        Debug.Log("[EditorAssetProvider] 初始化完成（Editor 模式）");
    }

    public void Cleanup()
    {
        CheckUnreleasedRefs();
        _assetRefs.Clear();
        _instanceAssetMap.Clear();
        _instanceCounts.Clear();
    }

    public T LoadAsset<T>(string assetName, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_config == null)
        {
            Debug.LogError("[EditorAssetProvider] AssetInfoConfig 未加载");
            return null;
        }

        var info = _config.GetByName(assetName);
        if (info == null)
        {
            Debug.LogError($"[EditorAssetProvider] 未找到资源 {assetName} 的配置信息");
            return null;
        }

        if (!_assetRefs.TryGetValue(assetName, out var assetRef))
        {
            assetRef = new AssetRef(assetName);
            _assetRefs[assetName] = assetRef;
        }

        if (assetRef.state == LoadState.Loaded && assetRef.asset != null)
        {
            assetRef.refCount++;
            return GetSubAssetOrMain<T>(assetRef.asset, subAssetName);
        }

        string path = info.assetPath;
        var mainAsset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (mainAsset == null)
        {
            Debug.LogError($"[EditorAssetProvider] 加载资源失败: {path}");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[EditorAssetProvider] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                return LoadAsset<T>(info.fallbackAssetName, subAssetName);
            }

            return null;
        }

        assetRef.asset = mainAsset;
        assetRef.state = LoadState.Loaded;
        assetRef.bundleName = info.bundleName;
        assetRef.refCount++;

        return GetSubAssetOrMain<T>(mainAsset, subAssetName);
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
            Debug.LogWarning($"[EditorAssetProvider] 尝试释放未加载的资源: {assetName}");
            return;
        }

        assetRef.refCount--;
        if (assetRef.refCount <= 0)
        {
            assetRef.refCount = 0;
            assetRef.state = LoadState.Unloaded;
            assetRef.asset = null;
            _assetRefs.Remove(assetName);
        }
    }

    public GameObject InstantiateAsset(string assetName, Transform parent = null)
    {
        var prefab = LoadAsset<GameObject>(assetName);
        if (prefab == null) return null;

        var instance = UnityEngine.Object.Instantiate(prefab, parent);
        _instanceAssetMap[instance] = assetName;

        var info = _config?.GetByName(assetName);
        if (info != null && info.referencedBundles != null)
        {
            foreach (var bundleName in info.referencedBundles)
            {
                if (!_instanceCounts.ContainsKey(bundleName))
                    _instanceCounts[bundleName] = 0;
                _instanceCounts[bundleName]++;
            }
        }

        return instance;
    }

    public void DestroyAsset(GameObject instance)
    {
        if (instance == null) return;

        if (_instanceAssetMap.TryGetValue(instance, out var assetName))
        {
            var info = _config?.GetByName(assetName);
            if (info != null && info.referencedBundles != null)
            {
                foreach (var bundleName in info.referencedBundles)
                {
                    if (_instanceCounts.ContainsKey(bundleName))
                    {
                        _instanceCounts[bundleName]--;
                        if (_instanceCounts[bundleName] <= 0)
                            _instanceCounts.Remove(bundleName);
                    }
                }
            }

            _instanceAssetMap.Remove(instance);
        }

        UnityEngine.Object.Destroy(instance);
    }

    public bool IsLoaded(string assetName)
    {
        return _assetRefs.ContainsKey(assetName) && _assetRefs[assetName].state == LoadState.Loaded;
    }

    public void CleanupForSceneChange()
    {
        CheckUnreleasedRefs();
    }

    public string GetDebugInfo()
    {
        int loadedCount = 0;
        int totalRefs = 0;
        foreach (var kvp in _assetRefs)
        {
            if (kvp.Value.state == LoadState.Loaded)
            {
                loadedCount++;
                totalRefs += kvp.Value.refCount;
            }
        }
        return $"[EditorAssetProvider] 已加载资源: {loadedCount}, 总引用数: {totalRefs}, 实例追踪数: {_instanceAssetMap.Count}";
    }

    private void CheckUnreleasedRefs()
    {
        foreach (var kvp in _assetRefs)
        {
            if (kvp.Value.refCount > 0)
            {
                Debug.LogWarning($"[EditorAssetProvider] 资源 \"{kvp.Key}\" 存在未配对的 ReleaseAsset（loaded {kvp.Value.refCount} 次，未释放）");
            }
        }
    }

    private T GetSubAssetOrMain<T>(UnityEngine.Object mainAsset, string subAssetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(subAssetName))
            return mainAsset as T;

        if (mainAsset is T)
            return mainAsset as T;

        var subAssets = EditorUtility.InstanceIDToObject(mainAsset.GetInstanceID());
        return null;
    }
}
#endif
