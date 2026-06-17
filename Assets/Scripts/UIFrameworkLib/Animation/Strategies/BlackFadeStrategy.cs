using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UIFrameworkLib
{
    /// <summary>黑屏渐入渐出策略</summary>
    public class BlackFadeStrategy : IAnimationStrategy
    {
        public int EnterDurationMs => 300;
        public int ExitDurationMs => 200;

        public void Enter(RectTransform target)
        {
            // 查找全屏黑色 Image
            var blackImage = FindBlackImage(target);
            if (blackImage == null) return;

            blackImage.gameObject.SetActive(true);
            blackImage.color = new Color(0, 0, 0, 0);
            blackImage.raycastTarget = true;

            float duration = AnimUtils.MsToSeconds(EnterDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blackImage.color = new Color(0, 0, 0, elapsed / duration);
                UniTask.Yield(PlayerLoopTiming.Update);
            }
            blackImage.color = new Color(0, 0, 0, 1);
        }

        public void Exit(RectTransform target)
        {
            var blackImage = FindBlackImage(target);
            if (blackImage == null) return;

            float duration = AnimUtils.MsToSeconds(ExitDurationMs);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blackImage.color = new Color(0, 0, 0, 1f - elapsed / duration);
                UniTask.Yield(PlayerLoopTiming.Update);
            }
            blackImage.color = new Color(0, 0, 0, 0);
            blackImage.raycastTarget = false;
            blackImage.gameObject.SetActive(false);
        }

        private static Image FindBlackImage(Transform root)
        {
            return root.GetComponentInChildren<Image>(true);
        }
    }
}
