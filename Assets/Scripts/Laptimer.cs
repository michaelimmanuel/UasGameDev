using UnityEngine;
using TMPro;

public class LapTimer : MonoBehaviour
{
    public Transform player;
    public TMP_Text lapTimeText;

    private bool lapRunning = false;
    private float lapTime = 0f;

    private bool canTrigger = true;

    void Update()
    {
        if (lapRunning)
        {
            lapTime += Time.deltaTime;
            lapTimeText.text = "Lap Time: " + lapTime.ToString("F2");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!canTrigger) return;

        StartCoroutine(TriggerCooldown());

        if (!lapRunning)
        {
            lapTime = 0f;
            lapRunning = true;
            Debug.Log("Lap Started");
        }
        else
        {
            lapRunning = false;
            Debug.Log("Lap Finished: " + lapTime.ToString("F2"));
        }
    }

    private System.Collections.IEnumerator TriggerCooldown()
    {
        canTrigger = false;
        yield return new WaitForSeconds(2f); // cegah bolak-balik
        canTrigger = true;
    }
}
