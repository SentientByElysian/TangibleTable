using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UTool.Utility
{
    public static partial class UUtility
    {
        public static float Select(this Vector2 vector, bool selectX)
        {
            return selectX ? vector.x : vector.y;
        }

        public static string ToMinSec(this TimeSpan ts)
        {
            return $"{ts.Minutes.ToString("00")}:{ts.Seconds.ToString("00")}";
        }

        public static string ToSecMill(this TimeSpan ts)
        {
            return $"{ts.Seconds.ToString("00")}.{ts.Milliseconds.ToString("000")}";
        }

        public static string ToMinSecMill(this TimeSpan ts)
        {
            return $"{ts.Minutes.ToString("00")}:{ts.Seconds.ToString("00")}.{ts.Milliseconds.ToString("000")}";
        }

        public static string ToHexString(this string binaryString)
        {
            int decimalNumber = Convert.ToInt32(binaryString, 2);
            string hexadecimalNumber = Convert.ToString(decimalNumber, 16);

            return hexadecimalNumber;
        }

        public static void SetPivot(this RectTransform rectTransform, Vector2 pivot)
        {
            Vector3 deltaPosition = rectTransform.pivot - pivot;    // get change in pivot
            deltaPosition.Scale(rectTransform.rect.size);           // apply sizing
            deltaPosition.Scale(rectTransform.localScale);          // apply scaling
            deltaPosition = rectTransform.rotation * deltaPosition; // apply rotation

            rectTransform.pivot = pivot;                            // change the pivot
            rectTransform.localPosition -= deltaPosition;           // reverse the position change
        }

        public static void SetAnchors(this RectTransform This, Vector2 AnchorMin, Vector2 AnchorMax)
        {
            var OriginalPosition = This.localPosition;
            var OriginalSize = This.sizeDelta;

            This.anchorMin = AnchorMin;
            This.anchorMax = AnchorMax;

            This.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, OriginalSize.x);
            This.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, OriginalSize.y);
            This.localPosition = OriginalPosition;
        }

        public static Vector2 PreserveAspectRatio(this Vector2 size, Vector2 aspectRatio, bool preserveHeight = true)
        {
            if (preserveHeight)
                size.x = size.y * (aspectRatio.x / aspectRatio.y);
            else
                size.y = size.x * (aspectRatio.y / aspectRatio.x);

            return size;
        }

        public static void DestroyAllChild(this Transform transform)
        {
            while (transform.childCount > 0)
                GameObject.Destroy(transform.GetChild(0).gameObject);
        }

        public static void DestroyAllChildImmediate(this Transform transform)
        {
            while (transform.childCount > 0)
                GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
        }

        public static bool IsPrefabInScene(this UnityEngine.Object prefab)
        {
#if UNITY_EDITOR
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Regular)
                if (PrefabUtility.GetPrefabInstanceStatus(prefab) == PrefabInstanceStatus.Connected)
                    return true;
#endif
            return false;
        }

        public static bool IsPrefabSceneView()
        {
#if UNITY_EDITOR
            return PrefabStageUtility.GetCurrentPrefabStage();
#else
            return false;
#endif
        }

        public static void RecordPrefabChanges(this UnityEngine.Object uObject)
        {
#if UNITY_EDITOR
            PrefabUtility.RecordPrefabInstancePropertyModifications(uObject);
#endif
        }

        public static void ForceRecordPrefabChanges(this UnityEngine.Object uObject)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(uObject);
#endif
        }

        public static void ApplyPrefabChanges(this UnityEngine.Object uObject)
        {
#if UNITY_EDITOR
            PrefabUtility.ApplyObjectOverride(uObject, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(uObject), InteractionMode.AutomatedAction);
#endif
        }

        public static void ApplyAllPrefabChanges(this GameObject gameObject)
        {
#if UNITY_EDITOR
            PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
#endif
        }
    }
}