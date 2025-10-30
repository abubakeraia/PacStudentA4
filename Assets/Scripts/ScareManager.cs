using UnityEngine;
using System.Collections;

public class ScareManager : MonoBehaviour
{
    public float scaredDuration = 10f;
    public float recoveringLastSeconds = 3f;
    public HUDController hud;
    public BgmPlayer bgm;   

    Coroutine co;

    public void TriggerScared()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RunScared());
    }

    IEnumerator RunScared()
    {
        var ghosts = FindObjectsOfType<GhostController>();
        Debug.Log($"ScareManager: ghosts found = {ghosts.Length}");

        foreach (var g in ghosts) if (!g.IsDead) g.SetScared();

        if (bgm) bgm.PlayScaredLoop();
        if (hud) hud.StartScared(scaredDuration);

        float t = scaredDuration;
        bool recoveringSet = false;

        while (t > 0f)
        {
            t -= Time.deltaTime;

            if (!recoveringSet && t <= recoveringLastSeconds)
            {
                foreach (var g in ghosts) if (!g.IsDead) g.SetRecovering();
                recoveringSet = true;
            }
            yield return null;
        }

        foreach (var g in ghosts) if (!g.IsDead) g.SetNormal();
        if (bgm) bgm.PlayNormalLoop();

        co = null;
    }
}
