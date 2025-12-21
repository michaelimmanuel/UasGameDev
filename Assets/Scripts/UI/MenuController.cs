using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuController : MonoBehaviour
{
    [Header("Start Menu")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsBackButton;
    
    [Header("Track Select Menu")]
    [SerializeField] private Button track1Button;
    [SerializeField] private Button track2Button;
    [SerializeField] private Button track3Button;
    [SerializeField] private Button backButton;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitToMenuButton;
    [SerializeField] private Button pauseQuitButton;
    
    [Header("Placeholder Track")]
    [SerializeField] private Button placeholderBackButton;
    
    private bool isPaused;
    private PauseMenuAnimator pauseAnimator;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        SetupButtons();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        
        if (pausePanel != null)
        {
            SetupPauseMenu();
        }
        
        if (startButton != null || track1Button != null)
        {
            SetupMenuScene();
        }
        
        if (placeholderBackButton != null)
        {
            SetupPlaceholderScene();
        }
    }
    
    private void SetupButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.TrackSelect));
            startButton.interactable = true;
        }
        
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(() => optionsPanel?.SetActive(true));
            optionsButton.interactable = true;
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            quitButton.interactable = true;
        }
        
        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.AddListener(() => optionsPanel?.SetActive(false));
            optionsBackButton.interactable = true;
        }
        
        if (track1Button != null)
        {
            track1Button.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Track1));
        }
        
        if (track2Button != null)
        {
            track2Button.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Track2));
        }
        
        if (track3Button != null)
        {
            track3Button.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Track3));
        }
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.StartMenu));
        }
        
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.AddListener(OnExitToMenuClicked);
        }
        
        if (pauseQuitButton != null)
        {
            pauseQuitButton.onClick.AddListener(OnQuitClicked);
        }
        
        if (placeholderBackButton != null)
        {
            placeholderBackButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.StartMenu));
        }
    }
    
    private void SetupMenuScene()
    {
        EnsureBackgroundAnimator();
        MenuResizer.DisableIntroAnimations();
        MenuResizer.EnsureHoverAnimators();
        MenuResizer.ResizeAllButtons();
        MenuResizer.ResizeAllTexts();
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
            MenuResizer.ResizePanel("OptionsContent", new Vector2(750, 625));
        }
    }
    
    private void SetupPauseMenu()
    {
        pauseAnimator = pausePanel.GetComponent<PauseMenuAnimator>();
        if (pauseAnimator == null)
        {
            pauseAnimator = pausePanel.AddComponent<PauseMenuAnimator>();
        }
        
        pausePanel.SetActive(false);
        isPaused = false;
        
        RemoveSpeedLines();
        RemoveBrandingText();
        MenuResizer.EnsureHoverAnimators(pausePanel.transform);
        MenuResizer.ResizeAllButtons(pausePanel.transform);
        MenuResizer.ResizeAllTexts(pausePanel.transform);
        MenuResizer.ResizePanel(pausePanel.transform, "PauseContent", new Vector2(625, 562));
    }
    
    private void SetupPlaceholderScene()
    {
        MenuResizer.EnsureHoverAnimators();
    }
    
    private void EnsureBackgroundAnimator()
    {
        GameObject background = GameObject.Find("Background");
        if (background != null)
        {
            BackgroundAnimator animator = background.GetComponent<BackgroundAnimator>();
            if (animator == null)
            {
                background.AddComponent<BackgroundAnimator>();
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
    
    private void RemoveSpeedLines()
    {
        if (pausePanel == null) return;
        
        Transform[] children = pausePanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name.Contains("SpeedLine"))
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
    
    private void RemoveBrandingText()
    {
        if (pausePanel == null) return;
        
        TMPro.TextMeshProUGUI[] texts = pausePanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if (text.text.Contains("FULL THROTTLE"))
            {
                Object.Destroy(text.gameObject);
            }
        }
    }

    private void Update()
    {
        if (pausePanel != null && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            pausePanel.SetActive(true);
            pauseAnimator.Show();
        }
        else
        {
            pauseAnimator.Hide();
        }
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void OnResumeClicked()
    {
        isPaused = false;
        pauseAnimator.Hide();
        Time.timeScale = 1f;
    }

    private void OnExitToMenuClicked()
    {
        Time.timeScale = 1f;
        
        if (resumeButton != null) resumeButton.interactable = false;
        if (exitToMenuButton != null) exitToMenuButton.interactable = false;
        if (pauseQuitButton != null) pauseQuitButton.interactable = false;
        
        SceneManager.LoadScene(SceneNames.StartMenu);
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
