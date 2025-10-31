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
    Phase phase = Phase.None;

    public bool IsActive => remaining > 0f;
    public float Remaining => Mathf.Max(0f, remaining);
    public Phase CurrentPhase { get { return phase; } }

    public void TriggerScared()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RunScared());
    }

    IEnumerator RunScared()
    {
        // Get all ghosts BEFORE starting the coroutine to ensure immediate update
        var ghosts = FindObjectsOfType<GhostController>();

        phase = Phase.Scared;
        remaining = scaredDuration;
        
        // Update all ghosts immediately, synchronously, before yielding
        foreach (var g in ghosts)
        {
            if (!g.IsDead)
            {
                // Check if ghost has GhostStateController (on same GameObject or parent/child) and use it
                var stateController = g.GetComponent<GhostStateController>();
                if (stateController == null)
                    stateController = g.GetComponentInChildren<GhostStateController>();
                if (stateController == null)
                    stateController = g.GetComponentInParent<GhostStateController>();
                    
                if (stateController != null)
                {
                    stateController.SetScared();
                }
                else
                {
                    // No GhostStateController, use GhostController directly
                    g.SetScared();
                }
            }
        }
        
        // Force all animators to update immediately
        foreach (var g in ghosts)
        {
            if (!g.IsDead && g.animator != null)
            {
                g.animator.Update(0f); // Force immediate animator update
            }
        }
        
        if (bgm) bgm.PlayScaredLoop();
        if (hud) hud.StartScared(scaredDuration);

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (remaining <= recoveringLastSeconds && phase != Phase.Recovering)
            {
                phase = Phase.Recovering;
                foreach (var g in ghosts)
                {
                    if (!g.IsDead)
                    {
                        var stateController = g.GetComponent<GhostStateController>();
                        if (stateController == null)
                            stateController = g.GetComponentInChildren<GhostStateController>();
                        if (stateController == null)
                            stateController = g.GetComponentInParent<GhostStateController>();
                            
                        if (stateController != null)
                            stateController.SetRecovering();
                        else
                            g.SetRecovering();
                    }
                }
            }
            yield return null;
        }

        phase = Phase.None;
        foreach (var g in ghosts)
        {
            if (!g.IsDead)
            {
                var stateController = g.GetComponent<GhostStateController>();
                if (stateController == null)
                    stateController = g.GetComponentInChildren<GhostStateController>();
                if (stateController == null)
                    stateController = g.GetComponentInParent<GhostStateController>();
                    
                if (stateController != null)
                    stateController.SetNormal();
                else
                    g.SetNormal();
            }
        }
        if (bgm) bgm.PlayNormalLoop();

        remaining = 0f;
        co = null;
    }
}
