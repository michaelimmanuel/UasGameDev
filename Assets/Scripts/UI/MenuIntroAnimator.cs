using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuIntroAnimator : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Intro")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.22f;
    [SerializeField, Min(0f)] private float slideDuration = 0.22f;
    [SerializeField] private Vector2 slideOffset = new Vector2(0f, -40f);
    [SerializeField, Min(0f)] private float startScale = 0.96f;

    [Header("Button Pop")]
    [SerializeField, Min(0f)] private float popStagger = 0.06f;
    [SerializeField, Min(0f)] private float popDuration = 0.12f;
    [SerializeField, Min(1f)] private float popScale = 1.06f;
    [SerializeField, Min(0f)] private float prePopScale = 0.96f;
    [SerializeField] private bool includeNonInteractableButtons = false;

    private CanvasGroup canvasGroup;
    private RectTransform panel;

    private Vector2 baseAnchoredPosition;
    private Vector3 basePanelScale;

    private RectTransform[] popTargets;
    private Vector3[] basePopScales;

    private Coroutine playCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            throw new MissingComponentException($"{nameof(MenuIntroAnimator)} requires a {nameof(CanvasGroup)} on '{name}'.");

        panel = GetComponent<RectTransform>();
        if (panel == null)
            throw new MissingComponentException($"{nameof(MenuIntroAnimator)} requires a {nameof(RectTransform)} on '{name}'.");

        baseAnchoredPosition = panel.anchoredPosition;
        basePanelScale = panel.localScale;

        CachePopTargets();
    }

    private void OnEnable()
    {
        if (!playOnEnable)
            return;

        Play();
    }

    private void OnDisable()
    {
        StopCurrent();
        RestoreFinalState();
    }

    public void Play()
    {
        StopCurrent();
        CachePopTargets();
        ApplyHiddenState();
        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private void CachePopTargets()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        int targetCount = 0;

        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (!includeNonInteractableButtons && !button.interactable)
                continue;

            targetCount++;
        }

        if (targetCount == 0)
            throw new MissingReferenceException($"{nameof(MenuIntroAnimator)} on '{name}' couldn't find any UI Buttons to animate.");

        popTargets = new RectTransform[targetCount];
        basePopScales = new Vector3[targetCount];

        int targetIndex = 0;
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (!includeNonInteractableButtons && !button.interactable)
                continue;

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
                continue;

            popTargets[targetIndex] = rectTransform;
            basePopScales[targetIndex] = rectTransform.localScale;
            targetIndex++;
        }

        if (targetIndex != targetCount)
            throw new System.InvalidOperationException($"{nameof(MenuIntroAnimator)} on '{name}' found {targetCount} buttons but only cached {targetIndex} RectTransforms.");
    }

    private void StopCurrent()
    {
        if (playCoroutine == null)
            return;

        StopCoroutine(playCoroutine);
        playCoroutine = null;
    }

    private void ApplyHiddenState()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panel.anchoredPosition = baseAnchoredPosition + slideOffset;
        panel.localScale = basePanelScale * startScale;

        for (int index = 0; index < popTargets.Length; index++)
        {
            popTargets[index].localScale = basePopScales[index] * prePopScale;
        }
    }

    private void RestoreFinalState()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        panel.anchoredPosition = baseAnchoredPosition;
        panel.localScale = basePanelScale;

        if (popTargets == null || basePopScales == null)
            return;

        for (int index = 0; index < popTargets.Length; index++)
        {
            if (popTargets[index] == null)
                continue;

            popTargets[index].localScale = basePopScales[index];
        }
    }

    private IEnumerator PlayRoutine()
    {
        float introDuration = Mathf.Max(fadeDuration, slideDuration);
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float fadeT = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            float slideT = slideDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / slideDuration);

            float fadeEased = EaseOutCubic(fadeT);
            float slideEased = EaseOutCubic(slideT);

            canvasGroup.alpha = fadeEased;
            panel.anchoredPosition = Vector2.LerpUnclamped(baseAnchoredPosition + slideOffset, baseAnchoredPosition, slideEased);
            panel.localScale = Vector3.LerpUnclamped(basePanelScale * startScale, basePanelScale, slideEased);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        panel.anchoredPosition = baseAnchoredPosition;
        panel.localScale = basePanelScale;

        float popElapsed = 0f;
        int lastPoppedIndex = -1;

        while (lastPoppedIndex < popTargets.Length - 1)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            popElapsed += deltaTime;

            float nextIndexFloat = popStagger <= 0f ? popTargets.Length : popElapsed / popStagger;
            int nextIndex = Mathf.Min(popTargets.Length - 1, Mathf.FloorToInt(nextIndexFloat));

            for (int index = lastPoppedIndex + 1; index <= nextIndex; index++)
            {
                StartCoroutine(PopRoutine(index));
                lastPoppedIndex = index;
            }

            yield return null;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        playCoroutine = null;
    }

    private IEnumerator PopRoutine(int index)
    {
        RectTransform target = popTargets[index];
        Vector3 baseScale = basePopScales[index];

        if (target == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float t = popDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / popDuration);

            Vector3 from = baseScale * prePopScale;
            Vector3 to = baseScale;
            Vector3 overshoot = baseScale * popScale;

            if (t < 0.6f)
            {
                float t1 = Mathf.Clamp01(t / 0.6f);
                target.localScale = Vector3.LerpUnclamped(from, overshoot, EaseOutCubic(t1));
            }
            else
            {
                float t2 = Mathf.Clamp01((t - 0.6f) / 0.4f);
                target.localScale = Vector3.LerpUnclamped(overshoot, to, EaseOutCubic(t2));
            }

            yield return null;
        }

        target.localScale = baseScale;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - (inverse * inverse * inverse);
    }
}
