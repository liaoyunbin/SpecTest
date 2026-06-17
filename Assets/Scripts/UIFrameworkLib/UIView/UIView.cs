using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkLib
{
    /// <summary>
    /// 内置动画策略枚举
    /// PlayEnterAnimation / PlayExitAnimation 中调用对应 Helper 即可
    /// </summary>
    public enum UIAnimationType
    {
        Fade,       // 淡入淡出（默认）
        Scale,      // 缩放弹入弹
        SlideUp,    // 从下往上滑入 / 从上往下滑出
        SlideDown,  // 从上往下滑入 / 从下往上滑出
        BlackFade,  // 黑屏渐入渐出（搭配 CanvasGroup 和 Image 遮罩）
        None,       // 无动画，立即完成
    }

    /// <summary>
    /// UIView 基类
    /// 职责：纯表现层
    /// - 提供动画和交互控制接口
    /// - 子类在 Awake / OnInit 中手动绑定组件
    /// - 子类 override PlayEnterAnimation / PlayExitAnimation 选择动画效果
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        // ================================================================
        // 属性
        // ================================================================

        /// <summary>运行上下文（框架内部设置）</summary>
        public UIContext Context { get; private set; }

        /// <summary>关联的 Controller（跨池缓存周期持久化，非首次打开不重建）</summary>
        public IUIController Controller { get; internal set; }

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

        /// <summary>禁用所有 Selectable 组件（入场动画期间调用）</summary>
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

        /// <summary>恢复所有 Selectable 组件（入场动画结束时调用）</summary>
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
        // 默认动画（virtual，子类可重写）
        // ================================================================

        /// <summary>播放入场动画（默认淡入 0.3s）</summary>
        public virtual void PlayEnterAnimation(Action onComplete)
        {
            FadeEnter(onComplete, 0.3f);
        }

        /// <summary>播放退场动画（默认淡出 0.2s）</summary>
        public virtual void PlayExitAnimation(Action onComplete)
        {
            FadeExit(onComplete, 0.2f);
        }

        // ================================================================
        // 内置动画 Helper（子类可直接调用）
        // ================================================================

        /// <summary>淡入</summary>
        protected void FadeEnter(Action onComplete, float duration = 0.3f)
        {
            Fade(0f, 1f, duration, onComplete);
        }

        /// <summary>淡出</summary>
        protected void FadeExit(Action onComplete, float duration = 0.2f)
        {
            Fade(1f, 0f, duration, onComplete);
        }

        private async void Fade(float from, float to, float duration, Action onComplete)
        {
            if (_canvasGroup == null) { onComplete?.Invoke(); return; }

            _canvasGroup.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        /// <summary>缩放弹入（0→1，带弹性效果）</summary>
        protected void ScaleEnter(Action onComplete, float duration = 0.3f)
        {
            Scale(0f, 1f, duration, true, onComplete);
        }

        /// <summary>缩放弹出（1→0）</summary>
        protected void ScaleExit(Action onComplete, float duration = 0.2f)
        {
            Scale(1f, 0f, duration, false, onComplete);
        }

        private async void Scale(float from, float to, float duration, bool overshoot, Action onComplete)
        {
            transform.localScale = Vector3.one * from;
            var elapsed = 0f;

            if (overshoot)
            {
                // 弹性效果：先弹到 1.1，再回到 1.0
                var half = duration * 0.6f;
                while (elapsed < half)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / half;
                    transform.localScale = Vector3.one * Mathf.Lerp(from, 1.1f, t);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                elapsed = 0f;
                while (elapsed < duration * 0.4f)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / (duration * 0.4f);
                    transform.localScale = Vector3.one * Mathf.Lerp(1.1f, to, t);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / duration;
                    transform.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }

            transform.localScale = Vector3.one * to;
            onComplete?.Invoke();
        }

        /// <summary>从下滑入</summary>
        protected void SlideUpEnter(Action onComplete, float duration = 0.3f)
        {
            Slide(new Vector2(0f, -Screen.height * 0.3f), Vector2.zero, duration, onComplete);
        }

        /// <summary>向上滑出</summary>
        protected void SlideUpExit(Action onComplete, float duration = 0.2f)
        {
            Slide(Vector2.zero, new Vector2(0f, Screen.height * 0.3f), duration, onComplete);
        }

        /// <summary>从上滑入</summary>
        protected void SlideDownEnter(Action onComplete, float duration = 0.3f)
        {
            Slide(new Vector2(0f, Screen.height * 0.3f), Vector2.zero, duration, onComplete);
        }

        /// <summary>向下滑出</summary>
        protected void SlideDownExit(Action onComplete, float duration = 0.2f)
        {
            Slide(Vector2.zero, new Vector2(0f, -Screen.height * 0.3f), duration, onComplete);
        }

        private async void Slide(Vector2 from, Vector2 to, float duration, Action onComplete)
        {
            var rt = transform as RectTransform;
            if (rt == null) { onComplete?.Invoke(); return; }

            rt.anchoredPosition = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rt.anchoredPosition = Vector2.Lerp(from, to, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            rt.anchoredPosition = to;
            onComplete?.Invoke();
        }

        /// <summary>黑屏渐入渐出（需在 ui 根下放置全屏黑色 Image）</summary>
        /// <param name="blackImage">全屏黑色 Image 引用</param>
        protected void BlackFadeEnter(Image blackImage, Action onComplete, float fadeInDuration = 0.2f, float fadeOutDuration = 0.3f)
        {
            BlackFade(blackImage, true, fadeInDuration, fadeOutDuration, onComplete);
        }

        /// <summary>黑屏渐入渐出（退场）</summary>
        protected void BlackFadeExit(Image blackImage, Action onComplete, float fadeInDuration = 0.2f, float fadeOutDuration = 0.3f)
        {
            BlackFade(blackImage, false, fadeInDuration, fadeOutDuration, onComplete);
        }

        private async void BlackFade(Image blackImage, bool isEnter, float fadeIn, float fadeOut, Action onComplete)
        {
            if (blackImage == null) { onComplete?.Invoke(); return; }

            blackImage.gameObject.SetActive(true);
            blackImage.color = new Color(0, 0, 0, 0);
            blackImage.raycastTarget = true;

            // 黑入
            var elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                blackImage.color = new Color(0, 0, 0, elapsed / fadeIn);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            blackImage.color = new Color(0, 0, 0, 1);

            if (isEnter)
            {
                // 入场：黑屏 → 显示 UI → 黑出
                _canvasGroup.alpha = 1f;
            }
            else
            {
                // 退场：黑出前先确保 UI 隐藏
                _canvasGroup.alpha = 0f;
            }

            // 黑出
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                blackImage.color = new Color(0, 0, 0, 1f - elapsed / fadeOut);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            blackImage.color = new Color(0, 0, 0, 0);
            blackImage.raycastTarget = false;
            blackImage.gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        // ================================================================
        // 遮罩
        // ================================================================

        /// <summary>显示模态遮罩（Popup 使用，子类自行实现）</summary>
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