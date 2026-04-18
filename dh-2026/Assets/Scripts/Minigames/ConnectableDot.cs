using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Represents a single dot that can be connected to other dots in sequence.
/// Manages visibility, connection state, and visual feedback.
/// </summary>
public class ConnectableDot : MonoBehaviour
{
    [SerializeField] private int dotOrder = 0;
    
    [Header("Colors")]
    [SerializeField] private Color visibleColor = Color.white;
    [SerializeField] private Color hiddenColor = new Color(1, 1, 1, 0.2f);
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;

    [Header("Feedback")]
    [SerializeField] private float errorFlashDuration = 0.3f;

    private VisualElement visualElement;
    private bool isVisible = false;
    private bool isConnected = false;
    private float errorFlashTimer = 0f;
    private Vector2 worldPosition = Vector2.zero;

    void Awake()
    {
        // State will be initialized by DotConnectingMinigame
    }

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
        HandleErrorFlash();
    }

    /// <summary>
    /// Set the VisualElement that represents this dot visually
    /// </summary>
    public void SetVisualElement(VisualElement element)
    {
        visualElement = element;
    }

    /// <summary>
    /// Set whether this dot is visible to the player (based on cursor proximity)
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        
        isVisible = visible;
        UpdateDisplay();
    }

    /// <summary>
    /// Set whether this dot has been successfully connected
    /// </summary>
    public void SetConnected(bool connected)
    {
        if (isConnected == connected) return;
        
        isConnected = connected;
        UpdateDisplay();
    }

    /// <summary>
    /// Trigger an error flash when the wrong dot is clicked
    /// </summary>
    public void ShowError()
    {
        errorFlashTimer = errorFlashDuration;
    }

    /// <summary>
    /// Set the order of this dot in the connection sequence (for dynamic dot generation)
    /// </summary>
    public void SetDotOrder(int order)
    {
        dotOrder = order;
    }

    /// <summary>
    /// Reset the dot to its initial state
    /// </summary>
    public void Reset()
    {
        isConnected = false;
        isVisible = false;
        errorFlashTimer = 0f;
        UpdateDisplay();
    }

    /// <summary>
    /// Update the visual display based on current state
    /// </summary>
    private void UpdateDisplay()
    {
        if (visualElement != null)
        {
            if (isConnected)
            {
                visualElement.style.backgroundColor = connectedColor;
            }
            else
            {
                visualElement.style.backgroundColor = visibleColor;
            }

            visualElement.style.opacity = isVisible ? 1f : 0.2f;
        }
    }

    /// <summary>
    /// Handle the error flash animation
    /// </summary>
    private void HandleErrorFlash()
    {
        if (errorFlashTimer <= 0) return;

        errorFlashTimer -= Time.deltaTime;

        if (visualElement != null)
        {
            float flashFactor = Mathf.Sin(errorFlashTimer * Mathf.PI * 4) * 0.5f + 0.5f;
            Color flashColor = Color.Lerp(visibleColor, errorColor, flashFactor);
            visualElement.style.backgroundColor = flashColor;
        }

        if (errorFlashTimer <= 0)
        {
            UpdateDisplay();
        }
    }

    // ── Getters ──────────────────────────────────────────────

    public int GetDotOrder() => dotOrder;
    public bool IsVisible() => isVisible;
    public bool IsConnected() => isConnected;
    public Vector2 GetWorldPosition() => worldPosition;
    public VisualElement GetVisualElement() => visualElement;
    
    /// <summary>
    /// Set the world position for interaction detection (called by minigame)
    /// </summary>
    public void SetWorldPosition(Vector2 pos)
    {
        worldPosition = pos;
    }
}
