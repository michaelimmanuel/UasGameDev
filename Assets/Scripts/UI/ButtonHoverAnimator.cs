using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ButtonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float animationDuration = 0.15f;
    
    private RectTransform rectTransform;
    private Button button;
    private Vector3 baseScale;
    private bool isHovered;
    private bool isPressed;
    
    private Coroutine scaleCoroutine;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        baseScale = rectTransform.localScale;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable)
            return;
            
        isHovered = true;
        if (!isPressed)
        {
            AnimateScale(baseScale * hoverScale);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            AnimateScale(baseScale);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable)
            return;
            
        isPressed = true;
        AnimateScale(baseScale * pressScale);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        AnimateScale(isHovered ? baseScale * hoverScale : baseScale);
    }
    
    private void AnimateScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
    }
    
    private System.Collections.IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float eased = EaseOutCubic(t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            yield return null;
        }
        
        rectTransform.localScale = targetScale;
        scaleCoroutine = null;
    }
    
    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }
    
    private void OnDisable()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        rectTransform.localScale = baseScale;
        isHovered = false;
        isPressed = false;
    }
}
