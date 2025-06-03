using UnityEngine;
using TMPro;

public class TextEntry : MonoBehaviour
{
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;
    
    public void Set(string label, string value)
    {
        labelText.text = label;
        valueText.text = value;
    }
}