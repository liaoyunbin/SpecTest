using UnityEngine;

public class HotUpdateExample : MonoBehaviour
{
    private AssetBundleRemoteProvider _remoteProvider;
    private bool _isUpdating = false;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));

        if (GUILayout.Button("启动热更新"))
        {
            StartHotUpdate();
        }

        if (_isUpdating)
        {
            GUILayout.Label("热更新进行中...");
        }

        if (GUILayout.Button("加载远程 Hero"))
        {
            var hero = ResourceManager.Instance.InstantiateAsset("Hero", transform);
            if (hero != null)
            {
                Debug.Log("[HotUpdateExample] 远程 Hero 加载成功");
            }
        }

        GUILayout.EndArea();
    }

    private void StartHotUpdate()
    {
        ResourceManager.Instance.SetLoadMode(LoadMode.AssetBundleRemote);
        _remoteProvider = ResourceManager.Instance.CurrentProvider as AssetBundleRemoteProvider;

        if (_remoteProvider != null)
        {
            _isUpdating = true;
            StartCoroutine(_remoteProvider.PerformHotUpdate(
                (progress) =>
                {
                    Debug.Log($"[HotUpdateExample] 热更新进度: {progress:P0}");
                },
                (success) =>
                {
                    _isUpdating = false;
                    Debug.Log($"[HotUpdateExample] 热更新 {(success ? "成功" : "失败")}");
                }));
        }
    }
}
