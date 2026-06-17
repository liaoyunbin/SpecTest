using System.Collections.Generic;

namespace UIFrameworkLib
{
    /// <summary>
    /// UI 配置加载器
    /// 从外部数据源加载 UIItemConfig，与具体数据格式解耦
    /// 外部数据源（Luban / ScriptableObject / JSON）自行转换为 List&lt;UIItemConfig&gt;
    /// </summary>
    public class UIConfigLoader
    {
        private readonly Dictionary<string, UIItemConfig> _configMap = new();

        /// <summary>
        /// 批量加载配置
        /// </summary>
        /// <param name="configs">UIItemConfig 列表</param>
        public void Load(List<UIItemConfig> configs)
        {
            _configMap.Clear();
            foreach (var config in configs)
            {
                if (string.IsNullOrEmpty(config.UIKey))
                {
                    UnityEngine.Debug.LogWarning("[UIFrameworkLib] 配置中存在空 UIKey，已跳过");
                    continue;
                }
                _configMap[config.UIKey] = config;
            }
            UnityEngine.Debug.Log($"[UIFrameworkLib] 已加载 {_configMap.Count} 条 UI 配置");
        }

        /// <summary>
        /// 根据 UIKey 获取配置
        /// </summary>
        /// <param name="uiKey">UI 标识</param>
        /// <returns>UIItemConfig，未找到时返回 null</returns>
        public UIItemConfig Get(string uiKey)
        {
            return _configMap.TryGetValue(uiKey, out var config) ? config : null;
        }

        /// <summary>
        /// 获取所有 UIKey
        /// </summary>
        public string[] GetAllKeys()
        {
            var keys = new string[_configMap.Count];
            _configMap.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// 运行时热重载
        /// </summary>
        public void Reload(List<UIItemConfig> configs)
        {
            Load(configs);
        }
    }
}