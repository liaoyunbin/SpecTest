using System;
using UnityEngine;

public interface IResourceProvider
{
    void Initialize();
    void Cleanup();
    T LoadAsset<T>(string assetName, string subAssetName = null) where T : UnityEngine.Object;
    void LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress = null, string subAssetName = null) where T : UnityEngine.Object;
    void ReleaseAsset(string assetName);
    GameObject InstantiateAsset(string assetName, Transform parent = null);
    void DestroyAsset(GameObject instance);
    bool IsLoaded(string assetName);
    void CleanupForSceneChange();
    string GetDebugInfo();
}
