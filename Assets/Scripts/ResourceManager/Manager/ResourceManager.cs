using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[ResourceManager]");
                _instance = go.AddComponent<ResourceManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private LoadMode _loadMode = LoadMode.Editor;
    [SerializeField] private ResourceConfig _config;

    private IResourceProvider _provider;
    private LoadMode _currentMode;

    public string BaseUrl => _config != null ? _config.baseUrl : "http://localhost/assetbundles";
    public int MaxConcurrentLoads => _config != null ? _config.maxConcurrentLoads : 5;
    public IResourceProvider CurrentProvider => _provider;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _config = Resources.Load<ResourceConfig>("ResourceConfig");
        if (_config == null)
        {
            _config = ScriptableObject.CreateInstance<ResourceConfig>();
        }

#if !UNITY_EDITOR
        if (_config.enableHotUpdate)
            _loadMode = LoadMode.AssetBundleRemote;
        else
            _loadMode = LoadMode.AssetBundleLocal;
#endif

        InitializeProvider(_loadMode);
    }

    private void InitializeProvider(LoadMode mode)
    {
        if (_provider != null)
        {
            _provider.Cleanup();
            _provider = null;
        }

        switch (mode)
        {
            case LoadMode.Editor:
#if UNITY_EDITOR
                _provider = new EditorAssetProvider();
#else
                _provider = new AssetBundleLocalProvider();
#endif
                break;

            case LoadMode.AssetBundleLocal:
                _provider = new AssetBundleLocalProvider();
                break;

            case LoadMode.AssetBundleRemote:
                var remoteProvider = new AssetBundleRemoteProvider();
                _provider = remoteProvider;
                StartCoroutine(remoteProvider.PerformHotUpdate(null, (success) =>
                {
                    Debug.Log($"[ResourceManager] 热更新 {(success ? "成功" : "失败，使用包内资源")}");
                }));
                break;

            case LoadMode.Resources:
                _provider = new ResourcesProvider();
                break;
        }

        _provider?.Initialize();
        _currentMode = mode;
        Debug.Log($"[ResourceManager] 已切换到加载模式: {mode}");
    }

    public void SetLoadMode(LoadMode mode)
    {
        if (mode == _currentMode) return;
        InitializeProvider(mode);
    }

    public T LoadAsset<T>(string assetName, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_provider == null) return null;
        return _provider.LoadAsset<T>(assetName, subAssetName);
    }

    public void LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress = null, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_provider == null)
        {
            onComplete?.Invoke(null);
            return;
        }
        _provider.LoadAssetAsync(assetName, onComplete, onProgress, subAssetName);
    }

    public void ReleaseAsset(string assetName)
    {
        _provider?.ReleaseAsset(assetName);
    }

    public GameObject InstantiateAsset(string assetName, Transform parent = null)
    {
        if (_provider == null) return null;
        return _provider.InstantiateAsset(assetName, parent);
    }

    public void DestroyAsset(GameObject instance)
    {
        _provider?.DestroyAsset(instance);
    }

    public void RegisterPooledInstance(GameObject instance, string assetName)
    {
        if (_provider is AssetBundleProviderBase abProvider)
        {
            var info = GetAssetInfo(assetName);
            if (info != null && info.referencedBundles != null)
            {
                foreach (var bundleName in info.referencedBundles)
                {
                    var bundleInfo = GetBundleInfo(bundleName);
                    if (bundleInfo != null)
                    {
                        bundleInfo.instanceCount++;
                    }
                }
            }
        }
    }

    public void UnregisterPooledInstance(GameObject instance)
    {
        if (_provider is AssetBundleProviderBase abProvider)
        {
            string instanceKey = instance.GetHashCode().ToString();
            var field = typeof(AssetBundleProviderBase).GetField("_instanceBundleMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var map = field.GetValue(abProvider) as Dictionary<string, HashSet<string>>;
                if (map != null && map.TryGetValue(instanceKey, out var bundleSet))
                {
                    foreach (var bundleName in bundleSet)
                    {
                        var bundleInfo = GetBundleInfo(bundleName);
                        if (bundleInfo != null)
                        {
                            bundleInfo.instanceCount--;
                        }
                    }
                    map.Remove(instanceKey);
                }
            }
        }
    }

    public void CleanupForSceneChange()
    {
        _provider?.CleanupForSceneChange();
    }

    public AssetInfo GetAssetInfo(string assetName)
    {
        if (_provider is AssetBundleProviderBase abProvider)
        {
            var configField = typeof(AssetBundleProviderBase).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var config = configField.GetValue(abProvider) as AssetInfoConfig;
                return config?.GetByName(assetName);
            }
        }
        return null;
    }

    public BundleInfo GetBundleInfo(string bundleName)
    {
        if (_provider is AssetBundleProviderBase abProvider)
        {
            var cacheField = typeof(AssetBundleProviderBase).GetField("_bundleCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cacheField != null)
            {
                var cache = cacheField.GetValue(abProvider) as Dictionary<string, BundleInfo>;
                if (cache != null && cache.TryGetValue(bundleName, out var info))
                    return info;
            }
        }
        return null;
    }

    public List<BundleDebugInfo> GetLoadedBundles()
    {
        var result = new List<BundleDebugInfo>();
        if (_provider is AssetBundleProviderBase abProvider)
        {
            var cacheField = typeof(AssetBundleProviderBase).GetField("_bundleCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cacheField != null)
            {
                var cache = cacheField.GetValue(abProvider) as Dictionary<string, BundleInfo>;
                if (cache != null)
                {
                    foreach (var kvp in cache)
                    {
                        result.Add(new BundleDebugInfo
                        {
                            bundleName = kvp.Key,
                            refCount = kvp.Value.refCount,
                            instanceCount = kvp.Value.instanceCount,
                            isPermanent = kvp.Value.isPermanent,
                            estimatedSize = kvp.Value.estimatedSize,
                            loadDuration = kvp.Value.loadDuration
                        });
                    }
                }
            }
        }
        return result;
    }

    public List<AssetDebugInfo> GetLoadedAssets()
    {
        var result = new List<AssetDebugInfo>();
        if (_provider is AssetBundleProviderBase abProvider)
        {
            var cacheField = typeof(AssetBundleProviderBase).GetField("_assetCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cacheField != null)
            {
                var cache = cacheField.GetValue(abProvider) as Dictionary<string, AssetRef>;
                if (cache != null)
                {
                    foreach (var kvp in cache)
                    {
                        result.Add(new AssetDebugInfo
                        {
                            assetName = kvp.Key,
                            refCount = kvp.Value.refCount,
                            state = kvp.Value.state,
                            type = kvp.Value.asset != null ? kvp.Value.asset.GetType().Name : "Unknown"
                        });
                    }
                }
            }
        }
        return result;
    }

    public MemorySummary GetMemorySummary()
    {
        var summary = new MemorySummary();
        var bundles = GetLoadedBundles();
        summary.totalBundles = bundles.Count;
        foreach (var b in bundles)
        {
            summary.totalMemoryBytes += b.estimatedSize;
        }
        return summary;
    }

    private void OnDestroy()
    {
        _provider?.Cleanup();
    }
}

public class BundleDebugInfo
{
    public string bundleName;
    public int refCount;
    public int instanceCount;
    public bool isPermanent;
    public long estimatedSize;
    public float loadDuration;
}

public class AssetDebugInfo
{
    public string assetName;
    public int refCount;
    public LoadState state;
    public string type;
}

public class MemorySummary
{
    public int totalBundles;
    public long totalMemoryBytes;

    public string GetMemoryString()
    {
        if (totalMemoryBytes < 1024 * 1024)
            return $"{totalMemoryBytes / 1024} KB";
        return $"{totalMemoryBytes / (1024 * 1024):F2} MB";
    }
}
