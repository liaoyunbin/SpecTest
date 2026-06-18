using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// 资源加载器（单例）
    /// 职责：从 Resources 异步加载 Prefab，返回原始 GameObject。
    /// 如需自定义加载方式（Addressables / AssetBundle），继承重写 <see cref="LoadPrefabAsync"/>。
    /// </summary>
    public class AssetMgr : Singleton<AssetMgr>
    {
        /// <summary>
        /// 异步加载 Prefab
        /// </summary>
        /// <param name="prefabPath">Resources 中的 Prefab 路径</param>
        /// <returns>加载的 Prefab 原始引用（未实例化），失败返回 null</returns>
        public async UniTask<GameObject> LoadPrefabAsync(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"[AssetMgr] PrefabPath 为空");
                return null;
            }

            var req = Resources.LoadAsync<GameObject>(prefabPath);
            await req.ToUniTask();

            if (req.asset == null)
            {
                Debug.LogError($"[AssetMgr] 加载失败: {prefabPath}");
                return null;
            }

            return req.asset as GameObject;
        }
    }
}
