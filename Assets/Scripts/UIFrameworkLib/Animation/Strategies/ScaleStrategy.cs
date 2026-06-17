using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UIFrameworkLib
{
    /// <summary>缩放弹入弹出策略</summary>
    public class ScaleStrategy : IAnimationStrategy
    {
        public int EnterDurationMs => 300;
        public int ExitDurationMs => 200;

        public async void Enter(RectTransform target)
        {
            target.localScale = Vector3.zero;
            float duration = AnimUtils.MsToSeconds(EnterDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            target.localScale = Vector3.one;
        }

        public async void Exit(RectTransform target)
        {
            target.localScale = Vector3.one;
            float duration = AnimUtils.MsToSeconds(ExitDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            target.localScale = Vector3.zero;
        }
    }
}
