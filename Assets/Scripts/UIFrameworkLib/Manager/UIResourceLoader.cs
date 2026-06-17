using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIFrameworkLib
{
    /// <summary>
    /// 资源加载结果
    /// </summary>
    public class UIResourceLoadResult
    {
        public GameObject Instance { get; set; }
        public UIView View { get; set; }
    }

    /// <summary>
    /// UI 资源加载器
    /// 职责：对象池获取 / Resources 异步加载 / 实例化 / UIView 组件解析
    /// 与 UIManager 解耦，便于替换加载方式（Addressables / AssetBundle）
    /// </summary>
    public class UIResourceLoader
    {
        private readonly UIPool _pool;

        /// <param name="pool">UI 对象池引用</param>
        public UIResourceLoader(UIPool pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        /// <summary>
        /// 异步加载并实例化 UI
        /// </summary>
        /// <param name="config">UI 配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载结果（含 Instance 和 UIView），失败返回 null</returns>
        public async UniTask<UIResourceLoadResult> LoadAsync(
            UIItemConfig config,
            CancellationToken cancellationToken)
        {
            if (config == null)
            {
                Debug.LogError("[UIResourceLoader] config 为空");
                return null;
            }

            var ct = cancellationToken;

            // 1. 优先从对象池获取
            var go = _pool.Get(config.UIKey);
            if (go != null)
            {
                // 从对象池取出：直接实例化
                return InstantiateUI(go, config);
            }

            // 2. 异步加载 Prefab
            go = await LoadPrefabAsync(config, ct);
            if (go == null || ct.IsCancellationRequested) return null;

            // 3. 实例化
            return InstantiateUI(go, config);
        }

        /// <summary>
        /// 异步加载 Prefab（从 Resources）
        /// 子类可重写以支持 Addressables / AssetBundle
        /// </summary>
        protected virtual async UniTask<GameObject> LoadPrefabAsync(
            UIItemConfig config,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(config.PrefabPath))
            {
                Debug.LogError($"[UIResourceLoader] {config.UIKey} PrefabPath 为空");
                return null;
            }

            var ct = cancellationToken;

            // 超时看门狗（仅日志警告，不阻塞）
            TimeoutGuard(config.UIKey, 5f).Forget();

            var req = Resources.LoadAsync<GameObject>(config.PrefabPath);
            await req.ToUniTask(cancellationToken: ct);

            if (ct.IsCancellationRequested) return null;

            var go = req.asset as GameObject;
            if (go == null)
            {
                Debug.LogError($"[UIResourceLoader] Prefab 加载失败: {config.PrefabPath}");
                return null;
            }

            return go;
        }

        /// <summary>
        /// 实例化 UI
        /// </summary>
        protected virtual UIResourceLoadResult InstantiateUI(GameObject prefab, UIItemConfig config)
        {
            var layerRoot = UIRoot.Instance.GetLayer(config.Layer);
            var instance = Object.Instantiate(prefab, layerRoot);
            instance.name = config.UIKey;

            var view = instance.GetComponent<UIView>();
            if (view == null)
            {
                Debug.LogError($"[UIResourceLoader] {config.UIKey} 缺少 UIView 组件");
                Object.Destroy(instance);
                return null;
            }

            return new UIResourceLoadResult
            {
                Instance = instance,
                View = view,
            };
        }

        /// <summary>
        /// 超时看门狗（仅日志警告，不中断流程）
        /// </summary>
        private static async UniTaskVoid TimeoutGuard(string uiKey, float timeoutSeconds)
        {
            await UniTask.Delay((int)(timeoutSeconds * 1000));
            Debug.LogWarning($"[UIResourceLoader] {uiKey} 加载超过 {timeoutSeconds}s");
        }
    }
}