using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitToMenuButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;
    private PauseMenuAnimator pauseAnimator;

    private void Awake()
    {
        if (pausePanel == null)
            throw new UnassignedReferenceException($"{nameof(pausePanel)} is not assigned in {name}");
        if (resumeButton == null)
            throw new UnassignedReferenceException($"{nameof(resumeButton)} is not assigned in {name}");
        if (exitToMenuButton == null)
            throw new UnassignedReferenceException($"{nameof(exitToMenuButton)} is not assigned in {name}");
        if (quitButton == null)
            throw new UnassignedReferenceException($"{nameof(quitButton)} is not assigned in {name}");

        pauseAnimator = pausePanel.GetComponent<PauseMenuAnimator>();
        if (pauseAnimator == null)
        {
            pauseAnimator = pausePanel.AddComponent<PauseMenuAnimator>();
        }

        resumeButton.onClick.AddListener(OnResumeClicked);
        exitToMenuButton.onClick.AddListener(OnExitToMenuClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        pausePanel.SetActive(false);
        isPaused = false;
    }
    
    private void Start()
    {
        RemoveSpeedLines();
        RemoveBrandingText();
        MenuResizer.EnsureHoverAnimators(pausePanel.transform);
        MenuResizer.ResizeAllButtons(pausePanel.transform);
        MenuResizer.ResizeAllTexts(pausePanel.transform);
        MenuResizer.ResizePanel(pausePanel.transform, "PauseContent", new Vector2(625, 562));
    }
    
    private void RemoveSpeedLines()
    {
        Transform[] children = pausePanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name.Contains("SpeedLine"))
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void RemoveBrandingText()
    {
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
        if (Input.GetKeyDown(KeyCode.Escape))
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
        // Reset time scale immediately - critical for UI to work in next scene
        Time.timeScale = 1f;
        
        // Ensure buttons are not blocking
        resumeButton.interactable = false;
        exitToMenuButton.interactable = false;
        quitButton.interactable = false;
        
        // Load scene immediately
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

