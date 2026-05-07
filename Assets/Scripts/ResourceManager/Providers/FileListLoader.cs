using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class FileListLoader
{
    private string _baseUrl;
    private string _localPath;

    public FileList RemoteFileList { get; private set; }
    public List<FileEntry> DownloadList { get; private set; }

    public FileListLoader(string baseUrl)
    {
        _baseUrl = baseUrl;
        _localPath = Path.Combine(Application.persistentDataPath, "files.json");
    }

    public IEnumerator LoadRemoteFileList(Action<bool> onComplete)
    {
        string url = _baseUrl.TrimEnd('/') + "/files.json";
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            RemoteFileList = JsonUtility.FromJson<FileList>(request.downloadHandler.text);
            onComplete?.Invoke(RemoteFileList != null);
        }
        else
        {
            Debug.LogError($"[FileListLoader] 获取远程文件列表失败: {request.error}");
            onComplete?.Invoke(false);
        }
    }

    public void ComputeDiffList()
    {
        DownloadList = new List<FileEntry>();
        if (RemoteFileList == null) return;

        var localFiles = LoadLocalFileList();

        foreach (var remoteFile in RemoteFileList.files)
        {
            if (localFiles.TryGetValue(remoteFile.name, out string localMd5))
            {
                if (localMd5 != remoteFile.md5)
                {
                    DownloadList.Add(remoteFile);
                }
            }
            else
            {
                DownloadList.Add(remoteFile);
            }
        }

        DownloadList.Sort((a, b) =>
        {
            if (a.name == "AssetBundleManifest") return -1;
            if (b.name == "AssetBundleManifest") return 1;
            if (a.name == "asset_config") return -1;
            if (b.name == "asset_config") return 1;
            return 0;
        });
    }

    public void SaveLocalFileList(FileList fileList)
    {
        string json = JsonUtility.ToJson(fileList);
        File.WriteAllText(_localPath, json);
    }

    private Dictionary<string, string> LoadLocalFileList()
    {
        var result = new Dictionary<string, string>();
        if (File.Exists(_localPath))
        {
            try
            {
                string json = File.ReadAllText(_localPath);
                var localList = JsonUtility.FromJson<FileList>(json);
                if (localList != null)
                {
                    foreach (var file in localList.files)
                    {
                        result[file.name] = file.md5;
                    }
                }
            }
            catch { }
        }
        return result;
    }
}
