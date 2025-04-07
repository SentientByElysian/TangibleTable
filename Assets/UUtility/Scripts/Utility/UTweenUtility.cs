using System;
using UnityEngine;

using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace UTool.Utility
{
    public static partial class UUtility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tween"></param>
        /// <returns>Returns true if tween is killed</returns>
        public static bool KillTween(this Tween tween)
        {
            if (tween != null)
            {
                tween.Kill();
                tween = null;
                return true;
            }

            return false;
        }

        public static Tween FadeCanvasGroup(this CanvasGroup canvasGroup, bool state, float duration = 0.3f)
            => canvasGroup.FadeCanvasGroup(state ? 1 : 0, duration: duration);

        public static Tween FadeCanvasGroup(this CanvasGroup canvasGroup, float alpha, float blockRaycastThreshold = 0.5f, float duration = 0.3f)
        {
            Tween tween = canvasGroup.DOFade(alpha, duration)
               .OnStart(() => canvasGroup.CanvasGroupState(false))
               .OnComplete(() =>
               {
                   bool blockRaycast = alpha > blockRaycastThreshold;
                   if (blockRaycast)
                       canvasGroup.CanvasGroupState(true);
               });

            return tween;
        }

        private static void CanvasGroupState(this CanvasGroup canvasGroup, bool state)
        {
            canvasGroup.blocksRaycasts = state;
            //canvasGroup.interactable = state;
        }

        public static TweenerCore<Vector2, Vector2, VectorOptions> DOSizeDeltaX(this RectTransform target, float endValue, float duration, bool snapping = false)
            => target.DOSizeDelta(new Vector2(endValue, target.sizeDelta.y), duration, snapping);

        public static TweenerCore<Vector2, Vector2, VectorOptions> DOSizeDeltaY(this RectTransform target, float endValue, float duration, bool snapping = false)
            => target.DOSizeDelta(new Vector2(target.sizeDelta.x, endValue), duration, snapping);
    }
}