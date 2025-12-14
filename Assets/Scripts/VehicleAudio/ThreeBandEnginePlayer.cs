using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class ThreeBandEnginePlayer : MonoBehaviour
{
    [Header("Audio Sources (assign looped clips)")]
    public AudioSource idleSource;
    public AudioSource midSource;
    public AudioSource highSource;
    public AudioSource veryHighSource; // optional extra high band (e.g. 5000 RPM)

    [Header("Optional Mixer")]
    public AudioMixer audioMixer;
    public string mixerLPFParam = "Engine_LPF_Cutoff";

    [Header("RPM Bands")]
    public float idleRPM = 800f;
    public float midRPM = 2200f;
    public float highRPM = 4000f;
    public float veryHighRPM = 5000f;
    public float maxRPM = 7000f;
    public float bandBlend = 400f;

    [Header("Pitch / Smoothing")]
    public float pitchAtIdle = 0.95f;
    public float pitchAtMax = 1.25f;
    public float pitchSmoothTime = 0.08f;
    public float volSmoothTime = 0.06f;

    // telemetry
    float engineRPM;
    float throttle;
    float load;
    int gear;
    float speed;

    // internal smoothers
    float smoothedRPM = 0f;
    float smoothedPitch = 1f;
    float smoothedIdleVol, smoothedMidVol, smoothedHighVol;
    float smoothedVeryHighVol;

    void Awake()
    {
        if (idleSource != null) idleSource.loop = true;
        if (midSource != null) midSource.loop = true;
        if (highSource != null) highSource.loop = true;
        if (veryHighSource != null) veryHighSource.loop = true;
    }

    void Start()
    {
        smoothedRPM = engineRPM;
        smoothedPitch = Mathf.Lerp(pitchAtIdle, pitchAtMax, 0f);
    }

    public void SetTelemetry(float rpm, float thr, float ld, int g, float spd)
    {
        engineRPM = rpm;
        throttle = thr;
        load = ld;
        gear = g;
        speed = spd;
    }

    void Update()
    {
        // smooth RPM
        float rpmTarget = engineRPM;
        float alphaR = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, 0.04f));
        smoothedRPM = Mathf.Lerp(smoothedRPM, rpmTarget, alphaR);

        // normalized progression for pitch
        float n = Mathf.Clamp01((smoothedRPM - idleRPM) / Mathf.Max(1f, (maxRPM - idleRPM)));
        float targetPitch = Mathf.Lerp(pitchAtIdle, pitchAtMax, Mathf.Pow(n, 0.9f));
        float alphaP = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, pitchSmoothTime));
        smoothedPitch = Mathf.Lerp(smoothedPitch, targetPitch, alphaP);

        // compute band volumes using chained crossfades for idle↔mid↔high↔veryHigh
        float idleV = 0f, midV = 0f, highV = 0f, veryHighV = 0f;
        float halfBlend = bandBlend * 0.5f;

        float midStart = midRPM - halfBlend;
        float midEnd = midRPM + halfBlend;
        float highStart = highRPM - halfBlend;
        float highEnd = highRPM + halfBlend;
        float veryHighStart = veryHighRPM - halfBlend;
        float veryHighEnd = veryHighRPM + halfBlend;

        if (smoothedRPM <= midStart)
        {
            // fully idle
            idleV = 1f;
        }
        else if (smoothedRPM < midEnd)
        {
            // idle -> mid
            float t = Mathf.InverseLerp(midStart, midEnd, smoothedRPM);
            idleV = 1f - t; midV = t;
        }
        else if (smoothedRPM < highStart)
        {
            // fully mid
            midV = 1f;
        }
        else if (smoothedRPM < highEnd)
        {
            // mid -> high
            float t = Mathf.InverseLerp(highStart, highEnd, smoothedRPM);
            midV = 1f - t; highV = t;
        }
        else if (smoothedRPM < veryHighStart)
        {
            // fully high
            highV = 1f;
        }
        else if (smoothedRPM < veryHighEnd)
        {
            // high -> veryHigh
            float t = Mathf.InverseLerp(veryHighStart, veryHighEnd, smoothedRPM);
            highV = 1f - t; veryHighV = t;
        }
        else
        {
            // fully veryHigh
            veryHighV = 1f;
        }

        // throttle shapes overall energy
        float throttleShape = Mathf.Pow(Mathf.Clamp01(throttle), 1.2f);
        float baseGain = 0.45f + 0.7f * throttleShape;

        float targetIdleVol = idleV * baseGain;
        float targetMidVol = midV * baseGain;
        float targetHighVol = highV * baseGain;
        float targetVeryHighVol = veryHighV * baseGain;

        float alphaV = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, volSmoothTime));
        smoothedIdleVol = Mathf.Lerp(smoothedIdleVol, targetIdleVol, alphaV);
        smoothedMidVol = Mathf.Lerp(smoothedMidVol, targetMidVol, alphaV);
        smoothedHighVol = Mathf.Lerp(smoothedHighVol, targetHighVol, alphaV);
        smoothedVeryHighVol = Mathf.Lerp(smoothedVeryHighVol, targetVeryHighVol, alphaV);

        ApplyVolumesAndPitch();

        // Mixer LPF
        if (audioMixer != null && !string.IsNullOrEmpty(mixerLPFParam))
        {
            float cutoff = 2000f + 10000f * n - 2500f * Mathf.Clamp01(load);
            cutoff = Mathf.Clamp(cutoff, 200f, 20000f);
            audioMixer.SetFloat(mixerLPFParam, cutoff);
        }
    }

    float Band(float rpm, float start, float end)
    {
        float s = Mathf.InverseLerp(start - bandBlend, start, rpm);
        float e = 1f - Mathf.InverseLerp(end - bandBlend, end, rpm);
        return Mathf.Clamp01(s * e);
    }

    void ApplyVolumesAndPitch()
    {
        if (idleSource != null) idleSource.volume = smoothedIdleVol;
        if (midSource != null) midSource.volume = smoothedMidVol;
        if (highSource != null) highSource.volume = smoothedHighVol;
        if (veryHighSource != null) veryHighSource.volume = smoothedVeryHighVol;

        if (idleSource != null) idleSource.pitch = smoothedPitch;
        if (midSource != null) midSource.pitch = smoothedPitch;
        if (highSource != null) highSource.pitch = smoothedPitch;
        if (veryHighSource != null) veryHighSource.pitch = smoothedPitch;
    }
}
