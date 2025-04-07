using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TNRD.Autohook;
using UnityEngine;
using UTool.Utility;

public class Keyboard : MonoBehaviour
{
    [SerializeField][AutoHook(SearchArea = AutoHookSearchArea.Parent)] private OSK osk;
    [SpaceArea]
    [SerializeField] private CanvasGroup cg;
    [SpaceArea]
    [SerializeField] private bool active = false;

    Tween cgTween;

    public void Toggle()
    {
        active = !active;

        cgTween.KillTween();
        cgTween = cg.FadeCanvasGroup(active, duration: 0.2f).SetDelay(active? 0.15f : 0);
    }

    public void OnKeyPress(KeyType keyType, string keyValue)
    {
        osk.OnKeyPress(keyType, keyValue);
    }

    public enum KeyType
    {
        Char = 0,
        None = 1,
        Backspace = 2,
        Submit = 3,
        NumberBoard = 4,
        SwitchBoard = 5,
    }
}
