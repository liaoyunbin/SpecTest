using System;

namespace UIFrameworkLib
{
    /// <summary>
    /// 泛型单例基类
    /// 继承本类的子类可通过 <c>T.Instance</c> 访问全局唯一实例
    ///
    /// 使用方式：
    /// <code>
    /// public class MyManager : Singleton&lt;MyManager&gt;
    /// {
    ///     public MyManager() { }  // 公开构造（new() 约束要求）
    /// }
    ///
    /// MyManager.Instance.DoSomething();
    /// </code>
    /// </summary>
    /// <typeparam name="T">子类类型（必须具有公开无参构造）</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lock = new();

        /// <summary>全局唯一实例</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }
    }
}