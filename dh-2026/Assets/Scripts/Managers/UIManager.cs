using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;

    private const string HiddenClass = "hidden";
    private const string StartSituationId = "start";
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MusicVolumeKey = "settings.musicVolume";
    private const string SfxVolumeKey = "settings.sfxVolume";
    private const string FullscreenKey = "settings.fullscreen";

    private enum ActivePanel
    {
        Gameplay,
        FeelAround,
        Memory,
        Interaction,
        Puzzle
    }

    private enum SettingsOrigin
    {
        MainMenu,
        PauseMenu
    }

    // Panels
    private VisualElement _gameplayPanel;
    private VisualElement _feelAroundPanel;
    private VisualElement _memoryPanel;
    private VisualElement _interactionPanel;
    private VisualElement _puzzlePanel;
    private VisualElement _mainMenuPanel;
    private VisualElement _settingsMenuPanel;
    private VisualElement _pauseMenuPanel;

    // Menu controls
    private Button _startGameButton;
    private Button _mainMenuSettingsButton;
    private Button _quitGameButton;
    private Button _settingsBackButton;
    private Button _pauseResumeButton;
    private Button _pauseSettingsButton;
    private Button _pauseMainMenuButton;
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Toggle _fullscreenToggle;

    // Gameplay elements
    public Label SituationText { get; private set; }
    public VisualElement OptionsContainerR1 { get; private set; }
    public VisualElement OptionsContainerR2 { get; private set; }

    // Interaction elements
    public Label InteractionText { get; private set; }
    public VisualElement InteractionOptions { get; private set; }

    // Minigame feedback
    private Label _minigameFeedbackText;

    private ActivePanel _currentActivePanel = ActivePanel.Gameplay;
    private ActivePanel _panelBeforePause = ActivePanel.Gameplay;
    private SettingsOrigin _settingsOrigin = SettingsOrigin.MainMenu;
    private bool _gameStarted;
    private bool _isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (uiDocument == null)
        {
            Debug.LogError("UIManager requires a UIDocument reference.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("UIManager could not access the rootVisualElement.");
            return;
        }

        CacheElements(root);
        BindMenuEvents();
        LoadSettings();
    }

    private void Start()
    {
        Time.timeScale = 0f;
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        UnbindMenuEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            HandleEscapePressed();
            return;
        }

        if (!_gameStarted || IsAnyMenuVisible() || _isPaused)
        {
            return;
        }

        if (keyboard.mKey.wasPressedThisFrame)
        {
            ToggleMemory();
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            CloseInteraction();
        }
    }

    private void CacheElements(VisualElement root)
    {
        _gameplayPanel = root.Q("GameplayPanel");
        _feelAroundPanel = root.Q("FeelAroundPanel");
        _memoryPanel = root.Q("MemoryPanel");
        _interactionPanel = root.Q("InteractionPanel");
        _puzzlePanel = root.Q("PuzzlePanel");
        _mainMenuPanel = root.Q("MainMenuPanel");
        _settingsMenuPanel = root.Q("SettingsMenuPanel");
        _pauseMenuPanel = root.Q("PauseMenuPanel");

        SituationText = root.Q<Label>("SituationText");
        OptionsContainerR1 = root.Q("OptionsContainerR1");
        OptionsContainerR2 = root.Q("OptionsContainerR2");
        InteractionText = root.Q<Label>("InteractionText");
        InteractionOptions = root.Q("InteractionOptions");
        _minigameFeedbackText = root.Q<Label>("MinigameFeedbackText");

        _startGameButton = root.Q<Button>("StartGameButton");
        _mainMenuSettingsButton = root.Q<Button>("MainMenuSettingsButton");
        _quitGameButton = root.Q<Button>("QuitGameButton");
        _settingsBackButton = root.Q<Button>("SettingsBackButton");
        _pauseResumeButton = root.Q<Button>("PauseResumeButton");
        _pauseSettingsButton = root.Q<Button>("PauseSettingsButton");
        _pauseMainMenuButton = root.Q<Button>("PauseMainMenuButton");

        _masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
        _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        _sfxVolumeSlider = root.Q<Slider>("SfxVolumeSlider");
        _fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
    }

    private void BindMenuEvents()
    {
        if (_startGameButton != null)
        {
            _startGameButton.clicked += OnStartGameButtonClicked;
        }

        if (_mainMenuSettingsButton != null)
        {
            _mainMenuSettingsButton.clicked += OnMainMenuSettingsClicked;
        }

        if (_quitGameButton != null)
        {
            _quitGameButton.clicked += OnQuitGameClicked;
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.clicked += OnSettingsBackClicked;
        }

        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.clicked += OnPauseResumeClicked;
        }

        if (_pauseSettingsButton != null)
        {
            _pauseSettingsButton.clicked += OnPauseSettingsClicked;
        }

        if (_pauseMainMenuButton != null)
        {
            _pauseMainMenuButton.clicked += OnPauseMainMenuClicked;
        }

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        }
    }

    private void UnbindMenuEvents()
    {
        if (_startGameButton != null)
        {
            _startGameButton.clicked -= OnStartGameButtonClicked;
        }

        if (_mainMenuSettingsButton != null)
        {
            _mainMenuSettingsButton.clicked -= OnMainMenuSettingsClicked;
        }

        if (_quitGameButton != null)
        {
            _quitGameButton.clicked -= OnQuitGameClicked;
        }

        if (_settingsBackButton != null)
        {
            _settingsBackButton.clicked -= OnSettingsBackClicked;
        }

        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.clicked -= OnPauseResumeClicked;
        }

        if (_pauseSettingsButton != null)
        {
            _pauseSettingsButton.clicked -= OnPauseSettingsClicked;
        }

        if (_pauseMainMenuButton != null)
        {
            _pauseMainMenuButton.clicked -= OnPauseMainMenuClicked;
        }

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.UnregisterValueChangedCallback(OnMasterVolumeChanged);
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.UnregisterValueChangedCallback(OnMusicVolumeChanged);
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.UnregisterValueChangedCallback(OnSfxVolumeChanged);
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenChanged);
        }
    }

    private void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.85f);
        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.SetValueWithoutNotify(musicVolume);
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.SetValueWithoutNotify(fullscreen);
        }

        AudioListener.volume = masterVolume;
        Screen.fullScreen = fullscreen;
    }

    private void HandleEscapePressed()
    {
        if (IsPanelVisible(_mainMenuPanel))
        {
            return;
        }

        if (IsPanelVisible(_settingsMenuPanel))
        {
            if (_settingsOrigin == SettingsOrigin.PauseMenu)
            {
                ShowPauseMenu();
            }
            else
            {
                ShowMainMenu();
            }

            return;
        }

        if (IsPanelVisible(_pauseMenuPanel))
        {
            ResumeFromPause();
            return;
        }

        if (_gameStarted)
        {
            PauseGame();
        }
    }

    private void OnStartGameButtonClicked()
    {
        Time.timeScale = 1f;
        _isPaused = false;

        if (!_gameStarted)
        {
            _gameStarted = true;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.LoadSituation(StartSituationId);
            }
            else
            {
                ShowGameplay();
            }

            return;
        }

        ResumeTrackedPanel();
    }

    private void OnMainMenuSettingsClicked()
    {
        _settingsOrigin = SettingsOrigin.MainMenu;
        ShowSettingsMenu();
    }

    private void OnPauseSettingsClicked()
    {
        _settingsOrigin = SettingsOrigin.PauseMenu;
        ShowSettingsMenu();
    }

    private void OnSettingsBackClicked()
    {
        if (_settingsOrigin == SettingsOrigin.PauseMenu)
        {
            ShowPauseMenu();
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void OnPauseResumeClicked()
    {
        ResumeFromPause();
    }

    private void OnPauseMainMenuClicked()
    {
        _isPaused = false;
        Time.timeScale = 0f;
        ShowMainMenu();
    }

    private static void OnQuitGameClicked()
    {
        Application.Quit();
    }

    private void PauseGame()
    {
        _panelBeforePause = DetermineActivePanel();
        _isPaused = true;
        Time.timeScale = 0f;
        ShowPauseMenu();
    }

    private void ResumeFromPause()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        ResumeTrackedPanel();
    }

    private void ResumeTrackedPanel()
    {
        switch (_panelBeforePause)
        {
            case ActivePanel.FeelAround:
                ShowFeelAround();
                break;
            case ActivePanel.Memory:
                ShowGameplay();
                _memoryPanel.RemoveFromClassList(HiddenClass);
                _currentActivePanel = ActivePanel.Memory;
                break;
            case ActivePanel.Interaction:
                ShowInteraction();
                break;
            case ActivePanel.Puzzle:
                ShowPuzzle();
                break;
            default:
                ShowGameplay();
                break;
        }
    }

    private ActivePanel DetermineActivePanel()
    {
        if (IsPanelVisible(_pauseMenuPanel) || IsPanelVisible(_settingsMenuPanel) || IsPanelVisible(_mainMenuPanel))
        {
            return _panelBeforePause;
        }

        if (IsPanelVisible(_puzzlePanel))
        {
            return ActivePanel.Puzzle;
        }

        if (IsPanelVisible(_interactionPanel))
        {
            return ActivePanel.Interaction;
        }

        if (IsPanelVisible(_feelAroundPanel))
        {
            return ActivePanel.FeelAround;
        }

        if (IsPanelVisible(_memoryPanel))
        {
            return ActivePanel.Memory;
        }

        return ActivePanel.Gameplay;
    }

    private bool IsAnyMenuVisible()
    {
        return IsPanelVisible(_mainMenuPanel) || IsPanelVisible(_settingsMenuPanel) || IsPanelVisible(_pauseMenuPanel);
    }

    private static bool IsPanelVisible(VisualElement panel)
    {
        return panel != null && !panel.ClassListContains(HiddenClass);
    }

    private void HideGameplayPanels()
    {
        _gameplayPanel.AddToClassList(HiddenClass);
        _feelAroundPanel.AddToClassList(HiddenClass);
        _memoryPanel.AddToClassList(HiddenClass);
        _interactionPanel.AddToClassList(HiddenClass);
        _puzzlePanel.AddToClassList(HiddenClass);
    }

    public void ShowGameplay()
    {
        _currentActivePanel = ActivePanel.Gameplay;
        HideGameplayPanels();
        HideMenuPanels();
        _gameplayPanel.RemoveFromClassList(HiddenClass);
    }

    public void ShowFeelAround()
    {
        _currentActivePanel = ActivePanel.FeelAround;
        HideGameplayPanels();
        HideMenuPanels();
        _feelAroundPanel.RemoveFromClassList(HiddenClass);
    }

    public void ShowInteraction()
    {
        _currentActivePanel = ActivePanel.Interaction;
        HideGameplayPanels();
        HideMenuPanels();
        _interactionPanel.RemoveFromClassList(HiddenClass);
    }

    public void ShowPuzzle()
    {
        _currentActivePanel = ActivePanel.Puzzle;
        HideGameplayPanels();
        HideMenuPanels();
        _puzzlePanel.RemoveFromClassList(HiddenClass);
    }

    public void ShowMainMenu()
    {
        HideGameplayPanels();
        _mainMenuPanel.RemoveFromClassList(HiddenClass);
        _settingsMenuPanel.AddToClassList(HiddenClass);
        _pauseMenuPanel.AddToClassList(HiddenClass);
    }

    public void ShowSettingsMenu()
    {
        HideGameplayPanels();
        _mainMenuPanel.AddToClassList(HiddenClass);
        _settingsMenuPanel.RemoveFromClassList(HiddenClass);
        _pauseMenuPanel.AddToClassList(HiddenClass);
    }

    public void ShowPauseMenu()
    {
        HideGameplayPanels();
        _mainMenuPanel.AddToClassList(HiddenClass);
        _settingsMenuPanel.AddToClassList(HiddenClass);
        _pauseMenuPanel.RemoveFromClassList(HiddenClass);
    }

    public void HideMenuPanels()
    {
        _mainMenuPanel.AddToClassList(HiddenClass);
        _settingsMenuPanel.AddToClassList(HiddenClass);
        _pauseMenuPanel.AddToClassList(HiddenClass);
    }

    public void ToggleMemory()
    {
        if (!_gameStarted || IsAnyMenuVisible())
        {
            return;
        }

        bool isHidden = _memoryPanel.ClassListContains(HiddenClass);
        if (isHidden)
        {
            ShowGameplay();
            _memoryPanel.RemoveFromClassList(HiddenClass);
            _currentActivePanel = ActivePanel.Memory;
        }
        else
        {
            _memoryPanel.AddToClassList(HiddenClass);
            _currentActivePanel = ActivePanel.Gameplay;
        }
    }

    public void CloseInteraction()
    {
        if (!_interactionPanel.ClassListContains(HiddenClass))
        {
            ShowGameplay();
        }
    }

    public void ShowMinigame()
    {
        ShowFeelAround();
    }

    public void ShowMinigameProgress(int connected, int total)
    {
        if (_minigameFeedbackText != null)
        {
            _minigameFeedbackText.text = $"Connected {connected} of {total}";
        }
    }

    public void ShowMinigameError(string message = "Wrong dot! Try again.")
    {
        if (_minigameFeedbackText != null)
        {
            _minigameFeedbackText.text = message;
        }
    }

    public void ShowMinigameCompletion(string message = "Picture complete!")
    {
        if (_minigameFeedbackText != null)
        {
            _minigameFeedbackText.text = message;
        }
    }

    public void ClearMinigameFeedback()
    {
        if (_minigameFeedbackText != null)
        {
            _minigameFeedbackText.text = "";
        }
    }

    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, evt.newValue);
        AudioListener.volume = evt.newValue;
    }

    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, evt.newValue);
    }

    private void OnSfxVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, evt.newValue);
    }

    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        PlayerPrefs.SetInt(FullscreenKey, evt.newValue ? 1 : 0);
        Screen.fullScreen = evt.newValue;
    }

    public VisualElement GetGameplayPanel() => _gameplayPanel;
    public VisualElement GetFeelAroundPanel() => _feelAroundPanel;
    public VisualElement GetPuzzlePanel() => _puzzlePanel;
    public VisualElement GetMemoryPanel() => _memoryPanel;
    public VisualElement GetInteractionPanel() => _interactionPanel;
    public VisualElement GetMainMenuPanel() => _mainMenuPanel;
    public VisualElement GetSettingsMenuPanel() => _settingsMenuPanel;
    public VisualElement GetPauseMenuPanel() => _pauseMenuPanel;
}
