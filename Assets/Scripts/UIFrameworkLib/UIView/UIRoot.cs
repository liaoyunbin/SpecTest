using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIRoot：UI 根节点
    ///
    /// 层级结构（由低到高）：
    ///   [UIRoot] (Canvas, SortingOrder=10000)
    ///   ├── BackgroundLayer  (SortingOrder=0)
    ///   ├── NormalLayer      (SortingOrder=100)
    ///   └── PopupLayer       (SortingOrder=200)
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        public Transform BackgroundLayer { get; private set; }
        public Transform NormalLayer { get; private set; }
        public Transform PopupLayer { get; private set; }

        /// <summary>
        /// 场景加载前自动初始化 UIRoot
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;

            var go = new GameObject("[UIRoot]");
            DontDestroyOnLoad(go);

            Instance = go.AddComponent<UIRoot>();

            // 主 Canvas
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // 创建层级子节点（由低到高）
            Instance.BackgroundLayer = CreateLayer(go.transform, "BackgroundLayer", 0);
            Instance.NormalLayer     = CreateLayer(go.transform, "NormalLayer",     100);
            Instance.PopupLayer      = CreateLayer(go.transform, "PopupLayer",      200);

            Debug.Log("[UIFrameworkLib] UIRoot 初始化完成");
        }

        /// <summary>
        /// 根据 UILayer 获取对应的 Transform
        /// </summary>
        public Transform GetLayer(UILayer layer)
        {
            return layer switch
            {
                UILayer.Background => BackgroundLayer,
                UILayer.Normal     => NormalLayer,
                UILayer.Popup      => PopupLayer,
                _ => NormalLayer,
            };
        }

        private static Transform CreateLayer(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            go.AddComponent<GraphicRaycaster>();

            return go.transform;
        }
    }
}