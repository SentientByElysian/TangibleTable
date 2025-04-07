using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UTool.Utility
{
    public static partial class UUtility
    {
        public static string ColorCoat(this string text, Color color)
        {
            return text.ColorCoat(ColorUtility.ToHtmlStringRGB(color));
        }

        public static string ColorCoat(this string text, string htmlStringRGB)
        {
            return $"<color=#{htmlStringRGB}>{text}</color>";
        }

        public static string ToMinSec(this float time)
        {
            TimeSpan ts = TimeSpan.FromSeconds(time);
            return ts.ToMinSec();
        }

        public static string ToSecMill(this float time)
        {
            TimeSpan ts = TimeSpan.FromSeconds(time);
            return ts.ToSecMill();
        }

        public static string ToMinSecMill(this float time)
        {
            TimeSpan ts = TimeSpan.FromSeconds(time);
            return ts.ToMinSecMill();
        }

        public static string ToddMMyyyyhhmmss(this DateTime dateTime)
        {
            return dateTime.ToString("dd_MM_yyyy_hh_mm_ss");
        }

        public static string Tohhmmss(this DateTime dateTime, string spacer = "_")
        {
            return dateTime.ToString($"hh{spacer}mm{spacer}ss");
        }

        public static string ToddMMyyyy(this DateTime dateTime, string spacer = "_")
        {
            return dateTime.ToString($"dd{spacer}MM{spacer}yyyy");
        }
    }
}