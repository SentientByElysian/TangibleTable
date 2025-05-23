using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UTool.Utility;
using UTool.Tweening.Helper;

using DG.Tweening;

namespace UTool.Tweening
{
    public class TweenElement : MonoBehaviour
    {
        [SerializeField][BeginGroup] public TransformType transformType;
        [SerializeField][ShowIf(nameof(isRectTransform),true)] private RectTransform tweenRect;
        [SerializeField][EndGroup][ShowIf(nameof(isTransform),true)] private Transform tweenTransform;

        [SpaceArea]
     
        [SerializeField][BeginGroup][Disable] private int timeframe = 1000;
        [SerializeField][Disable] private int currentFrame = 0;
        [SerializeField][Disable] private int loopCounter = 0;
        [SerializeField][Disable] private bool loopCompleted = false;
        [SerializeField][Disable] private PlaybackState playbackState = PlaybackState.Idle;
        [SpaceArea]
        [SerializeField][Disable] private int totalProperty = 0;
        [SerializeField][Disable] private int completedProperty = 0;
        [SerializeField][Disable][Range(0, 1)] private float progress = 0;
        [SpaceArea]
        [SpaceArea]
        [SerializeField][Disable] private int totalSubTween = 0;
        [SerializeField][Disable] private int completedSubTween = 0;
        [SerializeField][EndGroup][Disable][Range(0, 1)] private float subTweenProgress = 0;

        [SpaceArea]
        [EditorButton(nameof(PlayTween), activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(ReverseTween), activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(StopTween), activityType: ButtonActivityType.OnPlayMode)]
        [SpaceArea]
      
        [SerializeField][BeginGroup][SearchableEnum] private AutoStartTween autoStart = AutoStartTween.Disabled;
        [SerializeField][SearchableEnum] private LoopMode loopMode = LoopMode.Disabled;
        [SerializeField] private int loopCount = -1;
        [SpaceArea]
        [SerializeField] private bool useSingleTween = true;
        [SpaceArea]
        [SerializeField] private TweenConfig playTweenConfig = new TweenConfig() { delay = 0, duration = 0.3f, ease = Ease.Linear };
        [SpaceArea]
        [SerializeField][HideIf(nameof(useSingleTween),false)] private TweenConfig reverseTweenConfig = new TweenConfig() { delay = 0, duration = 0.3f, ease = Ease.Linear };
        [SpaceArea]
        [SerializeField] private bool invertReverseTime = false;  
        [SerializeField][EndGroup] private bool invertKeyframeReverseTime = false;

        [SpaceArea]

        [SerializeField][LabelByChild("tweenPropertyType")][ReorderableList(Foldable = true)]
        public List<TweenProperty> tweenPropertyList = new List<TweenProperty>();

        [SpaceArea]

        [SerializeField][ReorderableList(Foldable = true)]
        public List<TweenAction> tweenActionList = new List<TweenAction>();

        [SpaceArea, Line(5)]

        [SerializeField][BeginGroup("Events")] public UnityEvent OnPlayRequest = new UnityEvent();
        [SerializeField] public UnityEvent OnPlayRequestComplete = new UnityEvent();
        [SpaceArea]
        [SerializeField] public UnityEvent OnReverseRequest = new UnityEvent();
        [SerializeField] public UnityEvent OnReverseRequestComplete = new UnityEvent();
        [SpaceArea, Line(5)]
        [SerializeField] public UnityEvent<bool> OnRequest = new UnityEvent<bool>();
        [SerializeField][EndGroup] public UnityEvent<bool> OnRequestComplete = new UnityEvent<bool>();

        private bool isRectTransform => transformType == TransformType.RectTransform;
        private bool isTransform => transformType == TransformType.Transform;

        private Tween tween;

        private bool latestRequestState;

        private List<Action> onCompleteCallbacks = new List<Action>();

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
                return;

            if (!tweenRect && !tweenTransform)
            {
                tweenRect = transform.GetComponent<RectTransform>();
                if (!tweenRect)
                {
                    tweenTransform = transform;
                    transformType = TransformType.Transform;
                }
            }

            SetDefaultValue();

            this.RecordPrefabChanges();
        }

        private void Awake()
        {
            foreach (TweenProperty tweenProperty in tweenPropertyList)
            {
                tweenProperty.OnPropertyChanged = UpdateProperty;
                tweenProperty.OnComplete = PropertyComplete;
            }

            foreach (TweenAction tAction in tweenActionList)
                if (tAction.tweenElement)
                    tAction.tweenElement.OnRequestComplete.AddListener((state) => OnSubTweenComplete());
        }

        private void Start()
        {
            foreach (TweenAction tAction in tweenActionList)
                tAction.Setup();

            switch(autoStart)
            {
                case AutoStartTween.Play: 
                    PlayTween(); 
                    break;

                case AutoStartTween.Reverse:
                    ReverseTween();
                    break;

                default:
                    break;
            }
        }

        private void SetDefaultValue()
        {
            if (transformType == TransformType.RectTransform)
            {
                if (!tweenRect)
                    return;
            }
            else
            {
                if (!tweenTransform)
                    return;
            }

            foreach (TweenProperty tweenProperty in tweenPropertyList)
            {
                if (transformType == TransformType.RectTransform)
                    tweenProperty.SetDefaultValue(tweenRect);
                else
                    tweenProperty.SetDefaultValue(tweenTransform);
            }
        }

        private void PopulateChildElements()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                TweenElement childElement = transform.GetChild(i).GetComponent<TweenElement>();
                if (!childElement)
                    continue;

                TweenAction aa = tweenActionList.Find(x => x.tweenElement == childElement);
                if (aa != null)
                    continue;

                TweenAction element = new TweenAction();
                element.tag = childElement.name;
                element.timeframe = 0;
                element.tweenElement = childElement;

                tweenActionList.Add(element);
            }

            this.RecordPrefabChanges();
        }

        private void HandleOnCompleteCallback()
        {
            playbackState = PlaybackState.Idle;

            List<Action> vaildOnCompleteCallbacks = new List<Action>(onCompleteCallbacks);
            onCompleteCallbacks.Clear();

            vaildOnCompleteCallbacks.ForEach(x => x?.Invoke());

            if (latestRequestState)
                OnPlayRequestComplete?.Invoke();
            else
                OnReverseRequestComplete?.Invoke();

            OnRequestComplete?.Invoke(latestRequestState);

            if (loopMode != LoopMode.Disabled)
            {
                if (loopCount != -1)
                    if (++loopCounter >= loopCount)
                    {
                        loopCompleted = true;
                        return;
                    }

                switch (loopMode)
                {
                    case LoopMode.PlayReverseRepeat:
                        if (latestRequestState)
                            ReverseTween();
                        else
                            PlayTween();
                        break;

                    case LoopMode.PlayRepeat:
                        ResetTween(latestRequestState);
                        if (latestRequestState)
                            PlayTween();
                        else
                            ReverseTween();
                        break;
                }
            }
        }

        private void SetupCallbacks()
        {
            onCompleteCallbacks.Clear();

            totalProperty = tweenPropertyList.Where(x => !x.disableTween).Count();
            completedProperty = 0;

            totalSubTween = tweenActionList.Where(x => x.tweenElement).Count();
            completedSubTween = 0;
        }

        private void CheckCompletion()
        {
            bool propertyCompleted = completedProperty == totalProperty;
            bool subTweenCompleted = completedSubTween == totalSubTween;

            if (propertyCompleted && subTweenCompleted)
                HandleOnCompleteCallback();
        }

        public void PlayTween(Action OnComplete) => Show(true, OnComplete);
        public void ReverseTween(Action OnComplete) => Show(false, OnComplete);

        public void PlayTween() => Show(true);
        public void ReverseTween() => Show(false);
        public void Show(bool state, Action OnComplete = null)
        {
            PlaybackState requestedPlayback = state ? PlaybackState.Playing : PlaybackState.Reverse;

            if (requestedPlayback == playbackState)
                return;

            KillTween();
            SetupCallbacks();

            if (loopCompleted)
            {
                loopCounter = 0;
                loopCompleted = false;
            }

            playbackState = requestedPlayback;
            //currentFrame = playbackState == PlaybackState.Playing ? 0 : timeframe;

            tweenPropertyList.ForEach(tweenProperty => tweenProperty.playbackState = PlaybackState.Ready);
            tweenActionList.ForEach(tweenAction => tweenAction.playbackState = PlaybackState.Ready);

            TweenConfig tc = playTweenConfig;
            if (!useSingleTween && playbackState == PlaybackState.Reverse)
                tc = reverseTweenConfig;

            tween = DOVirtual.Int(currentFrame, state ? timeframe : 0, tc.duration, Step)
                .SetDelay(tc.delay)
                .SetEase(tc.ease)
                .OnComplete(() =>
                {
                    //KillTween();
                });

            onCompleteCallbacks.Add(OnComplete);

            latestRequestState = state;

            if (latestRequestState)
                OnPlayRequest?.Invoke();
            else
                OnReverseRequest?.Invoke();

            OnRequest?.Invoke(latestRequestState);
        }

        private void Step(int step)
        {
            currentFrame = step;

            foreach (TweenAction tAction in tweenActionList)
            {
                if (playbackState == PlaybackState.Playing)
                {
                    if (tAction.timeframe > step)
                        continue;
                }
                else
                {
                    if (invertReverseTime)
                    {
                        if (tAction.timeframe > Mathf.Abs(step - timeframe))
                            continue;
                    }
                    else
                    {
                        if (tAction.timeframe < step)
                            continue;
                    }
                }

                if (tAction.playbackState == PlaybackState.Ready)
                {
                    tAction.playbackState = playbackState;
                    tAction.Trigger();
                }
            }

            foreach (TweenProperty tweenProperty in tweenPropertyList.Where(x => x.playbackState != playbackState))
            {
                if (playbackState == PlaybackState.Playing)
                {
                    if (tweenProperty.keyframe > step)
                        continue;
                }
                else
                {
                    if (invertKeyframeReverseTime)
                    {
                        if (tweenProperty.keyframe > Mathf.Abs(step - timeframe))
                            continue;
                    }
                    else
                    {
                        if (tweenProperty.keyframe < step)
                            continue;
                    }
                }

                if (tweenProperty.playbackState == PlaybackState.Ready)
                    tweenProperty.Show(playbackState == PlaybackState.Playing);
            }
        }

        public void ResetTween(bool state)
        {
            tween.KillTween();
            currentFrame = state ? 0 : 1000;
            playbackState = PlaybackState.Idle;

            tweenActionList.ForEach(tAction => tAction.ResetTween(state));
            tweenPropertyList.ForEach(tweenProperty => tweenProperty.ResetTween(state));
        }

        public void StopTween() => KillTween();
        private void KillTween()
        {
            tween.KillTween();
            playbackState = PlaybackState.Idle;

            tweenActionList.ForEach(tAction => tAction.KillTween());
            tweenPropertyList.ForEach(tweenProperty => tweenProperty.KillTween());
        }

        private void UpdateProperty(TweenPropertyType propertyType, Vector3 value)
        {
            if(transformType == TransformType.RectTransform)
                TweenHelper.UpdateProperty(tweenRect, propertyType, value);
            else
                TweenHelper.UpdateProperty(tweenTransform, propertyType, value);
        }

        private void PropertyComplete(TweenPropertyType propertyType)
        {
            completedProperty++;
            progress = UUtility.RangedMapClamp(completedProperty, 0, totalProperty, 0, 1);

            if (completedProperty == totalProperty)
                CheckCompletion();
        }

        private void OnSubTweenComplete()
        {
            completedSubTween++;
            subTweenProgress = UUtility.RangedMapClamp(completedSubTween, 0, totalSubTween, 0, 1);

            if (completedSubTween == totalSubTween)
                CheckCompletion();
        }

        [System.Serializable]
        public class TweenAction
        {
            [BeginGroup] public string tag;
            public int timeframe;
            [SpaceArea]
            public TweenElement tweenElement;
            [SpaceArea]
            [Disable] public PlaybackState playbackState = PlaybackState.Idle;
            [SpaceArea]
            public bool invertPlaybackState;
            public bool dontInvertAtStart;
            [SpaceArea]
            [EndGroup] public UnityEvent<PlaybackState> OnTrigger = new UnityEvent<PlaybackState>();

            public void Setup()
            {
                if (dontInvertAtStart)
                    return;

                if (invertPlaybackState)
                {
                    playbackState = PlaybackState.Reverse;
                    Trigger();
                }
            }

            public void Trigger()
            {
                PlaybackState ps = playbackState;

                if (invertPlaybackState)
                    if (ps != PlaybackState.Idle)
                        ps = playbackState == PlaybackState.Playing ? PlaybackState.Reverse : PlaybackState.Playing;

                if (tweenElement)
                    tweenElement.Show(ps == PlaybackState.Playing);

                OnTrigger?.Invoke(ps);
            }

            public void ResetTween(bool state)
            {
                if (tweenElement)
                    tweenElement.ResetTween(state);
            }

            public void KillTween()
            {
                if(tweenElement)
                    tweenElement.KillTween();

                playbackState = PlaybackState.Idle;
            }
        }
    }

    [System.Serializable]
    public class TweenProperty
    {
        [BeginGroup]

        [BeginGroup] public bool disableTween = false;
        [SpaceArea]
        public TweenPropertyType tweenPropertyType = TweenPropertyType.None;
        [ShowIf(nameof(IsCavasGroup),true)] public CanvasGroup canvasGroup;
        public int keyframe;
        [SpaceArea]
        [Disable] public PlaybackState playbackState = PlaybackState.Idle;
        [SpaceArea]
        [EndGroup] public bool useSingleTween = true;

        [SpaceArea]

        [BeginGroup] public bool useCurrentValue = true;
        public bool useAbsoluteEndValue = false;
        public Vector3 currentValue;
        [SpaceArea]
        public Vector3 from;
        [ShowIf(nameof(useAbsoluteEndValue),true)] public Vector3 to;
        [EndGroup][HideIf(nameof(useAbsoluteEndValue),false)] public Vector3 toOffset;

        [SpaceArea]

        public TweenConfig playTweenConfig = new TweenConfig() { delay = 0, duration = 0.3f, ease = Ease.InOutQuad };
        [SpaceArea]
        [EndGroup][HideIf(nameof(useSingleTween),false)] public TweenConfig reverseTweenConfig = new TweenConfig() { delay = 0, duration = 0.3f, ease = Ease.InOutQuad };

        [SpaceArea]

        [BeginGroup("Event"), EndGroup] public TweenEvent tweenEvent = new TweenEvent();

        Tween tween;

        public Action<TweenPropertyType, Vector3> OnPropertyChanged;
        public Action<TweenPropertyType> OnComplete;

        private bool IsCavasGroup => tweenPropertyType == TweenPropertyType.CanvasGroup;
        private bool IsCustom => tweenPropertyType == TweenPropertyType.Custom;

        private Vector3 endValue => useAbsoluteEndValue ? to : from + toOffset;

        public void PlayTween() => Show(true);
        public void ReverseTween() => Show(false);
        public void Show(bool state)
        {
            if (disableTween)
                return;

            KillTween();
            playbackState = state ? PlaybackState.Playing : PlaybackState.Reverse; ;

            TweenConfig tc = playTweenConfig;
            if (!useSingleTween && playbackState == PlaybackState.Reverse)
                tc = reverseTweenConfig;

            bool hidding = (state ? endValue : from).x == 0;
            tween = DOVirtual.Vector3(currentValue, state ? endValue : from, tc.duration, UpdateValue)
                .SetDelay(tc.delay)
                .SetEase(tc.ease)
                .OnStart(() =>
                {
                    switch (tweenPropertyType)
                    {
                        case TweenPropertyType.CanvasGroup:
                            canvasGroup.blocksRaycasts = false;
                            //canvasGroup.interactable = false;
                            break;
                    }

                    tweenEvent.Started(currentValue, state);
                })
                .OnComplete(() =>
                {
                    KillTween();

                    switch (tweenPropertyType)
                    {
                        case TweenPropertyType.CanvasGroup:

                            if (!hidding)
                                canvasGroup.blocksRaycasts = true;
                            //canvasGroup.interactable = state;
                            break;
                    }

                    tweenEvent.Completed(currentValue, state);
                    OnComplete?.Invoke(tweenPropertyType);
                });
        }

        private void UpdateValue(Vector3 vector)
        {
            switch (tweenPropertyType)
            {
                case TweenPropertyType.CanvasGroup:
                    canvasGroup.alpha = vector.x;
                    break;
            }

            currentValue = vector;

            tweenEvent.Updated(vector);
            OnPropertyChanged?.Invoke(tweenPropertyType, currentValue);
        }

        public void KillTween()
        {
            tween.KillTween();
            playbackState = PlaybackState.Idle;
        }

        public void ResetTween(bool state)
        {
            KillTween();
            currentValue = state ? from : endValue;
        }

        public void SetDefaultValue(RectTransform tweenRect)
        {
            if (!useCurrentValue)
                return;

            switch (tweenPropertyType)
            {
                case TweenPropertyType.Position:
                    from = tweenRect.anchoredPosition;
                    break;

                case TweenPropertyType.Scale:
                    from = tweenRect.localScale;
                    break;

                case TweenPropertyType.Rotation:
                    from = tweenRect.localRotation.eulerAngles;
                    break;

                case TweenPropertyType.Size:
                    from = tweenRect.sizeDelta;
                    break;

                case TweenPropertyType.Pivot:
                    from = tweenRect.pivot;
                    break;
            }

            SetDefaultSpecialPropertyValue();

            currentValue = from;
        }

        public void SetDefaultValue(Transform tweenTransform)
        {
            if (!useCurrentValue)
                return;

            switch (tweenPropertyType)
            {
                case TweenPropertyType.Position:
                    from = tweenTransform.localPosition;
                    break;

                case TweenPropertyType.Scale:
                    from = tweenTransform.localScale;
                    break;

                case TweenPropertyType.Rotation:
                    from = tweenTransform.localRotation.eulerAngles;
                    break;
            }

            SetDefaultSpecialPropertyValue();

            currentValue = from;
        }

        private void SetDefaultSpecialPropertyValue()
        {
            switch (tweenPropertyType)
            {
                case TweenPropertyType.CanvasGroup:
                    if (canvasGroup)
                        from = new Vector3(canvasGroup.alpha, 0, 0);
                    break;
            }
        }
    }

    [System.Serializable]
    public class TweenEvent
    {
        [BeginGroup] public UnityEvent OnPlay = new UnityEvent();
        public UnityEvent OnPlayComplete = new UnityEvent();
        public UnityEvent OnReverse = new UnityEvent();
        public UnityEvent OnReverseComplete = new UnityEvent();
        [SpaceArea]
        public UnityEvent<float> OnUpdateFloat = new UnityEvent<float>();
        public UnityEvent<Vector2> OnUpdateVector2 = new UnityEvent<Vector2>();
        public UnityEvent<Vector3> OnUpdateVector3 = new UnityEvent<Vector3>();
        [EndGroup] public UnityEvent<TweenEventType, Vector3> OnEvent = new UnityEvent<TweenEventType, Vector3>();

        public void Started(Vector3 vector, bool state)
        {
            if (state)
                OnPlay?.Invoke();
            else
                OnReverse?.Invoke();

            OnEvent?.Invoke(TweenEventType.Start, vector);
        }

        public void Updated(Vector3 vector)
        {
            OnUpdateFloat?.Invoke(vector.x);
            OnUpdateVector2?.Invoke(new Vector2(vector.x, vector.y));
            OnUpdateVector3?.Invoke(vector);

            OnEvent?.Invoke(TweenEventType.Update, vector);
        }

        public void Completed(Vector3 vector, bool state)
        {
            if (state)
                OnPlayComplete?.Invoke();
            else
                OnReverseComplete?.Invoke();

            OnEvent?.Invoke(TweenEventType.Completed, vector);
        }
    }

    [System.Serializable]
    public class TweenConfig
    {
        [BeginGroup] public float delay = 0;
        public float duration = 0.3f;
        [EndGroup][SearchableEnum] public Ease ease = Ease.InOutQuad;
    }

    public enum TransformType
    {
        RectTransform,
        Transform
    }

    public enum AutoStartTween
    {
        Disabled,
        Play,
        Reverse
    }

    public enum LoopMode
    {
        Disabled,
        PlayReverseRepeat,
        PlayRepeat,
    }

    public enum PlaybackState
    {
        Idle,
        Playing,
        Reverse,
        Ready
    }

    public enum TweenPropertyType
    {
        None,
        Position,
        Scale,
        Rotation,
        Size,
        Pivot,
        CanvasGroup,
        Custom
    }

    public enum TweenEventType
    {
        Start,
        Update,
        Completed
    }
}