using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsBackButton;

    private void Awake()
    {
        // Ensure Time.timeScale is reset when menu loads (in case coming from paused game)
        Time.timeScale = 1f;
        
        // Ensure EventSystem exists for UI interaction
        EnsureEventSystem();
        
        if (startButton == null)
            throw new UnassignedReferenceException($"{nameof(startButton)} is not assigned in {name}");
        if (optionsButton == null)
            throw new UnassignedReferenceException($"{nameof(optionsButton)} is not assigned in {name}");
        if (quitButton == null)
            throw new UnassignedReferenceException($"{nameof(quitButton)} is not assigned in {name}");
        if (optionsPanel == null)
            throw new UnassignedReferenceException($"{nameof(optionsPanel)} is not assigned in {name}");
        if (optionsBackButton == null)
            throw new UnassignedReferenceException($"{nameof(optionsBackButton)} is not assigned in {name}");

        // Ensure buttons are interactable
        startButton.interactable = true;
        optionsButton.interactable = true;
        quitButton.interactable = true;
        optionsBackButton.interactable = true;

        startButton.onClick.AddListener(OnStartClicked);
        optionsButton.onClick.AddListener(OnOptionsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        optionsBackButton.onClick.AddListener(OnOptionsBackClicked);

        optionsPanel.SetActive(false);
    }

    private void Start()
    {
        // Double-check Time.timeScale in Start as well
        Time.timeScale = 1f;
        
        EnsureBackgroundAnimator();
        MenuResizer.DisableIntroAnimations();
        MenuResizer.EnsureHoverAnimators();
        MenuResizer.ResizeAllButtons();
        MenuResizer.ResizeAllTexts();
        MenuResizer.ResizePanel("OptionsContent", new Vector2(750, 625));
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

    private void OnStartClicked()
    {
        SceneManager.LoadScene(SceneNames.TrackSelect);
    }

    private void OnOptionsClicked()
    {
        optionsPanel.SetActive(true);
    }

    private void OnOptionsBackClicked()
    {
        optionsPanel.SetActive(false);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

