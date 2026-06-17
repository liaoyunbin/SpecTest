using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIView 基类
    /// 职责：纯表现层，不涉及任何业务逻辑
    /// - 持有 UIMark 绑定的组件引用
    /// - 提供动画和交互控制接口
    /// - 由 UIMark + 代码生成器自动生成 AutoBind() 实现（不含反射）
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        // ================================================================
        // 属性
        // ================================================================

        /// <summary>运行上下文（框架内部设置）</summary>
        public UIContext Context { get; private set; }

        private CanvasGroup _canvasGroup;
        private Dictionary<Selectable, bool> _selectableSnapshot;

        // ================================================================
        // 生命周期
        // ================================================================

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ================================================================
        // 绑定（子类必须实现，由 UIMark 代码生成器自动生成）
        // ================================================================

        /// <summary>
        /// 自动绑定组件引用
        /// 由 UIMark 编辑器代码生成器生成实现
        /// </summary>
        protected abstract void AutoBind();

        // ================================================================
        // 框架内部调用
        // ================================================================

        /// <summary>设置上下文（由 UIManager 调用）</summary>
        public void Internal_SetContext(UIContext context)
        {
            Context = context;
        }

        // ================================================================
        // 交互控制
        // ================================================================

        /// <summary>设置整体交互状态（CanvasGroup 级别）</summary>
        public void SetInteractive(bool enabled)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = enabled;
            _canvasGroup.blocksRaycasts = enabled;
        }

        /// <summary>
        /// 禁用所有 Selectable 组件（入场动画期间调用）
        /// 保存当前状态快照用于后续恢复
        /// </summary>
        public void DisableAllSelectables()
        {
            var selectables = GetComponentsInChildren<Selectable>(true);
            _selectableSnapshot = new Dictionary<Selectable, bool>(selectables.Length);
            foreach (var s in selectables)
            {
                if (s == null) continue;
                _selectableSnapshot[s] = s.interactable;
                s.interactable = false;
            }
        }

        /// <summary>
        /// 恢复所有 Selectable 组件（入场动画结束时调用）
        /// </summary>
        public void RestoreSelectables()
        {
            if (_selectableSnapshot == null) return;

            foreach (var kv in _selectableSnapshot)
            {
                if (kv.Key != null)
                    kv.Key.interactable = kv.Value;
            }
            _selectableSnapshot = null;
        }

        // ================================================================
        // 过渡动画
        // ================================================================

        /// <summary>
        /// 播放入场动画（子类可重写自定义效果）
        /// 默认实现：CanvasGroup 淡入，持续 0.3 秒
        /// </summary>
        /// <param name="onComplete">动画完成回调</param>
        public virtual void PlayEnterAnimation(Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _canvasGroup.alpha = 0f;

            // 使用 DOTween（若项目中未安装 DOTween，可用协程替代）
            DOTween.To(() => _canvasGroup.alpha, v => _canvasGroup.alpha = v, 1f, 0.3f)
                   .SetEase(Ease.OutQuad)
                   .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 播放退场动画（子类可重写自定义效果）
        /// 默认实现：CanvasGroup 淡出，持续 0.2 秒
        /// </summary>
        /// <param name="onComplete">动画完成回调</param>
        public virtual void PlayExitAnimation(Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.To(() => _canvasGroup.alpha, v => _canvasGroup.alpha = v, 0f, 0.2f)
                   .SetEase(Ease.InQuad)
                   .OnComplete(() => onComplete?.Invoke());
        }

        // ================================================================
        // 遮罩
        // ================================================================

        /// <summary>显示模态遮罩（Popup 使用，由子类通过 UIMark 实现）</summary>
        public virtual void ShowMask() { }

        /// <summary>隐藏模态遮罩</summary>
        public virtual void HideMask() { }

        // ================================================================
        // 析构
        // ================================================================

        protected virtual void OnDestroy()
        {
            _selectableSnapshot = null;
        }
    }
}