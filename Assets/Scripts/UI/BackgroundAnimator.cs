using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackgroundAnimator : MonoBehaviour
{
    [Header("Speed Lines")]
    [SerializeField] private float speedLineSpeed = 50f;
    [SerializeField] private float speedLineResetPosition = -200f;
    [SerializeField] private float speedLineSpawnPosition = 1200f;
    
    [Header("Pulse Effect")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseDuration = 3f;
    [SerializeField] private float pulseIntensity = 0.1f;
    
    private RectTransform[] speedLines;
    private Image backgroundImage;
    private Color baseColor;
    
    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            baseColor = backgroundImage.color;
        }
        
        CacheSpeedLines();
    }
    
    private void Start()
    {
        if (enablePulse && backgroundImage != null)
        {
            StartCoroutine(PulseCoroutine());
        }
        
        StartCoroutine(AnimateSpeedLines());
    }
    
    private void CacheSpeedLines()
    {
        speedLines = GetComponentsInChildren<RectTransform>();
        int count = 0;
        foreach (RectTransform rt in speedLines)
        {
            if (rt.name.Contains("SpeedLine"))
            {
                count++;
            }
        }
        
        if (count == 0)
        {
            speedLines = new RectTransform[0];
            return;
        }
        
        RectTransform[] temp = new RectTransform[count];
        int index = 0;
        foreach (RectTransform rt in speedLines)
        {
            if (rt.name.Contains("SpeedLine"))
            {
                temp[index++] = rt;
            }
        }
        speedLines = temp;
    }
    
    private IEnumerator AnimateSpeedLines()
    {
        if (speedLines == null || speedLines.Length == 0)
            yield break;
            
        float[] offsets = new float[speedLines.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = Random.Range(0f, 500f);
        }
        
        while (true)
        {
            for (int i = 0; i < speedLines.Length; i++)
            {
                if (speedLines[i] == null)
                    continue;
                    
                RectTransform line = speedLines[i];
                Vector2 pos = line.anchoredPosition;
                pos.y -= speedLineSpeed * Time.unscaledDeltaTime;
                
                if (pos.y < speedLineResetPosition)
                {
                    pos.y = speedLineSpawnPosition + offsets[i];
                }
                
                line.anchoredPosition = pos;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator PulseCoroutine()
    {
        while (true)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Sin((elapsed / pulseDuration) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float alpha = baseColor.a + (t * pulseIntensity - pulseIntensity * 0.5f);
                backgroundImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
                yield return null;
            }
        }
    }
}
