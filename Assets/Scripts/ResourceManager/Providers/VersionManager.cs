using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VersionManager
{
    private string _baseUrl;
    private string _localVersionPath;

    public string LocalVersion { get; private set; }
    public string RemoteVersion { get; private set; }
    public bool HasUpdate { get; private set; }

    public VersionManager(string baseUrl)
    {
        _baseUrl = baseUrl;
        _localVersionPath = Path.Combine(Application.persistentDataPath, "version.json");
    }

    public IEnumerator CheckVersion(Action<bool> onComplete)
    {
        LoadLocalVersion();

        string url = _baseUrl.TrimEnd('/') + "/version.json";
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var remoteInfo = JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
            if (remoteInfo != null)
            {
                RemoteVersion = remoteInfo.version;
                HasUpdate = LocalVersion != RemoteVersion;
                onComplete?.Invoke(HasUpdate);
                yield break;
            }
        }

        Debug.LogWarning($"[VersionManager] 获取远程版本失败: {request.error}，使用本地版本");
        HasUpdate = false;
        onComplete?.Invoke(false);
    }

    public void SaveLocalVersion(string version)
    {
        var info = new VersionInfo { version = version };
        string json = JsonUtility.ToJson(info);
        File.WriteAllText(_localVersionPath, json);
        LocalVersion = version;
    }

    private void LoadLocalVersion()
    {
        if (File.Exists(_localVersionPath))
        {
            try
            {
                string json = File.ReadAllText(_localVersionPath);
                var info = JsonUtility.FromJson<VersionInfo>(json);
                LocalVersion = info?.version ?? "0.0.0";
            }
            catch
            {
                LocalVersion = "0.0.0";
            }
        }
        else
        {
            LocalVersion = "0.0.0";
        }
    }
}
