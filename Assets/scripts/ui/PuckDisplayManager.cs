using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuckDisplayManager : MonoBehaviour
{
    [Header("References")]
    public Surface surface;
    public PuckInfoCard puckInfoCardPrefab;
    public Transform cardContainer;

    [Header("Settings")]
    [Tooltip("Delay in seconds before removing a puck card after disconnection")]
    public float removalDelay = 1.0f;
    [Tooltip("Whether to show debug visualization")]
    public bool showDebugVisualization = true;
    [Tooltip("Size of debug markers")]
    public float debugMarkerSize = 0.2f;

    // Dictionary to keep track of active puck cards
    private Dictionary<int, PuckInfoCard> activePuckCards = new Dictionary<int, PuckInfoCard>();
    // Dictionary to track pending removals
    private Dictionary<int, Coroutine> pendingRemovals = new Dictionary<int, Coroutine>();
    // Dictionary to track debug markers
    private Dictionary<int, GameObject> debugMarkers = new Dictionary<int, GameObject>();

    private void Start()
    {
        // Subscribe to Surface events
        if (surface != null)
        {
            surface.OnObjectAdded += OnPuckAdded;
            surface.OnObjectUpdated += OnPuckUpdated;
            surface.OnObjectRemoved += OnPuckRemoved;
        }
        else
        {
            Debug.LogError("Surface reference not set in PuckDisplayManager!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from Surface events
        if (surface != null)
        {
            surface.OnObjectAdded -= OnPuckAdded;
            surface.OnObjectUpdated -= OnPuckUpdated;
            surface.OnObjectRemoved -= OnPuckRemoved;
        }

        // Stop all pending removal coroutines
        foreach (var coroutine in pendingRemovals.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        
        // Clean up debug markers
        foreach (var marker in debugMarkers.Values)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
    }

    private void OnPuckAdded(SurfaceObject puck, Vector2 position, float angle)
    {
        // If there's a pending removal for this puck, cancel it
        if (pendingRemovals.TryGetValue(puck.id, out Coroutine removalCoroutine))
        {
            StopCoroutine(removalCoroutine);
            pendingRemovals.Remove(puck.id);
        }

        if (activePuckCards.ContainsKey(puck.id))
        {
            // Card already exists, update it
            UpdatePuckCard(puck, position, angle);
        }
        else
        {
            // Create a new card
            PuckInfoCard newCard = Instantiate(puckInfoCardPrefab, cardContainer);
            newCard.Initialize(puck.id);
            newCard.UpdateInfo(position, angle);
            
            // Store the reference
            activePuckCards.Add(puck.id, newCard);
            
            // Log the position values
            Debug.Log($"Puck {puck.id} added at position: {position}, angle: {angle}");
        }
        
        // Create or update debug visualization
        if (showDebugVisualization)
        {
            UpdateDebugMarker(puck.id, position);
        }
    }

    private void OnPuckUpdated(SurfaceObject puck, Vector2 position, float angle)
    {
        // If there's a pending removal for this puck, cancel it
        if (pendingRemovals.TryGetValue(puck.id, out Coroutine removalCoroutine))
        {
            StopCoroutine(removalCoroutine);
            pendingRemovals.Remove(puck.id);
        }

        UpdatePuckCard(puck, position, angle);
        
        // Update debug visualization
        if (showDebugVisualization)
        {
            UpdateDebugMarker(puck.id, position);
        }
    }

    private void OnPuckRemoved(SurfaceObject puck)
    {
        // If the puck is already being removed, don't start another removal
        if (pendingRemovals.ContainsKey(puck.id))
            return;

        // Start a delayed removal
        Coroutine removalCoroutine = StartCoroutine(DelayedPuckRemoval(puck));
        pendingRemovals.Add(puck.id, removalCoroutine);
        
        // Remove debug visualization
        RemoveDebugMarker(puck.id);
    }

    private IEnumerator DelayedPuckRemoval(SurfaceObject puck)
    {
        yield return new WaitForSeconds(removalDelay);

        if (activePuckCards.TryGetValue(puck.id, out PuckInfoCard card))
        {
            // Remove and destroy the card
            activePuckCards.Remove(puck.id);
            Destroy(card.gameObject);
        }

        pendingRemovals.Remove(puck.id);
    }

    private void UpdatePuckCard(SurfaceObject puck, Vector2 position, float angle)
    {
        if (activePuckCards.TryGetValue(puck.id, out PuckInfoCard card))
        {
            card.UpdateInfo(position, angle);
        }
    }
    
    private void UpdateDebugMarker(int puckId, Vector2 position)
    {
        // Calculate the world position - adjust this based on your setup
        Vector3 worldPos = new Vector3(position.x, position.y, 0);
        
        if (!debugMarkers.TryGetValue(puckId, out GameObject marker))
        {
            // Create a new marker
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"DebugMarker_{puckId}";
            marker.transform.localScale = Vector3.one * debugMarkerSize;
            
            // Make it stand out
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material.color = Color.red;
            
            debugMarkers.Add(puckId, marker);
        }
        
        // Update position
        marker.transform.position = worldPos;
    }
    
    private void RemoveDebugMarker(int puckId)
    {
        if (debugMarkers.TryGetValue(puckId, out GameObject marker))
        {
            Destroy(marker);
            debugMarkers.Remove(puckId);
        }
    }
}
