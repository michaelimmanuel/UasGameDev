using UnityEngine;
using UnityEngine.Audio;

public class CarAudioController : MonoBehaviour
{
    public interface IVehicleTelemetry
    {
        float EngineRPM { get; }
        float Throttle { get; }
        float Load { get; }
        int Gear { get; }
        float Speed { get; }
    }

    [Header("Telemetry (optional)")]
    public MonoBehaviour telemetrySource; // optional; implement IVehicleTelemetry to auto-read

    // Fallback manual telemetry values
    float engineRPM;
    float throttle;
    float load;
    int gear;
    float speed;

    [Header("Audio Layers")]
    public AudioSource idleSource;
    public AudioSource lowSource;
    public AudioSource midSource;
    public AudioSource highSource;
    public AudioSource harmonicSource;
    public AudioSource transientSource; // one-shot/shift playback
    public AudioMixer audioMixer;
    public string mixerLPFParam = "Engine_LPF_Cutoff";

    [Header("Engine Settings")]
    public float idleRPM = 800f;
    public float maxRPM = 7000f;
    public float blendRPM = 400f; // overlap size for crossfades
    public float pitchAtIdle = 0.96f;
    public float pitchAtMax = 1.28f;
    public float pitchSmoothTime = 0.08f;
    public float volumeSmoothTime = 0.06f;
    public float redlineBoost = 0.2f;

    float smoothedPitch;
    float smoothedIdleVol, smoothedLowVol, smoothedMidVol, smoothedHighVol, smoothedHarmVol;

    IVehicleTelemetry telemetry;

    void Start()
    {
        if (telemetrySource is IVehicleTelemetry vt)
            telemetry = vt;

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
        if (telemetry != null)
        {
            engineRPM = telemetry.EngineRPM;
            throttle = telemetry.Throttle;
            load = telemetry.Load;
            gear = telemetry.Gear;
            speed = telemetry.Speed;
        }

        float n = Mathf.Clamp01((engineRPM - idleRPM) / Mathf.Max(1e-3f, (maxRPM - idleRPM)));

        float targetPitch = Mathf.Lerp(pitchAtIdle, pitchAtMax, Mathf.Pow(n, 0.9f));
        float pitchL = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, pitchSmoothTime));
        smoothedPitch = Mathf.Lerp(smoothedPitch, targetPitch, pitchL);

        ApplyPitch(smoothedPitch);

        // Volumes per band
        float idleV = BandVolume(engineRPM, 0f, idleRPM + 200f);
        float lowV = BandVolume(engineRPM, idleRPM - blendRPM * 0.5f, 2500f);
        float midV = BandVolume(engineRPM, 2000f, 5000f);
        float highV = BandVolume(engineRPM, 4500f, maxRPM + 200f);
        float harmV = Mathf.Clamp01(n * 1.2f) * throttle; // harmonic layer follows RPM & throttle

        // throttle modifies overall energy
        float throttleShape = Mathf.Pow(Mathf.Clamp01(throttle), 1.3f);
        float baseGain = 0.5f + 0.7f * throttleShape;

        float targetIdleVol = idleV * baseGain;
        float targetLowVol = lowV * baseGain;
        float targetMidVol = midV * baseGain;
        float targetHighVol = highV * baseGain + redlineBoost * Mathf.Clamp01((n - 0.9f) / 0.1f);
        float targetHarmVol = harmV * baseGain;

        float volL = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, volumeSmoothTime));
        smoothedIdleVol = Mathf.Lerp(smoothedIdleVol, targetIdleVol, volL);
        smoothedLowVol = Mathf.Lerp(smoothedLowVol, targetLowVol, volL);
        smoothedMidVol = Mathf.Lerp(smoothedMidVol, targetMidVol, volL);
        smoothedHighVol = Mathf.Lerp(smoothedHighVol, targetHighVol, volL);
        smoothedHarmVol = Mathf.Lerp(smoothedHarmVol, targetHarmVol, volL);

        ApplyVolumes();

        // Mixer LPF cutoff mapping
        if (audioMixer != null && !string.IsNullOrEmpty(mixerLPFParam))
        {
            float cutoff = 2000f + 10000f * n - 2500f * Mathf.Clamp01(load);
            cutoff = Mathf.Clamp(cutoff, 200f, 20000f);
            audioMixer.SetFloat(mixerLPFParam, cutoff);
        }
    }

    float BandVolume(float rpm, float bandStart, float bandEnd)
    {
        float s = Mathf.InverseLerp(bandStart - blendRPM, bandStart, rpm);
        float e = 1f - Mathf.InverseLerp(bandEnd - blendRPM, bandEnd, rpm);
        return Mathf.Clamp01(s * e);
    }

    void ApplyPitch(float p)
    {
        if (idleSource != null) idleSource.pitch = p;
        if (lowSource != null) lowSource.pitch = p;
        if (midSource != null) midSource.pitch = p;
        if (highSource != null) highSource.pitch = p;
        if (harmonicSource != null) harmonicSource.pitch = p;
    }

    void ApplyVolumes()
    {
        if (idleSource != null) idleSource.volume = smoothedIdleVol;
        if (lowSource != null) lowSource.volume = smoothedLowVol;
        if (midSource != null) midSource.volume = smoothedMidVol;
        if (highSource != null) highSource.volume = smoothedHighVol;
        if (harmonicSource != null) harmonicSource.volume = smoothedHarmVol;
    }

    // Play a one-shot transient (shift, backfire, etc.)
    public void PlayTransient(AudioClip clip, float volume = 1f, float pitchMultiplier = 1f)
    {
        if (transientSource == null || clip == null) return;
        transientSource.pitch = smoothedPitch * pitchMultiplier;
        transientSource.PlayOneShot(clip, volume);
    }
}
