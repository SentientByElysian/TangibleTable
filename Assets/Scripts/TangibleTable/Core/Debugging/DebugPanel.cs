using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugPanel : MonoBehaviour
{
    public TextEntry textEntryPrefab;
    public TextMeshProUGUI objectLabelPrefab;
    
    Canvas canvas;
    ScrollRect scrollRect;
    RectTransform content;
    Dictionary<string, GameObject> objectContainers = new Dictionary<string, GameObject>();
    
    void Start()
    {
        CreateUI();
    }
    
    void CreateUI()
    {
        GameObject canvasObj = new GameObject("Debug Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.7f, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        GameObject scrollObj = new GameObject("Scroll View");
        scrollObj.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -10);
        this.scrollRect = scrollObj.AddComponent<ScrollRect>();
        
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>();
        viewport.AddComponent<Mask>();
        
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        content = contentObj.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 10;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        this.scrollRect.content = content;
        this.scrollRect.viewport = viewportRect;
    }
    
    void Update()
    {
        if (DebugManager.Instance == null) return;
        
        var currentObjects = new HashSet<string>(DebugManager.Instance.objects.Keys);
        var existingObjects = new HashSet<string>(objectContainers.Keys);
        
        foreach (var id in existingObjects)
        {
            if (!currentObjects.Contains(id))
            {
                Destroy(objectContainers[id]);
                objectContainers.Remove(id);
            }
        }
        
        foreach (var kvp in DebugManager.Instance.objects)
        {
            if (!objectContainers.ContainsKey(kvp.Key))
            {
                CreateObjectContainer(kvp.Key, kvp.Value);
            }
            else
            {
                UpdateObjectContainer(kvp.Key, kvp.Value);
            }
        }
    }
    
    void CreateObjectContainer(string id, TrackedObject obj)
    {
        GameObject container = new GameObject($"Object_{id}");
        container.transform.SetParent(content, false);
        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.spacing = 2;
        
        TextMeshProUGUI label = Instantiate(objectLabelPrefab, container.transform);
        label.text = obj.label;
        label.fontStyle = FontStyles.Bold;
        
        foreach (var stat in obj.stats)
        {
            TextEntry entry = Instantiate(textEntryPrefab, container.transform);
            entry.Set(stat.label, stat.value);
        }
        
        objectContainers[id] = container;
    }
    
    void UpdateObjectContainer(string id, TrackedObject obj)
    {
        GameObject container = objectContainers[id];
        TextEntry[] entries = container.GetComponentsInChildren<TextEntry>();
        
        for (int i = 0; i < obj.stats.Count && i < entries.Length; i++)
        {
            entries[i].Set(obj.stats[i].label, obj.stats[i].value);
        }
        
        if (obj.stats.Count != entries.Length)
        {
            Destroy(container);
            objectContainers.Remove(id);
            CreateObjectContainer(id, obj);
        }
    }
}