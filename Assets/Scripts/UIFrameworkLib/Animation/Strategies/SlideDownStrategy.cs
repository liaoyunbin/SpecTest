using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>从上往下滑入 / 往下滑出策略</summary>
    public class SlideDownStrategy : IAnimationStrategy
    {
        public int EnterDurationMs => 300;
        public int ExitDurationMs => 200;

        public void Enter(RectTransform target)
        {
            target.anchoredPosition = new Vector2(0f, Screen.height * 0.3f);
            float duration = AnimUtils.MsToSeconds(EnterDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.anchoredPosition = Vector2.Lerp(
                    new Vector2(0f, Screen.height * 0.3f), Vector2.zero, t);
                UniTask.Yield(PlayerLoopTiming.Update);
            }
            target.anchoredPosition = Vector2.zero;
        }

        public void Exit(RectTransform target)
        {
            target.anchoredPosition = Vector2.zero;
            float duration = AnimUtils.MsToSeconds(ExitDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.anchoredPosition = Vector2.Lerp(
                    Vector2.zero, new Vector2(0f, -Screen.height * 0.3f), t);
                UniTask.Yield(PlayerLoopTiming.Update);
            }
            target.anchoredPosition = new Vector2(0f, -Screen.height * 0.3f);
        }
    }
}
