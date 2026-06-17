using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UIView 基类
    /// 职责：纯表现层
    /// - 动画通过 AnimationFactory 调用策略
    /// - 交互控制通过 CanvasGroup
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // === 子类重写 ===
        public virtual void PlayEnterAnimation()
        {
            AnimationFactory.GetStrategy(UIAnimationType.Fade).Enter((RectTransform)transform);
        }

        public virtual void PlayExitAnimation()
        {
            AnimationFactory.GetStrategy(UIAnimationType.Fade).Exit((RectTransform)transform);
        }

        // === 时长获取 ===
        public virtual int GetEnterAnimationDurationMs()
        {
            return AnimationFactory.GetStrategy(UIAnimationType.Fade).EnterDurationMs;
        }

        public virtual int GetExitAnimationDurationMs()
        {
            return AnimationFactory.GetStrategy(UIAnimationType.Fade).ExitDurationMs;
        }

        // === 交互控制 ===
        public void SetInteractive(bool enabled)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = enabled;
            _canvasGroup.blocksRaycasts = enabled;
        }

        public bool IsInteractable { get; internal set; }
    }
}
