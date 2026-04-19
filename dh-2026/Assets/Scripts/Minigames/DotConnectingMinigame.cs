using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DotConnectingMinigame : MonoBehaviour
{
    public static DotConnectingMinigame Instance { get; private set; }

    [SerializeField] private float cursorVisibilityRadius = 100f;
    [SerializeField] private float dotInteractionRadius   = 30f;
    [SerializeField] private int   minDots = 7;
    [SerializeField] private int   maxDots = 10;
    [SerializeField] private Color dotColor = Color.cyan;

    private VisualElement dotsContainer;
    private VisualElement linesContainer;
    private VisualElement minigameElement;

    private List<ConnectableDot> dots = new List<ConnectableDot>();
    private int  nextDotIndex = 0;
    private bool isGameActive = false;

    // Current mouse position in dotsContainer local space — updated by pointer events
    private Vector2 localMousePos;

    private event System.Action gameCompletedEvent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        AutoSetup();
    }

    private void AutoSetup()
    {
        var uiDoc = GetComponentInParent<UIDocument>() ?? FindAnyObjectByType<UIDocument>();
        if (uiDoc == null) { Debug.LogError("No UIDocument found."); return; }

        var root = uiDoc.rootVisualElement;
        minigameElement = root.Q("DotConnectingMinigame");
        if (minigameElement == null) { Debug.LogError("DotConnectingMinigame element not found in UXML."); return; }

        dotsContainer = minigameElement.Q("DotsContainer");
        if (dotsContainer == null) { Debug.LogError("DotsContainer not found."); return; }

        dotsContainer.style.backgroundColor = Color.black;

        linesContainer = new VisualElement();
        linesContainer.name = "LinesContainer";
        linesContainer.style.position = Position.Absolute;
        linesContainer.style.left   = 0;
        linesContainer.style.top    = 0;
        linesContainer.style.right  = 0;
        linesContainer.style.bottom = 0;
        linesContainer.pickingMode  = PickingMode.Ignore;
        minigameElement.Insert(0, linesContainer);

        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        root.RegisterCallback<PointerDownEvent>(OnPointerDown);

        dotsContainer.RegisterCallback<GeometryChangedEvent>(OnContainerReady);
    }

    private void OnContainerReady(GeometryChangedEvent evt)
    {
        dotsContainer.UnregisterCallback<GeometryChangedEvent>(OnContainerReady);
        GenerateRandomDots();
        isGameActive = true;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isGameActive) return;
        // evt.position is panel space — WorldToLocal converts it to dotsContainer local space
        localMousePos = dotsContainer.WorldToLocal(evt.position);
        UpdateDotVisibility();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!isGameActive || evt.button != 0) return;
        localMousePos = dotsContainer.WorldToLocal(evt.position);
        HandleDotInteraction();
    }

    void GenerateRandomDots()
    {
        // Use panel layout dimensions so dots always span the full visible area,
        // regardless of how DotsContainer is sized in UXML.
        float w = dotsContainer.resolvedStyle.width;
        float h = dotsContainer.resolvedStyle.height;

        // Fallback: if container has no explicit size, use the panel root size
        if (w < 1 || h < 1)
        {
            var layout = dotsContainer.panel.visualTree.layout;
            w = layout.width;
            h = layout.height;
        }

        Debug.Log($"Dot area: {w} x {h}");

        int   dotCount   = Random.Range(minDots, maxDots + 1);
        float minDist    = 80f;
        float padX       = 80f;
        float padY       = 80f;
        var   used       = new List<Vector2>();

        for (int i = 0; i < dotCount; i++)
        {
            Vector2 pos    = Vector2.zero;
            bool    valid  = false;
            int     tries  = 0;

            while (!valid && tries < 30)
            {
                pos = new Vector2(
                    Random.Range(padX, w - padX),
                    Random.Range(padY, h - padY)
                );
                valid = true;
                foreach (var u in used)
                    if (Vector2.Distance(pos, u) < minDist) { valid = false; break; }
                tries++;
            }

            if (valid) { CreateDot(i, pos); used.Add(pos); }
        }

        Debug.Log($"Generated {dots.Count} dots");
    }

    void CreateDot(int order, Vector2 center)
    {
        float r = 20f;

        var el = new VisualElement();
        el.name = $"Dot_{order}";
        el.style.position = Position.Absolute;
        el.style.left     = center.x - r;
        el.style.top      = center.y - r;
        el.style.width    = r * 2;
        el.style.height   = r * 2;
        el.style.borderBottomLeftRadius  = r;
        el.style.borderBottomRightRadius = r;
        el.style.borderTopLeftRadius     = r;
        el.style.borderTopRightRadius    = r;
        el.style.backgroundColor = dotColor;
        el.style.opacity     = 0f;
        el.pickingMode       = PickingMode.Ignore;
        dotsContainer.Add(el);

        var go = new GameObject($"Dot_{order}");
        go.transform.SetParent(transform);
        go.hideFlags = HideFlags.HideInHierarchy;

        var cd = go.AddComponent<ConnectableDot>();
        cd.SetDotOrder(order);
        cd.SetVisualElement(el);
        cd.SetWorldPosition(center); // center in container-local space
        dots.Add(cd);
    }

    void UpdateDotVisibility()
    {
        foreach (var dot in dots)
            dot.SetVisible(Vector2.Distance(localMousePos, dot.GetWorldPosition()) <= cursorVisibilityRadius);
    }

    void HandleDotInteraction()
    {
        foreach (var dot in dots)
        {
            if (!dot.IsVisible()) continue;
            if (Vector2.Distance(localMousePos, dot.GetWorldPosition()) > dotInteractionRadius) continue;

            if (dot.GetDotOrder() == nextDotIndex)
            {
                int justConnected = nextDotIndex;
                dot.SetConnected(true);
                nextDotIndex++;

                UIManager.Instance.ShowMinigameProgress(nextDotIndex, dots.Count);

                if (nextDotIndex >= dots.Count)
                    CompleteGame();
                else if (justConnected > 0)
                    DrawLine(justConnected - 1, justConnected);
            }
            else
            {
                dot.ShowError();
                UIManager.Instance.ShowMinigameError($"Wrong dot! Try dot #{nextDotIndex + 1}");
            }
            break;
        }
    }

    private void DrawLine(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex >= dots.Count) return;

        Vector2 from     = dots[fromIndex].GetWorldPosition();
        Vector2 to       = dots[toIndex].GetWorldPosition();
        Vector2 diff     = to - from;
        float   distance = diff.magnitude;
        float   angle    = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        var line = new VisualElement();
        line.name = $"Line_{fromIndex}_{toIndex}";
        line.style.position        = Position.Absolute;
        line.style.backgroundColor = Color.white;
        line.style.width           = distance;
        line.style.height          = 3f;
        line.style.left            = from.x;
        line.style.top             = from.y;
        line.style.transformOrigin = new TransformOrigin(
            new Length(0f,  LengthUnit.Pixel),
            new Length(50f, LengthUnit.Percent),
            0f
        );
        line.style.rotate = new Rotate(angle);
        line.pickingMode = PickingMode.Ignore;

        linesContainer.Add(line);
    }

    void CompleteGame()
    {
        isGameActive = false;
        UIManager.Instance.ShowMinigameCompletion("Picture complete!");
        gameCompletedEvent?.Invoke();
    }

    public void RegisterCompletionCallback(System.Action callback) => gameCompletedEvent += callback;

    /// <summary>
    /// Start the dot game: show UI, initialize, and set up completion callback
    /// </summary>
    public void StartGame(System.Action onComplete = null)
    {
        Debug.Log("Starting dot game...");
        
        // Show minigame UI and hide gameplay
        UIManager.Instance.ShowMinigame();
        
        // Initialize the game
        ResetGame();
        
        // Register completion callback
        if (onComplete != null)
            RegisterCompletionCallback(onComplete);
        
        Debug.Log("Dot game initialized and started");
    }

    public void ResetGame()
    {
        nextDotIndex = 0;
        foreach (var dot in dots) dot.Reset();
        linesContainer?.Clear();
        UIManager.Instance.ClearMinigameFeedback();
        isGameActive = true;
    }
}