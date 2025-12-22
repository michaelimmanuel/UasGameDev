using System.Collections;
using TMPro;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("UI (VALUE ONLY - TextMeshPro)")]
    [Tooltip("Isi hanya bagian angka/value (kolom kanan). Label dibuat statis di UI.")]
    public TMP_Text currentLapValueText;
    public TMP_Text lastLapValueText;
    public TMP_Text bestLapValueText;
    public TMP_Text deltaValueText;
    public TMP_Text lapCountValueText;

    [Header("Settings")]
    [Tooltip("Cooldown supaya trigger tidak kepanggil bolak-balik.")]
    public float triggerCooldown = 2f;

    [Tooltip("0 = infinity (∞). Kalau > 0, tampilkan Lap x/Total.")]
    public int totalLaps = 0;

    [Tooltip("Kalau true: lap pertama auto start saat pertama kali lewat garis.")]
    public bool startOnFirstTrigger = true;

    // Runtime state
    private bool lapRunning = false;
    private float currentLapTime = 0f;

    private float lastLapTime = -1f;
    private float bestLapTime = -1f;
    private float previousLapTime = -1f;

    private int currentLapNumber = 1;
    private bool canTrigger = true;

    private void Start()
    {
        // Init UI (value only)
        SetCurrentLapValueUI(0f);
        SetLastLapValueUI(-1f);
        SetBestLapValueUI(-1f);
        SetDeltaValueUI(float.NaN); // tampilkan ---
        SetLapCountValueUI();
    }

    private void Update()
    {
        if (!lapRunning) return;

        currentLapTime += Time.deltaTime;
        SetCurrentLapValueUI(currentLapTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!canTrigger) return;

        StartCoroutine(TriggerCooldown());

        // Kalau belum running
        if (!lapRunning)
        {
            if (startOnFirstTrigger)
                StartLap();
            return;
        }

        // Kalau sedang running: finish lap lalu langsung start lap baru
        FinishLapAndStartNext();
    }

    private void StartLap()
    {
        lapRunning = true;
        currentLapTime = 0f;

        SetCurrentLapValueUI(currentLapTime);
        SetLapCountValueUI();

        // Delta belum valid sampai minimal punya 2 lap selesai
        if (lastLapTime < 0f) SetDeltaValueUI(float.NaN);
    }

    private void FinishLapAndStartNext()
    {
        lapRunning = false;

        float finishedLap = currentLapTime;

        // Delta = lap terakhir - lap sebelumnya
        float delta = float.NaN;
        if (lastLapTime >= 0f) // artinya sudah ada lap sebelumnya
        {
            previousLapTime = lastLapTime;
            delta = finishedLap - previousLapTime;
        }

        // Update last lap
        lastLapTime = finishedLap;
        SetLastLapValueUI(lastLapTime);

        // Update best lap
        if (bestLapTime < 0f || finishedLap < bestLapTime)
        {
            bestLapTime = finishedLap;
            SetBestLapValueUI(bestLapTime);
        }

        // Update delta
        SetDeltaValueUI(delta);

        // Next lap number
        currentLapNumber++;
        if (currentLapNumber < 1) currentLapNumber = 1;

        // Auto reset & start lap baru
        StartLap();
    }

    private IEnumerator TriggerCooldown()
    {
        canTrigger = false;
        yield return new WaitForSeconds(triggerCooldown);
        canTrigger = true;
    }

    // ---------------- UI Helpers (VALUE ONLY) ----------------

    private void SetCurrentLapValueUI(float t)
    {
        if (currentLapValueText == null) return;
        currentLapValueText.text = FormatTime(t);
    }

    private void SetLastLapValueUI(float t)
    {
        if (lastLapValueText == null) return;

        if (t < 0f) lastLapValueText.text = "--:--.---";
        else lastLapValueText.text = FormatTime(t);
    }

    private void SetBestLapValueUI(float t)
    {
        if (bestLapValueText == null) return;

        if (t < 0f) bestLapValueText.text = "--:--.---";
        else bestLapValueText.text = FormatTime(t);
    }

    private void SetDeltaValueUI(float delta)
    {
        if (deltaValueText == null) return;

        if (float.IsNaN(delta))
        {
            deltaValueText.text = "---";
            return;
        }

        string sign = delta >= 0f ? "+" : "-";
        deltaValueText.text = $"{sign}{FormatDelta(Mathf.Abs(delta))}";
    }

    private void SetLapCountValueUI()
    {
        if (lapCountValueText == null) return;

        if (totalLaps <= 0) lapCountValueText.text = $"{currentLapNumber} / ∞";
        else lapCountValueText.text = $"{currentLapNumber} / {totalLaps}";
    }

    // Format utama: M:SS.mmm
    private string FormatTime(float timeSec)
    {
        int totalMs = Mathf.Max(0, Mathf.RoundToInt(timeSec * 1000f));
        int minutes = totalMs / 60000;
        int seconds = (totalMs % 60000) / 1000;
        int millis = totalMs % 1000;
        return $"{minutes}:{seconds:00}.{millis:000}";
    }

    // Delta: SS.mmm (kalau >= 1 menit jadi M:SS.mmm)
    private string FormatDelta(float timeSec)
    {
        int totalMs = Mathf.Max(0, Mathf.RoundToInt(timeSec * 1000f));
        int minutes = totalMs / 60000;
        int seconds = (totalMs % 60000) / 1000;
        int millis = totalMs % 1000;

        if (minutes <= 0) return $"{seconds}.{millis:000}";
        return $"{minutes}:{seconds:00}.{millis:000}";
    }

    // Optional: panggil ini kalau kamu mau reset dari UI button / restart race
    public void ResetTimerAndRecords()
    {
        lapRunning = false;
        currentLapTime = 0f;

        lastLapTime = -1f;
        bestLapTime = -1f;
        previousLapTime = -1f;

        currentLapNumber = 1;

        SetCurrentLapValueUI(0f);
        SetLastLapValueUI(-1f);
        SetBestLapValueUI(-1f);
        SetDeltaValueUI(float.NaN);
        SetLapCountValueUI();
    }
}
