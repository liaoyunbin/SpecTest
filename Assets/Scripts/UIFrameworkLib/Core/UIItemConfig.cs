namespace UIFrameworkLib
{
    /// <summary>
    /// UI 配置模型
    /// 由外部将原始配置（Luban / JSON / ScriptableObject）转换为本模型后注入框架
    /// </summary>
    public class UIItemConfig
    {
        /// <summary>UI 唯一标识（与 Controller 类名对应，如 "Shop"）</summary>
        public string UIKey { get; set; }

        /// <summary>预制体 Resources 路径</summary>
        public string PrefabPath { get; set; }

        /// <summary>层级（Background / Normal / Popup）</summary>
        public UILayer Layer { get; set; } = UILayer.Normal;
    }
}