using UnityEngine;
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
    public VisualElement OptionsContainer { get; private set; }

    // Interaction elements
    public Label InteractionText { get; private set; }
    public VisualElement InteractionOptions { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var root = uiDocument.rootVisualElement;

        _gameplayPanel    = root.Q("GameplayPanel");
        _feelAroundPanel  = root.Q("FeelAroundPanel");
        _memoryPanel      = root.Q("MemoryPanel");
        _interactionPanel = root.Q("InteractionPanel");

        SituationText    = root.Q<Label>("SituationText");
        OptionsContainer = root.Q("OptionsContainer");
        InteractionText  = root.Q<Label>("InteractionText");
        InteractionOptions = root.Q("InteractionOptions");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMemory();

        if (Input.GetKeyDown(KeyCode.Q))
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
}
