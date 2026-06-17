using System;
using System.Collections.Generic;

namespace UIFrameworkLib
{
    /// <summary>
    /// Controller 注册表
    /// 启动时扫描并预创建所有 IUIController 实例，运行时直接返回
    /// </summary>
    public static class UIControllerRegistry
    {
        private static readonly Dictionary<string, IUIController> _all = new();

        /// <summary>初始化注册所有 Controller</summary>
        public static void Init()
        {
            _all.Clear();
            var types = Type.EmptyTypes;
            foreach (var t in types)
            {
                if (!typeof(IUIController).IsAssignableFrom(t) || t.IsInterface) continue;
                var ctrl = (IUIController)Activator.CreateInstance(t);
                _all[t.Name] = ctrl;
            }
        }

        /// <summary>通过 Type 获取 Controller</summary>
        public static IUIController GetController(Type key)
        {
            return _all.TryGetValue(key.Name, out var ctrl) ? ctrl : null;
        }
    }
}
