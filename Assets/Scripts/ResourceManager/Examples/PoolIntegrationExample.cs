using UnityEngine;

public class PoolIntegrationExample : MonoBehaviour
{
    private GameObject _poolRoot;
    private int _poolSize = 10;
    private string _bulletAssetName = "Bullet";

    private void Start()
    {
        _poolRoot = new GameObject("BulletPool");
        _poolRoot.transform.SetParent(transform);

        PrewarmPool();
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            var bullet = ResourceManager.Instance.InstantiateAsset(_bulletAssetName, _poolRoot.transform);
            if (bullet != null)
            {
                bullet.SetActive(false);
                ResourceManager.Instance.RegisterPooledInstance(bullet, _bulletAssetName);
            }
        }
        Debug.Log($"[PoolExample] 对象池预热完成，创建 {_poolSize} 个子弹实例");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));

        if (GUILayout.Button("从对象池获取子弹"))
        {
            SpawnBullet();
        }

        if (GUILayout.Button("清理对象池"))
        {
            CleanupPool();
        }

        GUILayout.EndArea();
    }

    private void SpawnBullet()
    {
        var bullet = ResourceManager.Instance.InstantiateAsset(_bulletAssetName, _poolRoot.transform);
        if (bullet != null)
        {
            bullet.SetActive(true);
            Debug.Log("[PoolExample] 从对象池获取子弹");
        }
    }

    private void CleanupPool()
    {
        for (int i = _poolRoot.transform.childCount - 1; i >= 0; i--)
        {
            var child = _poolRoot.transform.GetChild(i).gameObject;
            ResourceManager.Instance.UnregisterPooledInstance(child);
            ResourceManager.Instance.DestroyAsset(child);
        }
        Debug.Log("[PoolExample] 对象池已清理");
    }

    private void OnDestroy()
    {
        CleanupPool();
    }
}
