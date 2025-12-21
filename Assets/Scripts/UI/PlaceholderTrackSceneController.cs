using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PlaceholderTrackSceneController : MonoBehaviour
{
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (backButton == null)
            throw new UnassignedReferenceException($"{nameof(backButton)} is not assigned in {name}");

        EnsureEventSystem();
        Time.timeScale = 1f;

        backButton.interactable = true;
        backButton.onClick.AddListener(OnBackClicked);

        MenuResizer.EnsureHoverAnimators();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void OnBackClicked()
    {
        SceneManager.LoadScene(SceneNames.StartMenu);
    }
}
