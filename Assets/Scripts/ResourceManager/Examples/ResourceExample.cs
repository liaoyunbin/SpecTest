using UnityEngine;

public class ResourceExample : MonoBehaviour
{
    private GameObject _heroInstance;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));

        if (GUILayout.Button("1. Editor 模式加载 Hero"))
        {
            ResourceManager.Instance.SetLoadMode(LoadMode.Editor);
            _heroInstance = ResourceManager.Instance.InstantiateAsset("Hero", transform);
            Debug.Log("[Example] Editor 模式加载 Hero 完成");
        }

        if (GUILayout.Button("2. 销毁 Hero 实例"))
        {
            if (_heroInstance != null)
            {
                ResourceManager.Instance.DestroyAsset(_heroInstance);
                _heroInstance = null;
                Debug.Log("[Example] 销毁 Hero 实例");
            }
        }

        if (GUILayout.Button("3. 异步加载战斗场景"))
        {
            ResourceManager.Instance.LoadAssetAsync<GameObject>("BattleScene",
                (asset) =>
                {
                    if (asset != null)
                    {
                        Instantiate(asset);
                        Debug.Log("[Example] 异步加载战斗场景完成");
                    }
                },
                (progress) =>
                {
                    Debug.Log($"[Example] 加载进度: {progress:P0}");
                });
        }

        if (GUILayout.Button("4. 加载道具列表"))
        {
            LoadItemList();
        }

        if (GUILayout.Button("5. 场景切换清理"))
        {
            ResourceManager.Instance.CleanupForSceneChange();
            Debug.Log("[Example] 场景切换清理完成");
        }

        if (GUILayout.Button("6. 显示 Debug 信息"))
        {
            var bundles = ResourceManager.Instance.GetLoadedBundles();
            Debug.Log($"[Example] 已加载 AB: {bundles.Count}");
            foreach (var b in bundles)
            {
                Debug.Log($"  AB: {b.bundleName} ref={b.refCount} inst={b.instanceCount}");
            }

            var assets = ResourceManager.Instance.GetLoadedAssets();
            Debug.Log($"[Example] 已缓存资源: {assets.Count}");
            foreach (var a in assets)
            {
                Debug.Log($"  Asset: {a.assetName} ref={a.refCount}");
            }

            var summary = ResourceManager.Instance.GetMemorySummary();
            Debug.Log($"[Example] 内存概览: {summary.totalBundles} AB, {summary.GetMemoryString()}");
        }

        if (GUILayout.Button("7. 切换到 AB Local 模式"))
        {
            ResourceManager.Instance.SetLoadMode(LoadMode.AssetBundleLocal);
            Debug.Log("[Example] 已切换到 AB Local 模式");
        }

        GUILayout.EndArea();
    }

    private void LoadItemList()
    {
        string[] items = { "Item_Sword", "Item_Shield", "Item_Potion", "Item_Ring" };
        foreach (var item in items)
        {
            ResourceManager.Instance.LoadAssetAsync<GameObject>(item,
                (asset) =>
                {
                    if (asset != null)
                    {
                        var go = Instantiate(asset, transform);
                        Debug.Log($"[Example] 加载道具: {item}");
                    }
                });
        }
    }

    private void OnDestroy()
    {
        if (_heroInstance != null)
        {
            ResourceManager.Instance.DestroyAsset(_heroInstance);
            _heroInstance = null;
        }
    }
}
