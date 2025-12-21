using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class PauseMenuAnimator : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 baseScale;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        rectTransform.localScale = baseScale;
    }
    
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
