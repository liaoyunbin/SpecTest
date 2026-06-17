using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UI 对象池
    /// 用于缓存关闭的 UI Prefab，避免频繁 Instantiate / Destroy
    /// 每个 UIKey 最多缓存 MAX_PER_KEY 个实例
    /// </summary>
    public class UIPool
    {
        private readonly Dictionary<string, Stack<GameObject>> _pools = new();
        private const int MAX_PER_KEY = 5;

        /// <summary>池中对象总数</summary>
        public int TotalCount => _pools.Sum(kv => kv.Value.Count);

        /// <summary>
        /// 从池中取出一个对象
        /// </summary>
        /// <param name="key">UIKey</param>
        /// <returns>缓存的 GameObject，无缓存时返回 null</returns>
        public GameObject Get(string key)
        {
            if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
            {
                var go = stack.Pop();
                if (go != null)
                {
                    go.SetActive(true);
                    return go;
                }
            }
            return null;
        }

        /// <summary>
        /// 将对象归还池中
        /// </summary>
        /// <param name="key">UIKey</param>
        /// <param name="go">UI GameObject</param>
        public void Return(string key, GameObject go)
        {
            if (go == null) return;

            if (!_pools.ContainsKey(key))
                _pools[key] = new Stack<GameObject>();

            if (_pools[key].Count >= MAX_PER_KEY)
            {
                // 超出上限，直接销毁
                GameObject.Destroy(go);
            }
            else
            {
                go.SetActive(false);
                _pools[key].Push(go);
            }
        }

        /// <summary>清理所有缓存</summary>
        public void Clear()
        {
            foreach (var stack in _pools.Values)
            {
                while (stack.Count > 0)
                {
                    var go = stack.Pop();
                    if (go != null)
                        GameObject.Destroy(go);
                }
            }
            _pools.Clear();
        }
    }
}