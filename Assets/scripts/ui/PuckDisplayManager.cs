using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuckDisplayManager : MonoBehaviour
{
    [Header("References")]
    public Surface surface;
    public PuckInfoCard puckInfoCardPrefab;
    public Transform cardContainer;

    // Dictionary to keep track of active puck cards
    private Dictionary<int, PuckInfoCard> activePuckCards = new Dictionary<int, PuckInfoCard>();

    private void Start()
    {
        // Subscribe to Surface events
        if (surface != null)
        {
            surface.OnObjectAdded += OnPuckAdded;
            surface.OnObjectUpdated += OnPuckUpdated;
            // surface.OnObjectRemoved += OnPuckRemoved;
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
            // surface.OnObjectRemoved -= OnPuckRemoved;
        }
    }

    private void OnPuckAdded(SurfaceObject puck, Vector2 position, float angle)
    {
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
            // activePuckCards[puck.id] = newCard;
            activePuckCards.Add(puck.id, newCard);
        }
    }

    private void OnPuckUpdated(SurfaceObject puck, Vector2 position, float angle)
    {
        UpdatePuckCard(puck, position, angle);
    }

    private void OnPuckRemoved(SurfaceObject puck)
    {
        if (activePuckCards.TryGetValue(puck.id, out PuckInfoCard card))
        {
            // Remove and destroy the card
            activePuckCards.Remove(puck.id);
            Destroy(card.gameObject);
        }
    }

    private void UpdatePuckCard(SurfaceObject puck, Vector2 position, float angle)
    {
        if (activePuckCards.TryGetValue(puck.id, out PuckInfoCard card))
        {
            card.UpdateInfo(position, angle);
        }
    }
}
