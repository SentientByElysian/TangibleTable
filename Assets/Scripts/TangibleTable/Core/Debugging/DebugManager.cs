using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    public string label;
    public string value;
}

[System.Serializable]
public class TrackedObject
{
    public string     id;
    public string     label;
    public List<Stat> stats;
}

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;
    
    public Dictionary<string, TrackedObject> objects = new Dictionary<string, TrackedObject>();
    
    void Awake()
    {
        Instance = this;
    }
    
    public void Register(string id, string label, List<Stat> stats)
    {
        objects[id] = new TrackedObject { id = id, label = label, stats = stats };
    }
    
    public void UpdateObjs(string id, List<Stat> stats)
    {
        if (objects.ContainsKey(id))
            objects[id].stats = stats;
    }
    
    public void Unregister(string id)
    {
        objects.Remove(id);
    }
}