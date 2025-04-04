using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuckInfoCard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI positionXText;
    public TextMeshProUGUI positionYText;
    public TextMeshProUGUI angleText;

    private int puckId;

    public void Initialize(int id)
    {
        puckId = id;
        titleText.text = $"Puck: {puckId}";
    }

    public void UpdateInfo(Vector2 position, float angle)
    {
        // Update position values with 2 decimal places
        positionXText.text = $"{position.x:F2}";
        positionYText.text = $"{position.y:F2}";
        angleText.text = $"{angle:F1}°";
    }
}
