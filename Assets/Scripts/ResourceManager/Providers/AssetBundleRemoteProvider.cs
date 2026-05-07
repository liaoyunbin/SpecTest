using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class AssetBundleRemoteProvider : AssetBundleProviderBase
{
    private string _baseUrl;
    private VersionManager _versionManager;
    private FileListLoader _fileListLoader;
    private ABDownloader _downloader;

    private string _persistentBundlePath;
    private bool _hotUpdateCompleted = false;
    private bool _useFallback = false;

    public bool IsHotUpdateCompleted => _hotUpdateCompleted;
    public bool IsUsingFallback => _useFallback;

    public override void Initialize()
    {
        _baseUrl = ResourceManager.Instance != null
            ? ResourceManager.Instance.BaseUrl
            : "http://localhost/assetbundles";

        _persistentBundlePath = Path.Combine(Application.persistentDataPath, "AssetBundles");
        if (!Directory.Exists(_persistentBundlePath))
            Directory.CreateDirectory(_persistentBundlePath);

        _versionManager = new VersionManager(_baseUrl);
        _fileListLoader = new FileListLoader(_baseUrl);
        _downloader = new ABDownloader(_baseUrl);

        _config = Resources.Load<AssetInfoConfig>("AssetInfoConfig");

        _bundleCache.Clear();
        _assetCache.Clear();
        _instanceAssetMap.Clear();
        _instanceBundleMap.Clear();

        Debug.Log("[AssetBundleRemoteProvider] 初始化完成（远程 AB + 热更新模式）");
    }

    public IEnumerator PerformHotUpdate(Action<float> onProgress, Action<bool> onComplete)
    {
        yield return _versionManager.CheckVersion((hasUpdate) =>
        {
            if (!hasUpdate)
            {
                Debug.Log("[AssetBundleRemoteProvider] 版本一致，无需更新");
                _hotUpdateCompleted = true;
                LoadManifestAndConfig();
                onComplete?.Invoke(true);
            }
        });

        if (!_versionManager.HasUpdate)
        {
            if (!_hotUpdateCompleted)
            {
                _hotUpdateCompleted = true;
                LoadManifestAndConfig();
                onComplete?.Invoke(true);
            }
            yield break;
        }

        yield return _fileListLoader.LoadRemoteFileList((success) =>
        {
            if (!success)
            {
                Debug.LogError("[AssetBundleRemoteProvider] 获取远程文件列表失败，使用包内资源");
                _useFallback = true;
                _hotUpdateCompleted = true;
                LoadManifestAndConfig();
                onComplete?.Invoke(false);
            }
        });

        if (_useFallback) yield break;

        _fileListLoader.ComputeDiffList();
        var downloadList = _fileListLoader.DownloadList;

        if (downloadList.Count == 0)
        {
            Debug.Log("[AssetBundleRemoteProvider] 文件一致，无需下载");
            _versionManager.SaveLocalVersion(_versionManager.RemoteVersion);
            _fileListLoader.SaveLocalFileList(_fileListLoader.RemoteFileList);
            _hotUpdateCompleted = true;
            LoadManifestAndConfig();
            onComplete?.Invoke(true);
            yield break;
        }

        Debug.Log($"[AssetBundleRemoteProvider] 需要下载 {downloadList.Count} 个文件");

        yield return _downloader.DownloadFiles(downloadList, onProgress, (success) =>
        {
            if (success)
            {
                _versionManager.SaveLocalVersion(_versionManager.RemoteVersion);
                _fileListLoader.SaveLocalFileList(_fileListLoader.RemoteFileList);
                CleanupOldBundles();
                _hotUpdateCompleted = true;
                LoadManifestAndConfig();
                Debug.Log("[AssetBundleRemoteProvider] 热更新完成");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[AssetBundleRemoteProvider] 热更新失败: {_downloader.ErrorMessage}，使用包内资源");
                _useFallback = true;
                _hotUpdateCompleted = true;
                LoadManifestAndConfig();
                onComplete?.Invoke(false);
            }
        });
    }

    private void LoadManifestAndConfig()
    {
        string manifestPath = GetBundlePath("AssetBundleManifest");
        if (File.Exists(manifestPath))
        {
            var manifestBundle = AssetBundle.LoadFromFile(manifestPath);
            if (manifestBundle != null)
            {
                _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                manifestBundle.Unload(false);
            }
        }

        string configAbPath = Path.Combine(_persistentBundlePath, "asset_config");
        if (File.Exists(configAbPath))
        {
            var configBundle = AssetBundle.LoadFromFile(configAbPath);
            if (configBundle != null)
            {
                var loadedConfig = configBundle.LoadAsset<AssetInfoConfig>("AssetInfoConfig");
                if (loadedConfig != null)
                {
                    _config = loadedConfig;
                }
                configBundle.Unload(false);
            }
        }

        if (_config == null)
        {
            _config = Resources.Load<AssetInfoConfig>("AssetInfoConfig");
        }
    }

    public override void Cleanup()
    {
        var bundleNames = new List<string>(_bundleCache.Keys);
        foreach (var name in bundleNames)
        {
            UnloadBundle(name);
        }
        _bundleCache.Clear();
        _assetCache.Clear();
        _instanceAssetMap.Clear();
        _instanceBundleMap.Clear();
        _hotUpdateCompleted = false;
        _useFallback = false;
        Debug.Log("[AssetBundleRemoteProvider] 已清理所有资源");
    }

    protected override AssetBundle LoadSingleBundle(string bundleName)
    {
        string path = GetBundlePath(bundleName);
        if (string.IsNullOrEmpty(path)) return null;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[AssetBundleRemoteProvider] 本地不存在 {bundleName}，尝试从远程下载");

            var entry = _fileListLoader.RemoteFileList?.files?.FirstOrDefault(f => f.name == bundleName);
            if (entry == null) return null;

            var downloader = new ABDownloader(_baseUrl);
            return null;
        }

        var info = _config?.GetByName(bundleName);
        uint crc = info?.crc ?? 0;
        return AssetBundle.LoadFromFile(path, 0, crc);
    }

    protected override string GetBundlePath(string bundleName)
    {
        return Path.Combine(_persistentBundlePath, bundleName);
    }

    private void CleanupOldBundles()
    {
        if (_fileListLoader.RemoteFileList == null) return;

        var validBundles = new HashSet<string>();
        foreach (var file in _fileListLoader.RemoteFileList.files)
        {
            validBundles.Add(file.name);
        }

        var files = Directory.GetFiles(_persistentBundlePath, "*.ab");
        foreach (var file in files)
        {
            string name = Path.GetFileName(file);
            if (!validBundles.Contains(name))
            {
                try
                {
                    File.Delete(file);
                    Debug.Log($"[AssetBundleRemoteProvider] 已清理旧 AB 文件: {name}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AssetBundleRemoteProvider] 清理旧 AB 文件失败: {name} - {e.Message}");
                }
            }
        }
    }
}
