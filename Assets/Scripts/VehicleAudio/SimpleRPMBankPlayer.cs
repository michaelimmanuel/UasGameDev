using System;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SimpleRPMBankPlayer : MonoBehaviour
{
    [Serializable]
    public struct SampleEntry
    {
        [Tooltip("Engine RPM this clip represents (e.g. 1000)")]
        public int rpm;
        public AudioClip clip;
    }

    [Header("Samples (assign each recorded RPM sample)")]
    public SampleEntry[] samples;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;

    [Header("Telemetry / Test")]
    public MonoBehaviour telemetrySource; // optional; implement interface below
    public float testRPM = 1000f; // editable for quick testing

    [Header("Tuning")]
    public float crossfadeSmoothTime = 0.06f; // seconds
    public float pitchClamp = 0.05f; // +/- 5%
    [Tooltip("Time constant (s) used to smooth incoming RPM to avoid abrupt sample switching")]
    public float rpmSmoothTime = 0.04f;

    interface ITelemetry
    {
        float EngineRPM { get; }
    }

    float currentRPM => telemetry != null ? telemetry.EngineRPM : testRPM;
    ITelemetry telemetry;

    float smoothedRPM = 0f;

    int lastLowerIndex = -1;
    float volA = 0f, volB = 0f;
    int assignedIndexA = -1;
    int assignedIndexB = -1;

    void Awake()
    {
        if (telemetrySource is ITelemetry t) telemetry = t;

        // Create AudioSources if not assigned
        if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();
        if (sourceB == null) sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceB.loop = true;
    }

    void Start()
    {
        // sort samples by rpm
        samples = samples.OrderBy(s => s.rpm).ToArray();
        if (samples.Length == 0) return;

        // initialize clips
        SetupInitialClips();

        // initialize smoothed RPM to current value so we don't start at 0
        smoothedRPM = currentRPM;
    }

    void SetupInitialClips()
    {
        // start both sources with lowest two clips (or same if only one)
        if (samples.Length == 1)
        {
            AssignClipToSourcePreservePhase(sourceA, samples[0].clip, ref assignedIndexA, 0);
            AssignClipToSourcePreservePhase(sourceB, samples[0].clip, ref assignedIndexB, 0);
            lastLowerIndex = 0;
        }
        else
        {
            AssignClipToSourcePreservePhase(sourceA, samples[0].clip, ref assignedIndexA, 0);
            AssignClipToSourcePreservePhase(sourceB, samples[1].clip, ref assignedIndexB, 1);
            lastLowerIndex = 0;
        }

        sourceA.Play();
        sourceB.Play();
    }

    // Assign clip but preserve phase: keep normalized playback position when swapping clips
    void AssignClipToSourcePreservePhase(AudioSource src, AudioClip clip, ref int assignedIndex, int index)
    {
        if (src.clip == clip)
        {
            assignedIndex = index;
            return;
        }

        float normalizedPos = 0f;
        if (src.clip != null && src.clip.length > 0f)
        {
            // use timeSamples for exact sample position
            int prevSamples = src.clip.samples;
            if (prevSamples > 0)
            {
                normalizedPos = (float)src.timeSamples / (float)prevSamples;
            }
        }

        src.clip = clip;
        if (clip != null && clip.samples > 0)
        {
            int newSamples = clip.samples;
            int targetSample = Mathf.Clamp((int)(normalizedPos * newSamples), 0, newSamples - 1);
            src.timeSamples = targetSample;
        }

        if (!src.isPlaying) src.Play();
        assignedIndex = index;
    }

    void Update()
    {
        if (samples.Length == 0) return;

        // smooth RPM to avoid rapid sample bin switching
        float targetRPM = currentRPM;
        float alphaR = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, rpmSmoothTime));
        smoothedRPM = Mathf.Lerp(smoothedRPM, targetRPM, alphaR);
        float rpm = smoothedRPM;

        // clamp to available sample range
        if (rpm <= samples[0].rpm)
        {
            // both play lowest
            UpdateSourceAssignments(0, 0, 0f);
            return;
        }

        if (rpm >= samples[samples.Length - 1].rpm)
        {
            int last = samples.Length - 1;
            UpdateSourceAssignments(last, last, 0f);
            return;
        }

        // find lower index
        int lower = 0;
        for (int i = 0; i < samples.Length - 1; i++)
        {
            if (rpm >= samples[i].rpm && rpm <= samples[i + 1].rpm)
            {
                lower = i;
                break;
            }
        }

        int upper = Math.Min(lower + 1, samples.Length - 1);
        float denom = (samples[upper].rpm - samples[lower].rpm);
        float t = denom <= 0f ? 0f : Mathf.Clamp01((rpm - samples[lower].rpm) / denom);

        UpdateSourceAssignments(lower, upper, t);
    }

    void UpdateSourceAssignments(int lowerIndex, int upperIndex, float t)
    {
        // assign clips to sources, preserving phase
        // Prefer keeping current assignments where possible to avoid reassigning both
        if (assignedIndexA == lowerIndex)
        {
            AssignClipToSourcePreservePhase(sourceA, samples[lowerIndex].clip, ref assignedIndexA, lowerIndex);
            AssignClipToSourcePreservePhase(sourceB, samples[upperIndex].clip, ref assignedIndexB, upperIndex);
        }
        else if (assignedIndexB == lowerIndex)
        {
            AssignClipToSourcePreservePhase(sourceB, samples[lowerIndex].clip, ref assignedIndexB, lowerIndex);
            AssignClipToSourcePreservePhase(sourceA, samples[upperIndex].clip, ref assignedIndexA, upperIndex);
        }
        else
        {
            // fallback: assign lower->A, upper->B
            AssignClipToSourcePreservePhase(sourceA, samples[lowerIndex].clip, ref assignedIndexA, lowerIndex);
            AssignClipToSourcePreservePhase(sourceB, samples[upperIndex].clip, ref assignedIndexB, upperIndex);
        }

        // target volumes
        float targetLowerVol = 1f - Mathf.SmoothStep(0f, 1f, t);
        float targetUpperVol = Mathf.SmoothStep(0f, 1f, t);

        float alpha = 1 - Mathf.Exp(-Time.deltaTime / Mathf.Max(1e-5f, crossfadeSmoothTime));
        volA = Mathf.Lerp(volA, targetLowerVol, alpha);
        volB = Mathf.Lerp(volB, targetUpperVol, alpha);

        // apply volumes
        sourceA.volume = volA;
        sourceB.volume = volB;

        // pitch correction (small)
        float targetPitchLower = samples[lowerIndex].rpm > 0 ? (currentRPM / (float)samples[lowerIndex].rpm) : 1f;
        float targetPitchUpper = samples[upperIndex].rpm > 0 ? (currentRPM / (float)samples[upperIndex].rpm) : 1f;
        targetPitchLower = Mathf.Clamp(targetPitchLower, 1f - pitchClamp, 1f + pitchClamp);
        targetPitchUpper = Mathf.Clamp(targetPitchUpper, 1f - pitchClamp, 1f + pitchClamp);

        sourceA.pitch = Mathf.Lerp(sourceA.pitch, targetPitchLower, alpha);
        sourceB.pitch = Mathf.Lerp(sourceB.pitch, targetPitchUpper, alpha);

        lastLowerIndex = lowerIndex;
    }

    // Public API for quick tests
    public void SetRPM(float rpm)
    {
        testRPM = rpm;
        // update smoothedRPM immediately to reflect external telemetry updates
        smoothedRPM = rpm;
        // helpful debug when testing
        if (!Application.isPlaying) Debug.Log($"SetRPM called: {rpm}");
    }
}
