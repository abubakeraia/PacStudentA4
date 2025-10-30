using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PacGhostCollisionHandler : MonoBehaviour
{
    [Header("UI")]
    public LivesDisplay livesDisplay;

    [Header("Refs")]
    public GhostController[] ghosts;
    public ParticleSystem deathVfxPrefab;
    public AudioClip deathSfx;
    public ScareManager scareManager;
    public BgmPlayer bgm;

    [Header("Spawn Points")]
    public Vector3 pacSpawnWorld = new Vector3(-12.37f, 11.75f, 0f);
    public Transform[] ghostStartPoints;

    [Header("Rules")]
    public int startingLives = 3;
    public float deathFreezeSeconds = 1.2f;
    public int ghostEatScore = 300;
    public float ghostDeadSeconds = 3f;

    Animator pacAnimator;
    Collider2D pacCollider;
    Rigidbody2D pacRb;
    PacStudentController pacController;

    int lives;
    bool dying;

    Vector3[] ghostInitialPositions;

    void Awake()
    {
        pacAnimator = GetComponent<Animator>();
        pacCollider = GetComponent<Collider2D>();
        pacRb = GetComponent<Rigidbody2D>();
        pacController = GetComponent<PacStudentController>();

        if (ghosts != null && ghosts.Length > 0)
        {
            ghostInitialPositions = new Vector3[ghosts.Length];
            for (int i = 0; i < ghosts.Length; i++)
                ghostInitialPositions[i] = ghosts[i] ? ghosts[i].transform.position : Vector3.zero;
        }
    }

    void Start()
    {
        lives = startingLives;
        UpdateLivesUI();
        HardResetPositions();
        StartCoroutine(WaitForAnyKey());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ghost"))
        {
            var g = other.GetComponentInParent<GhostController>();
            if (!g) return;

            if (g.IsDead) return;

            if (g.IsScared || g.IsRecovering)
            {
                StartCoroutine(GhostEaten(g));
                return;
            }

            if (!dying) StartCoroutine(DieAndRespawn());
        }
    }

    IEnumerator GhostEaten(GhostController g)
    {
        g.SetDead();

        var hud = FindObjectOfType<HUDController>();
        hud?.AddScore(ghostEatScore);

        if (bgm) bgm.PlayDeadLoop();

        yield return new WaitForSecondsRealtime(ghostDeadSeconds);

        var phase = scareManager ? scareManager.CurrentPhase : ScareManager.Phase.None;

        int idx = IndexOfGhost(g);
        Vector3 spawnPos = g.transform.position;
        if (idx >= 0)
        {
            if (ghostStartPoints != null && idx < ghostStartPoints.Length && ghostStartPoints[idx])
                spawnPos = ghostStartPoints[idx].position;
            else if (ghostInitialPositions != null && idx < ghostInitialPositions.Length)
                spawnPos = ghostInitialPositions[idx];
        }
        g.transform.position = spawnPos;

        switch (phase)
        {
            case ScareManager.Phase.Scared: g.SetScared(); break;
            case ScareManager.Phase.Recovering: g.SetRecovering(); break;
            default: g.SetNormal(); break;
        }
    }

    int IndexOfGhost(GhostController g)
    {
        for (int i = 0; i < ghosts.Length; i++) if (ghosts[i] == g) return i;
        return -1;
    }

    IEnumerator DieAndRespawn()
    {
        dying = true;

        if (pacController) pacController.enabled = false;
        if (pacRb) pacRb.simulated = false;
        if (pacCollider) pacCollider.enabled = false;

        if (pacAnimator) pacAnimator.SetBool("IsDead", true);

        foreach (var gg in ghosts) if (gg && gg.animator) gg.animator.speed = 0f;

        if (deathVfxPrefab)
        {
            var vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            var main = vfx.main; main.stopAction = ParticleSystemStopAction.Destroy;
            vfx.Play();
        }
        if (deathSfx) AudioSource.PlayClipAtPoint(deathSfx, transform.position);

        yield return new WaitForSecondsRealtime(deathFreezeSeconds);

        lives = Mathf.Max(0, lives - 1);
        UpdateLivesUI();

        if (pacAnimator) pacAnimator.SetBool("IsDead", false);

        if (lives <= 0)
        {
            SceneManager.LoadScene("StartScene");
            yield break;
        }

        HardResetPositions();

        if (pacRb) pacRb.simulated = true;
        if (pacCollider) pacCollider.enabled = true;

        yield return WaitForAnyKey();

        if (pacController) pacController.enabled = true;
        foreach (var gg in ghosts) if (gg && gg.animator) gg.animator.speed = 1f;

        dying = false;
    }

    void HardResetPositions()
    {
        if (pacController) pacController.ResetTo(pacSpawnWorld);
        else transform.position = pacSpawnWorld;
        if (pacRb) pacRb.velocity = Vector2.zero;

        for (int i = 0; i < ghosts.Length; i++)
        {
            var g = ghosts[i];
            if (!g) continue;

            Vector3 targetPos =
                (ghostStartPoints != null && i < ghostStartPoints.Length && ghostStartPoints[i])
                ? ghostStartPoints[i].position
                : (ghostInitialPositions != null && i < ghostInitialPositions.Length ? ghostInitialPositions[i] : g.transform.position);

            g.transform.position = targetPos;

            var grb = g.GetComponent<Rigidbody2D>();
            if (grb) { grb.velocity = Vector2.zero; grb.angularVelocity = 0f; }

            g.SetNormal();
            if (g.animator) g.animator.speed = 1f;
        }
    }

    void UpdateLivesUI()
    {
        if (livesDisplay) livesDisplay.SetLives(lives);
    }

    IEnumerator WaitForAnyKey()
    {
        while (!Input.anyKeyDown) yield return null;
    }
}
