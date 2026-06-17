using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UI 资源管理器
    /// 职责：从 Resources 异步加载 Prefab，返回原始 GameObject。
    /// 不负责实例化、不负责缓存、不负责对象池。
    /// 如需自定义加载方式（Addressables / AssetBundle），继承重写 <see cref="LoadPrefabAsync"/>。
    /// </summary>
    public class UIResourceLoader : Singleton<UIResourceLoader>
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
                Debug.LogError($"[UIResourceLoader] PrefabPath 为空");
                return null;
            }

            TimeoutGuard(prefabPath, 5f).Forget();

            var req = Resources.LoadAsync<GameObject>(prefabPath);
            await req.ToUniTask();

            if (req.asset == null)
            {
                Debug.LogError($"[UIResourceLoader] 加载失败: {prefabPath}");
                return null;
            }

            return req.asset as GameObject;
        }

        /// <summary>
        /// 超时看门狗（仅日志警告，不中断流程）
        /// </summary>
        private static async UniTaskVoid TimeoutGuard(string path, float timeoutSeconds)
        {
            await UniTask.Delay((int)(timeoutSeconds * 1000));
            Debug.LogWarning($"[UIResourceLoader] {path} 加载超过 {timeoutSeconds}s");
        }
    }
}