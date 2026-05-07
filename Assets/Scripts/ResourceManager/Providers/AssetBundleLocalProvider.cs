using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleLocalProvider : AssetBundleProviderBase
{
    private string _bundleRootPath;

    public override void Initialize()
    {
        _config = Resources.Load<AssetInfoConfig>("AssetInfoConfig");
        if (_config == null)
        {
            Debug.LogError("[AssetBundleLocalProvider] 无法加载 AssetInfoConfig");
            return;
        }

        _bundleRootPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles");

        string manifestPath = GetBundlePath(GetManifestBundleName());
        var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
        if (manifestBundle != null)
        {
            _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
        }
        else
        {
            Debug.LogError("[AssetBundleLocalProvider] 无法加载 AssetBundleManifest");
        }

        _bundleCache.Clear();
        _assetCache.Clear();
        _instanceAssetMap.Clear();
        _instanceBundleMap.Clear();

        Debug.Log("[AssetBundleLocalProvider] 初始化完成（本地 AB 模式）");
    }

    public override void Cleanup()
    {
        var bundleNames = new System.Collections.Generic.List<string>(_bundleCache.Keys);
        foreach (var name in bundleNames)
        {
            UnloadBundle(name);
        }
        _bundleCache.Clear();
        _assetCache.Clear();
        _instanceAssetMap.Clear();
        _instanceBundleMap.Clear();
        Debug.Log("[AssetBundleLocalProvider] 已清理所有资源");
    }

    protected override AssetBundle LoadSingleBundle(string bundleName)
    {
        string path = GetBundlePath(bundleName);
        if (string.IsNullOrEmpty(path)) return null;

        var info = _config?.GetByName(bundleName);
        uint crc = info?.crc ?? 0;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (path.Contains(Application.streamingAssetsPath) || path.Contains("jar:"))
        {
            return LoadAndroidBundle(path);
        }
#endif

        return AssetBundle.LoadFromFile(path, 0, crc);
    }

    protected override string GetBundlePath(string bundleName)
    {
        string persistentPath = Path.Combine(Application.persistentDataPath, "AssetBundles", bundleName);
        if (File.Exists(persistentPath))
            return persistentPath;

#if UNITY_IOS && !UNITY_EDITOR
        string streamingPath = Application.dataPath + "/Raw/AssetBundles/" + bundleName;
#elif UNITY_ANDROID && !UNITY_EDITOR
        string streamingPath = Application.streamingAssetsPath + "/AssetBundles/" + bundleName;
#else
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles", bundleName);
#endif

        return streamingPath;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private AssetBundle LoadAndroidBundle(string path)
    {
        string unityWebRequestPath = path;
        if (!unityWebRequestPath.StartsWith("jar:"))
        {
            unityWebRequestPath = "jar:file://" + Application.dataPath + "!/assets/AssetBundles/" + Path.GetFileName(path);
        }

        var request = UnityWebRequestAssetBundle.GetAssetBundle(unityWebRequestPath);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            System.Threading.Thread.Sleep(1);
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AssetBundleLocalProvider] Android 加载 AB 失败: {request.error}");
            request.Dispose();
            return null;
        }

        var bundle = DownloadHandlerAssetBundle.GetContent(request);
        request.Dispose();
        return bundle;
    }
#endif

    private string GetManifestBundleName()
    {
        return GetBundlePath("AssetBundleManifest");
    }
}
