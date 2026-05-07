using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ABDownloader
{
    private string _baseUrl;
    private string _persistentPath;

    public int TotalFiles { get; private set; }
    public int CompletedFiles { get; private set; }
    public float OverallProgress { get; private set; }
    public bool HasError { get; private set; }
    public string ErrorMessage { get; private set; }

    public ABDownloader(string baseUrl)
    {
        _baseUrl = baseUrl;
        _persistentPath = Path.Combine(Application.persistentDataPath, "AssetBundles");
        if (!Directory.Exists(_persistentPath))
            Directory.CreateDirectory(_persistentPath);
    }

    public IEnumerator DownloadFiles(List<FileEntry> downloadList, Action<float> onProgress, Action<bool> onComplete)
    {
        TotalFiles = downloadList.Count;
        CompletedFiles = 0;
        HasError = false;

        for (int i = 0; i < downloadList.Count; i++)
        {
            var entry = downloadList[i];
            yield return DownloadSingleFile(entry, entry.name);

            CompletedFiles = i + 1;
            OverallProgress = (float)CompletedFiles / TotalFiles;
            onProgress?.Invoke(OverallProgress);

            if (HasError)
            {
                onComplete?.Invoke(false);
                yield break;
            }
        }

        onComplete?.Invoke(!HasError);
    }

    private IEnumerator DownloadSingleFile(FileEntry entry, string fileName)
    {
        string url = _baseUrl.TrimEnd('/') + "/" + fileName;
        string savePath = Path.Combine(_persistentPath, fileName);

        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] data = request.downloadHandler.data;

                string downloadedMd5 = ComputeMD5(data);
                if (downloadedMd5 != entry.md5)
                {
                    Debug.LogWarning($"[ABDownloader] {fileName} MD5 不匹配（第 {attempt + 1} 次），重试中...");
                    continue;
                }

                File.WriteAllBytes(savePath, data);
                yield break;
            }
            else
            {
                Debug.LogWarning($"[ABDownloader] {fileName} 下载失败（第 {attempt + 1} 次）: {request.error}");
                if (attempt < maxRetries - 1)
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        HasError = true;
        ErrorMessage = $"文件 {fileName} 下载失败（已重试 {maxRetries} 次）";
    }

    public static string ComputeMD5(byte[] data)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(data);
        var sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static string ComputeFileMD5(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        byte[] data = File.ReadAllBytes(filePath);
        return ComputeMD5(data);
    }
}
