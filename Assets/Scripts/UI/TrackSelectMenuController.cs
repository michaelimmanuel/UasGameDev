using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TrackSelectMenuController : MonoBehaviour
{
    [SerializeField] private Button track1Button;
    [SerializeField] private Button track2Button;
    [SerializeField] private Button track3Button;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject trackSelectPanel;

    private void Awake()
    {
        if (track1Button == null)
            throw new UnassignedReferenceException($"{nameof(track1Button)} is not assigned in {name}");
        if (track2Button == null)
            throw new UnassignedReferenceException($"{nameof(track2Button)} is not assigned in {name}");
        if (track3Button == null)
            throw new UnassignedReferenceException($"{nameof(track3Button)} is not assigned in {name}");
        if (backButton == null)
            throw new UnassignedReferenceException($"{nameof(backButton)} is not assigned in {name}");

        track1Button.onClick.AddListener(OnTrack1Clicked);
        track2Button.onClick.AddListener(OnTrack2Clicked);
        track3Button.onClick.AddListener(OnTrack3Clicked);
        backButton.onClick.AddListener(OnBackClicked);
        
        MenuResizer.DisableIntroAnimations();
        MenuResizer.EnsureHoverAnimators();
    }
    
    private void Start()
    {
        MenuResizer.ResizeAllButtons();
        MenuResizer.ResizeAllTexts();
    }

    private void OnTrack1Clicked()
    {
        SceneManager.LoadScene(SceneNames.Track1);
    }

    private void OnTrack2Clicked()
    {
        SceneManager.LoadScene(SceneNames.Track2);
    }

    private void OnTrack3Clicked()
    {
        SceneManager.LoadScene(SceneNames.Track3);
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(SceneNames.StartMenu);
    }
}

