using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIMark：标记需要自动绑定的 UI 组件
    /// 挂载在子节点上，编辑器代码生成器通过此标记
    /// 生成 UIView.AutoBind() 的强类型实现
    ///
    /// 使用方式：
    /// 1. 在 Prefab 的子节点上挂载 UIMark 组件
    /// 2. 设置 Comment 作为字段注释（可选）
    /// 3. 右键 Prefab → "Generate UIView Code"
    /// 4. 代码生成器输出 partial 类的 AutoBind() 实现
    /// </summary>
    [AddComponentMenu("UIFrameworkLib/UIMark")]
    public class UIMark : MonoBehaviour
    {
        /// <summary>注释/别名（可空，用于生成的代码注释）</summary>
        public string Comment;
    }
}