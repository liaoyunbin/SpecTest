using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AssetBundleProviderBase : IResourceProvider
{
    protected AssetInfoConfig _config;
    protected AssetBundleManifest _manifest;

    protected Dictionary<string, BundleInfo> _bundleCache = new Dictionary<string, BundleInfo>();
    protected Dictionary<string, AssetRef> _assetCache = new Dictionary<string, AssetRef>();
    protected Dictionary<GameObject, string> _instanceAssetMap = new Dictionary<GameObject, string>();
    protected Dictionary<string, HashSet<string>> _instanceBundleMap = new Dictionary<string, HashSet<string>>();

    protected int _maxConcurrentLoads = 5;
    protected int _activeLoadCount = 0;
    protected Queue<Action> _loadQueue = new Queue<Action>();

    public abstract void Initialize();
    public abstract void Cleanup();

    protected abstract AssetBundle LoadSingleBundle(string bundleName);
    protected abstract string GetBundlePath(string bundleName);

    public T LoadAsset<T>(string assetName, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_config == null)
        {
            Debug.LogError("[AssetBundleProviderBase] AssetInfoConfig 未加载");
            return null;
        }

        var info = _config.GetByName(assetName);
        if (info == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 未找到资源 {assetName} 的配置信息");
            return null;
        }

        if (_assetCache.TryGetValue(assetName, out var existingRef) && existingRef.state == LoadState.Loaded)
        {
            existingRef.refCount++;
            return ExtractSubAsset<T>(existingRef.asset, subAssetName);
        }

        LoadDependencies(info.bundleName);

        float startTime = Time.realtimeSinceStartup;

        var bundle = LoadBundleInternal(info.bundleName);
        if (bundle == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 加载 AB 失败: {info.bundleName}");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[AssetBundleProviderBase] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                return LoadAsset<T>(info.fallbackAssetName, subAssetName);
            }

            return null;
        }

        var asset = bundle.LoadAsset<T>(assetName);
        if (asset == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 从 AB {info.bundleName} 加载资源 {assetName} 失败");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[AssetBundleProviderBase] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                return LoadAsset<T>(info.fallbackAssetName, subAssetName);
            }

            return null;
        }

        float duration = Time.realtimeSinceStartup - startTime;

        var assetRef = new AssetRef(assetName);
        assetRef.asset = asset;
        assetRef.bundleName = info.bundleName;
        assetRef.state = LoadState.Loaded;
        assetRef.refCount = 1;
        _assetCache[assetName] = assetRef;

        if (_bundleCache.TryGetValue(info.bundleName, out var bundleInfo))
        {
            bundleInfo.refCount++;
            bundleInfo.loadedAssets.Add(assetName);
            bundleInfo.loadDuration = duration;
        }

        return ExtractSubAsset<T>(asset, subAssetName);
    }

    public void LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress = null, string subAssetName = null) where T : UnityEngine.Object
    {
        if (_config == null)
        {
            Debug.LogError("[AssetBundleProviderBase] AssetInfoConfig 未加载");
            onComplete?.Invoke(null);
            return;
        }

        var info = _config.GetByName(assetName);
        if (info == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 未找到资源 {assetName} 的配置信息");
            onComplete?.Invoke(null);
            return;
        }

        if (_assetCache.TryGetValue(assetName, out var existingRef))
        {
            if (existingRef.state == LoadState.Loaded)
            {
                existingRef.refCount++;
                onProgress?.Invoke(1f);
                onComplete?.Invoke(ExtractSubAsset<T>(existingRef.asset, subAssetName));
                return;
            }

            if (existingRef.state == LoadState.Loading)
            {
                existingRef.pendingCallbacks.Add((obj) =>
                {
                    onComplete?.Invoke(ExtractSubAsset<T>(obj, subAssetName));
                });
                return;
            }
        }

        var assetRef = new AssetRef(assetName);
        assetRef.state = LoadState.Loading;
        _assetCache[assetName] = assetRef;

        Action loadAction = () =>
        {
            _activeLoadCount++;
            ResourceManager.Instance.StartCoroutine(AsyncLoadCoroutine(assetName, info, assetRef, onComplete, onProgress, subAssetName));
        };

        if (_activeLoadCount >= _maxConcurrentLoads)
        {
            _loadQueue.Enqueue(loadAction);
        }
        else
        {
            loadAction();
        }
    }

    private IEnumerator AsyncLoadCoroutine<T>(string assetName, AssetInfo info, AssetRef assetRef,
        Action<T> onComplete, Action<float> onProgress, string subAssetName) where T : UnityEngine.Object
    {
        onProgress?.Invoke(0.1f);

        LoadDependencies(info.bundleName);

        onProgress?.Invoke(0.3f);

        var bundleRequest = LoadBundleAsyncInternal(info.bundleName);
        yield return bundleRequest;

        onProgress?.Invoke(0.6f);

        if (bundleRequest == null || bundleRequest.assetBundle == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 异步加载 AB 失败: {info.bundleName}");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[AssetBundleProviderBase] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                LoadAssetAsync(info.fallbackAssetName, onComplete, onProgress, subAssetName);
            }
            else
            {
                assetRef.state = LoadState.Failed;
                NotifyPendingCallbacks(assetRef, null);
                onComplete?.Invoke(null);
            }

            FinishAsyncLoad();
            yield break;
        }

        onProgress?.Invoke(0.8f);

        var assetLoadRequest = bundleRequest.assetBundle.LoadAssetAsync<T>(assetName);
        yield return assetLoadRequest;

        onProgress?.Invoke(1.0f);

        if (assetLoadRequest.asset == null)
        {
            Debug.LogError($"[AssetBundleProviderBase] 异步加载资源 {assetName} 失败");

            if (!string.IsNullOrEmpty(info.fallbackAssetName))
            {
                Debug.LogWarning($"[AssetBundleProviderBase] 尝试降级加载: {assetName} → {info.fallbackAssetName}");
                LoadAssetAsync(info.fallbackAssetName, onComplete, onProgress, subAssetName);
            }
            else
            {
                assetRef.state = LoadState.Failed;
                NotifyPendingCallbacks(assetRef, null);
                onComplete?.Invoke(null);
            }

            FinishAsyncLoad();
            yield break;
        }

        assetRef.asset = assetLoadRequest.asset;
        assetRef.bundleName = info.bundleName;
        assetRef.state = LoadState.Loaded;
        assetRef.refCount = 1;

        if (_bundleCache.TryGetValue(info.bundleName, out var bundleInfo))
        {
            bundleInfo.refCount++;
            bundleInfo.loadedAssets.Add(assetName);
        }

        var result = ExtractSubAsset<T>(assetLoadRequest.asset, subAssetName);
        NotifyPendingCallbacks(assetRef, assetLoadRequest.asset);
        onComplete?.Invoke(result);

        FinishAsyncLoad();
    }

    private void FinishAsyncLoad()
    {
        _activeLoadCount--;
        if (_loadQueue.Count > 0 && _activeLoadCount < _maxConcurrentLoads)
        {
            var next = _loadQueue.Dequeue();
            next();
        }
    }

    private void NotifyPendingCallbacks(AssetRef assetRef, UnityEngine.Object asset)
    {
        if (assetRef.pendingCallbacks.Count > 0)
        {
            foreach (var cb in assetRef.pendingCallbacks)
            {
                cb?.Invoke(asset);
            }
            assetRef.pendingCallbacks.Clear();
        }
    }

    protected void LoadDependencies(string bundleName)
    {
        if (_manifest == null) return;

        string[] deps = _manifest.GetAllDependencies(bundleName);
        if (deps == null) return;

        foreach (var dep in deps)
        {
            if (string.IsNullOrEmpty(dep) || dep == bundleName) continue;

            if (_bundleCache.TryGetValue(dep, out var depInfo) && depInfo.state == LoadState.Loaded)
                continue;

            LoadBundleInternal(dep);
        }
    }

    private AssetBundle LoadBundleInternal(string bundleName)
    {
        if (_bundleCache.TryGetValue(bundleName, out var existing))
        {
            if (existing.state == LoadState.Loaded && existing.bundle != null)
                return existing.bundle;

            if (existing.state == LoadState.Loading)
                return null;
        }

        var bundleInfo = new BundleInfo(bundleName);
        bundleInfo.state = LoadState.Loading;
        bundleInfo.loadTimestamp = Time.realtimeSinceStartup;
        _bundleCache[bundleName] = bundleInfo;

        var bundle = LoadSingleBundle(bundleName);
        if (bundle == null)
        {
            bundleInfo.state = LoadState.Failed;
            Debug.LogError($"[AssetBundleProviderBase] 加载 AB 失败: {bundleName}");
            return null;
        }

        bundleInfo.bundle = bundle;
        bundleInfo.state = LoadState.Loaded;
        bundleInfo.loadDuration = Time.realtimeSinceStartup - bundleInfo.loadTimestamp;

        return bundle;
    }

    private AssetBundleCreateRequest LoadBundleAsyncInternal(string bundleName)
    {
        if (_bundleCache.TryGetValue(bundleName, out var existing))
        {
            if (existing.state == LoadState.Loaded && existing.bundle != null)
                return null;

            if (existing.state == LoadState.Loading)
                return null;
        }

        var bundleInfo = new BundleInfo(bundleName);
        bundleInfo.state = LoadState.Loading;
        bundleInfo.loadTimestamp = Time.realtimeSinceStartup;
        _bundleCache[bundleName] = bundleInfo;

        string path = GetBundlePath(bundleName);
        var info = _config?.GetByName(bundleName);
        uint crc = info?.crc ?? 0;

        var request = AssetBundle.LoadFromFileAsync(path, 0, crc);
        if (request == null)
        {
            bundleInfo.state = LoadState.Failed;
            return null;
        }

        return request;
    }

    public void ReleaseAsset(string assetName)
    {
        if (!_assetCache.TryGetValue(assetName, out var assetRef))
        {
            Debug.LogWarning($"[AssetBundleProviderBase] 尝试释放未加载的资源: {assetName}");
            return;
        }

        assetRef.refCount--;
        if (assetRef.refCount <= 0)
        {
            assetRef.refCount = 0;
            string bundleName = assetRef.bundleName;
            assetRef.state = LoadState.Unloaded;
            _assetCache.Remove(assetName);

            if (_bundleCache.TryGetValue(bundleName, out var bundleInfo))
            {
                bundleInfo.refCount--;
                bundleInfo.loadedAssets.Remove(assetName);

                if (bundleInfo.CanUnload)
                {
                    UnloadBundle(bundleName);
                }
            }
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
                if (!_instanceBundleMap.TryGetValue(instance.GetHashCode().ToString(), out var bundleSet))
                {
                    bundleSet = new HashSet<string>();
                    _instanceBundleMap[instance.GetHashCode().ToString()] = bundleSet;
                }
                bundleSet.Add(bundleName);

                if (_bundleCache.TryGetValue(bundleName, out var bundleInfo))
                {
                    bundleInfo.instanceCount++;
                }
            }
        }

        return instance;
    }

    public void DestroyAsset(GameObject instance)
    {
        if (instance == null) return;

        string instanceKey = instance.GetHashCode().ToString();
        if (_instanceBundleMap.TryGetValue(instanceKey, out var bundleSet))
        {
            foreach (var bundleName in bundleSet)
            {
                if (_bundleCache.TryGetValue(bundleName, out var bundleInfo))
                {
                    bundleInfo.instanceCount--;
                }
            }
            _instanceBundleMap.Remove(instanceKey);
        }

        _instanceAssetMap.Remove(instance);
        UnityEngine.Object.Destroy(instance);
    }

    protected void UnloadBundle(string bundleName)
    {
        if (!_bundleCache.TryGetValue(bundleName, out var bundleInfo)) return;

        if (!bundleInfo.CanUnload) return;

        if (bundleInfo.bundle != null)
        {
            bundleInfo.bundle.Unload(true);
        }

        _bundleCache.Remove(bundleName);
        Debug.Log($"[AssetBundleProviderBase] 已卸载 AB: {bundleName}");
    }

    public bool IsLoaded(string assetName)
    {
        return _assetCache.ContainsKey(assetName) && _assetCache[assetName].state == LoadState.Loaded;
    }

    public void CleanupForSceneChange()
    {
        var toRemove = new List<string>();
        foreach (var kvp in _bundleCache)
        {
            if (!kvp.Value.isPermanent && kvp.Value.CanUnload)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var bundleName in toRemove)
        {
            UnloadBundle(bundleName);
        }

        Debug.Log($"[AssetBundleProviderBase] 场景切换清理完成，已卸载 {toRemove.Count} 个非永久 AB");
    }

    public string GetDebugInfo()
    {
        int bundleCount = _bundleCache.Count;
        int assetCount = _assetCache.Count;
        int totalRefs = 0;
        long totalSize = 0;

        foreach (var kvp in _bundleCache)
        {
            totalRefs += kvp.Value.refCount + kvp.Value.instanceCount;
            totalSize += kvp.Value.estimatedSize;
        }

        return $"[AssetBundleProviderBase] AB 数: {bundleCount}, 资源数: {assetCount}, 总引用: {totalRefs}, 估算内存: {totalSize / 1024}KB";
    }

    private T ExtractSubAsset<T>(UnityEngine.Object mainAsset, string subAssetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(subAssetName))
            return mainAsset as T;

        if (mainAsset is T && string.IsNullOrEmpty(subAssetName))
            return mainAsset as T;

        return null;
    }
}
