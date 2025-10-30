using UnityEngine;
using System.Collections;

public class ScareManager : MonoBehaviour
{
    public enum Phase { None, Scared, Recovering }

    public float scaredDuration = 10f;
    public float recoveringLastSeconds = 3f;
    public HUDController hud;
    public BgmPlayer bgm;

    Coroutine co;
    float remaining;

    public bool IsActive => remaining > 0f;
    public float Remaining => Mathf.Max(0f, remaining);
    public Phase CurrentPhase
    {
        get
        {
            if (remaining <= 0f) return Phase.None;
            return remaining <= recoveringLastSeconds ? Phase.Recovering : Phase.Scared;
        }
    }

    public void TriggerScared()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RunScared());
    }

    IEnumerator RunScared()
    {
        var ghosts = FindObjectsOfType<GhostController>();
        foreach (var g in ghosts) if (!g.IsDead) g.SetScared();

        if (bgm) bgm.PlayScaredLoop();
        if (hud) hud.StartScared(scaredDuration);

        remaining = scaredDuration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (remaining <= recoveringLastSeconds)
                foreach (var g in ghosts) if (!g.IsDead) g.SetRecovering();
            yield return null;
        }

        foreach (var g in ghosts) if (!g.IsDead) g.SetNormal();
        if (bgm) bgm.PlayNormalLoop();

        remaining = 0f;
        co = null;
    }
}
