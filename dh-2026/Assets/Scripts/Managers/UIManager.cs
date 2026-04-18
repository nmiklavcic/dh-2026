using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;

    // Panels
    private VisualElement _gameplayPanel;
    private VisualElement _feelAroundPanel;
    private VisualElement _memoryPanel;
    private VisualElement _interactionPanel;

    // Gameplay elements
    public Label SituationText { get; private set; }
    public VisualElement OptionsContainerR1 { get; private set; }
    public VisualElement OptionsContainerR2 { get; private set; }

    // Interaction elements
    public Label InteractionText { get; private set; }
    public VisualElement InteractionOptions { get; private set; }

    // Minigame feedback
    private Label _minigameFeedbackText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var root = uiDocument.rootVisualElement;
        Debug.Log("UIManager Awake - root is: " + (root == null ? "NULL" : "OK"));

        _gameplayPanel    = root.Q("GameplayPanel");
        _feelAroundPanel  = root.Q("FeelAroundPanel");
        _memoryPanel      = root.Q("MemoryPanel");
        _interactionPanel = root.Q("InteractionPanel");

        Debug.Log("GameplayPanel: " + (_gameplayPanel == null ? "NULL" : "OK"));
        Debug.Log("FeelAroundPanel: " + (_feelAroundPanel == null ? "NULL" : "OK"));

        SituationText      = root.Q<Label>("SituationText");
        OptionsContainerR1 = root.Q("OptionsContainerR1");
        OptionsContainerR2 = root.Q("OptionsContainerR2");
        InteractionText    = root.Q<Label>("InteractionText");
        InteractionOptions = root.Q("InteractionOptions");
        _minigameFeedbackText = root.Q<Label>("MinigameFeedbackText");

        Debug.Log("SituationText: " + (SituationText == null ? "NULL" : "OK"));
        Debug.Log("OptionsContainerR1: " + (OptionsContainerR1 == null ? "NULL" : "OK"));
        Debug.Log("OptionsContainerR2: " + (OptionsContainerR2 == null ? "NULL" : "OK"));
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.mKey.wasPressedThisFrame)
            ToggleMemory();

        if (keyboard.qKey.wasPressedThisFrame)
            CloseInteraction();
    }

    // ── Panel switching ──────────────────────────────────────

    public void ShowGameplay()
    {
        _gameplayPanel.RemoveFromClassList("hidden");
        _feelAroundPanel.AddToClassList("hidden");
        _memoryPanel.AddToClassList("hidden");
        _interactionPanel.AddToClassList("hidden");
    }

    public void ShowFeelAround()
    {
        _feelAroundPanel.RemoveFromClassList("hidden");
        _gameplayPanel.AddToClassList("hidden");
        _memoryPanel.AddToClassList("hidden");
        _interactionPanel.AddToClassList("hidden");
    }

    public void ShowInteraction()
    {
        _interactionPanel.RemoveFromClassList("hidden");
        _gameplayPanel.AddToClassList("hidden");
        _feelAroundPanel.AddToClassList("hidden");
        _memoryPanel.AddToClassList("hidden");
    }

    public void ToggleMemory()
    {
        bool isHidden = _memoryPanel.ClassListContains("hidden");
        if (isHidden)
            _memoryPanel.RemoveFromClassList("hidden");
        else
            _memoryPanel.AddToClassList("hidden");
    }

    public void CloseInteraction()
    {
        if (!_interactionPanel.ClassListContains("hidden"))
            ShowGameplay();
    }

    public void ShowMinigame()
    {
        ShowFeelAround();
    }

    // ── Minigame feedback ───────────────────────────────────

    /// <summary>
    /// Show progress message (e.g., "Connected 3 of 5 dots")
    /// </summary>
    public void ShowMinigameProgress(int connected, int total)
    {
        if (_minigameFeedbackText != null)
            _minigameFeedbackText.text = $"Connected {connected} of {total}";
    }

    /// <summary>
    /// Show error message when wrong dot is clicked
    /// </summary>
    public void ShowMinigameError(string message = "Wrong dot! Try again.")
    {
        if (_minigameFeedbackText != null)
            _minigameFeedbackText.text = message;
    }

    /// <summary>
    /// Show completion message
    /// </summary>
    public void ShowMinigameCompletion(string message = "Picture complete!")
    {
        if (_minigameFeedbackText != null)
            _minigameFeedbackText.text = message;
    }

    /// <summary>
    /// Clear the minigame feedback text
    /// </summary>
    public void ClearMinigameFeedback()
    {
        if (_minigameFeedbackText != null)
            _minigameFeedbackText.text = "";
    }

    // ── Getters ──────────────────────────────────────────────

    public VisualElement GetGameplayPanel() => _gameplayPanel;
    public VisualElement GetFeelAroundPanel() => _feelAroundPanel;
    public VisualElement GetMemoryPanel() => _memoryPanel;
    public VisualElement GetInteractionPanel() => _interactionPanel;
}
