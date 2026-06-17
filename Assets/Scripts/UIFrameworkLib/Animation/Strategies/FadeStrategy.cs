using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UIFrameworkLib
{
    /// <summary>淡入淡出策略</summary>
    public class FadeStrategy : IAnimationStrategy
    {
        public int EnterDurationMs => 300;
        public int ExitDurationMs => 200;

        public void Enter(RectTransform target)
        {
            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = 0f;
            float duration = AnimUtils.MsToSeconds(EnterDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                UniTask.Yield(PlayerLoopTiming.Update).Forget();
            }
            cg.alpha = 1f;
        }

        public void Exit(RectTransform target)
        {
            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null) return;
            cg.alpha = 1f;
            float duration = AnimUtils.MsToSeconds(ExitDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                UniTask.Yield(PlayerLoopTiming.Update).Forget();
            }
            cg.alpha = 0f;
        }
    }
}
