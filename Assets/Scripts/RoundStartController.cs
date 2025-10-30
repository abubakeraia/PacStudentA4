using UnityEngine;
using TMPro;
using System.Collections;

public class RoundStartController : MonoBehaviour
{
    public HUDController hud;
    public BgmPlayer bgm;
    public PacStudentController pac;
    public GhostController[] ghosts;

    [Header("UI")]
    public GameObject roundStartPanel;
    public TextMeshProUGUI countdownText;

    [Header("Timing")]
    public float beat = 1f;

    void Start()
    {
        StartCoroutine(RunRoundStart());
    }

    IEnumerator RunRoundStart()
    {
        hud.StopTimer();
        hud.ResetTimer();

        if (roundStartPanel) roundStartPanel.SetActive(true);
        if (countdownText) countdownText.text = "3";

        Freeze(true);

        yield return new WaitForSecondsRealtime(beat);
        if (countdownText) countdownText.text = "2";
        yield return new WaitForSecondsRealtime(beat);
        if (countdownText) countdownText.text = "1";
        yield return new WaitForSecondsRealtime(beat);
        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(beat);

        if (roundStartPanel) roundStartPanel.SetActive(false);

        hud.StartTimer();
        Freeze(false);

        if (bgm) bgm.PlayNormalLoop();
    }

    void Freeze(bool on)
    {
        if (pac)
        {
            var rb = pac.GetComponent<Rigidbody2D>();
            if (on)
            {
                pac.enabled = false;
                if (rb) rb.simulated = false;
            }
            else
            {
                if (rb) rb.simulated = true;
                pac.enabled = true;
            }
        }

        if (ghosts != null)
        {
            foreach (var g in ghosts)
            {
                if (!g) continue;
                if (g.animator) g.animator.speed = on ? 0f : 1f;

                var rb = g.GetComponent<Rigidbody2D>();
                if (rb) { if (on) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; } }
                var ai = g.GetComponent<MonoBehaviour>(); 
                if (ai && ai != g) ai.enabled = !on;
            }
        }
    }
}
