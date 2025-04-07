using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using UTool.Utility;

using DG.Tweening;

namespace UTool
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float startDelay = 0;
        [SerializeField] private float holdDuration = 1;
        [Space]
        [SerializeField] public UnityEvent OnHeld = new UnityEvent();

        /// <summary>
        /// Int = State of Hold. 0 = Start Of Hold, 1 = Holding State, 2 = End Of Hold
        /// Float = Value from 0 to 1 of Hold Duration
        /// </summary>
        [SerializeField] public UnityEvent<int, float> OnHold = new UnityEvent<int, float>();

        Tween holdTween;

        private int currentPointer = -99;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (currentPointer != -99)
                return;

            currentPointer = eventData.pointerId;
            StartHold();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            CancelHold(eventData.pointerId);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            CancelHold(eventData.pointerId);
        }

        private void StartHold()
        {
            holdTween = DOVirtual.Float(0, 1, holdDuration, HoldUpdate)
                .SetEase(Ease.Linear)
                .SetDelay(startDelay)
                .OnStart(HoldStart)
                .OnComplete(HoldEnd);
        }

        private void CancelHold(int id)
        {
            if (currentPointer != id)
                return;

            currentPointer = -99;

            holdTween.KillTween();
            OnHold?.Invoke(-1, 0);
        }

        private void HoldStart()
        {
            OnHold?.Invoke(0, 0);
        }

        private void HoldUpdate(float value)
        {
            OnHold?.Invoke(1, value);
        }

        private void HoldEnd()
        {
            OnHold?.Invoke(2, 1);
            OnHeld?.Invoke();
        }
    }
}