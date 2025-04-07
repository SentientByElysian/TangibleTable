using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using UTool.Utility;
using UTool.Tweening;

namespace UTool.PageSystem
{
    public class Page : MonoBehaviour
    {
        [SerializeField] private TweenElement pageOpenCloseTE;
        [SpaceArea]
        [SerializeField][BeginGroup][Disable] private Page activeSubPage;
        [SpaceArea]
        [SerializeField][EndGroup] private int currentPageIndex = 0;
        [SpaceArea]
        [SerializeField][BeginGroup][SearchableEnum] private FadeType nextPageFadeType = FadeType.FadeOutIn;
        [SerializeField][SearchableEnum] private FadeType previousPageFadeType = FadeType.FadeOutIn;
        [SpaceArea]
        [SerializeField][SearchableEnum] private FadeType goToUpperPageFadeType = FadeType.FadeOutIn;
        [SerializeField][EndGroup][SearchableEnum] private FadeType goToLowerPageFadeType = FadeType.FadeOutIn;
        [SpaceArea]
        [EditorButton(nameof(OpenPage), activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(ClosePage), activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(NextPage), activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(PreviousPage), activityType: ButtonActivityType.OnPlayMode)]
        [SpaceArea, Line(5), SpaceArea]

        [SpaceArea]
        [EditorButton(nameof(GetSubPages))]
        [SerializeField] private Transform subPageHolder;
        [SerializeField][ReorderableList(Foldable = true)] private List<Page> subPages = new List<Page>();

        [SpaceArea, Line(5), SpaceArea]

        [SerializeField][BeginGroup("Events")] private UnityEvent OnOpening = new UnityEvent();
        [SerializeField] private UnityEvent OnOpened = new UnityEvent();
        [SpaceArea]
        [SerializeField] private UnityEvent OnClosing = new UnityEvent();
        [SerializeField] private UnityEvent OnClosed = new UnityEvent();
        [SpaceArea, Line(5), SpaceArea]
        [SerializeField] private UnityEvent OnStartPageReached = new UnityEvent();
        [SerializeField][EndGroup] private UnityEvent OnLastPageReached = new UnityEvent();

        #region Internal

        private void OpenPage() => Open();
        private void ClosePage() => Close();
        private void NextPage() => Next();
        private void PreviousPage() => Previous();

        public void _OnRequest(bool state)
        {
            if (state)
                OnOpening?.Invoke();
            else
                OnClosing.Invoke();
        }

        public void _OnComplete(bool state)
        {
            if (state)
                OnOpened?.Invoke();
            else
                OnClosed.Invoke();
        }
        
        private void GetSubPages()
        {
            subPages.Clear();

            for (int i = 0; i < subPageHolder.childCount; i++)
                if (subPageHolder.GetChild(i).TryGetComponent(out Page page))
                    subPages.Add(page);

            this.RecordPrefabChanges();
        }

        #endregion

        private void Awake()
        {
            for (int i = 0; i < subPages.Count; i++)
            {
                if (i == currentPageIndex)
                {
                    activeSubPage = subPages[i];
                    activeSubPage.Open();
                    continue;
                }

                subPages[i].Close();
            }
        }

        public void Open(Action onComplete = null)
        {
            pageOpenCloseTE.PlayTween(onComplete);
        }

        public void Close(Action onComplete = null)
        {
            pageOpenCloseTE.ReverseTween(onComplete);
        }

        public void GoTo(Page page, Action onComplete = null)
        {
            if (subPages.Contains(page))
                GoTo(subPages.IndexOf(page), onComplete);
            else
                Debug.LogWarning($"[Page] Cant GoTo SubPage : {page.name} | Not part of this Page", gameObject);
        }

        public void GoTo(int pageIndex, Action onComplete = null)
        {
            if (currentPageIndex == pageIndex)
                return;

            if(pageIndex >= 0 && pageIndex < subPages.Count)
            {
                if (pageIndex > currentPageIndex)
                    FadePage(pageIndex, goToUpperPageFadeType, onComplete);
                else
                    FadePage(pageIndex, goToLowerPageFadeType, onComplete);
            }
            else
                Debug.LogWarning($"[Page] Cant GoTo SubPage Index : {pageIndex} | Does not Exist", gameObject);
        }

        public void Next(Action onComplete = null)
        {
            int nextIndex = currentPageIndex + 1;

            if (nextIndex < subPages.Count)
                FadePage(nextIndex, nextPageFadeType, onComplete);
            else
                OnStartPageReached?.Invoke();
        }

        public void Previous(Action onComplete = null)
        {
            int previousIndex = currentPageIndex - 1;

            if (subPages.Count != 0 && previousIndex < subPages.Count && previousIndex >= 0)
                FadePage(previousIndex, previousPageFadeType, onComplete);
            else
                OnLastPageReached?.Invoke();
        }

        private void FadePage(int nextIndex, FadeType fadeType, Action onComplete = null)
        {
            Page currentPage = activeSubPage;
            Page nextPage = subPages[nextIndex];

            switch (fadeType)
            {
                case FadeType.FadeOutIn:
                    {
                        currentPage.Close(() =>
                        {
                            OnNextPage();
                            nextPage.Open(FadeComplete);
                        });
                    }
                    break;

                case FadeType.CrossFade:
                    {
                        int completedCount = 0;

                        currentPage.Close(() =>
                        {
                            OnNextPage();
                            CheckIfCompleted();
                        });
                        nextPage.Open(CheckIfCompleted);

                        void CheckIfCompleted()
                        {
                            completedCount++;

                            if (completedCount >= 2)
                                FadeComplete();
                        }
                    }
                    break;

                case FadeType.FadeInOut:
                    {
                        OnNextPage();
                        nextPage.Open(() =>
                        {
                            currentPage.Close(FadeComplete);
                        });
                    }
                    break;
            }

            void OnNextPage()
            {
                currentPageIndex = nextIndex;
                activeSubPage = nextPage;
            }

            void FadeComplete()
            {
                onComplete?.Invoke();
            }
        }

        private enum FadeType
        {
            FadeOutIn,
            CrossFade,
            FadeInOut
        }
    }
}