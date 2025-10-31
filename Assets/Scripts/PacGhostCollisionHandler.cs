using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PacGhostCollisionHandler : MonoBehaviour
{
    [Header("Refs")]
    public GhostController[] ghosts;
    public ParticleSystem deathVfxPrefab;
    public AudioClip deathSfx;
    public ScareManager scareManager;
    public BgmPlayer bgm;
    public GameManager game;

    [Header("Spawn Points")]
    public Vector3 pacSpawnWorld = new Vector3(-12.37f, 11.75f, 0f);
    public Transform[] ghostStartPoints;

    [Header("Rules")]
    public float deathFreezeSeconds = 1.2f;
    public int ghostEatScore = 300;

    Animator pacAnimator;
    Collider2D pacCollider;
    Rigidbody2D pacRb;
    PacStudentController pacController;

    bool dying;

    Vector3[] ghostInitialPositions;

    void Awake()
    {
        pacAnimator = GetComponent<Animator>();
        pacCollider = GetComponent<Collider2D>();
        pacRb = GetComponent<Rigidbody2D>();
        pacController = GetComponent<PacStudentController>();
        if (!game) game = FindObjectOfType<GameManager>();

        if (ghosts != null && ghosts.Length > 0)
        {
            ghostInitialPositions = new Vector3[ghosts.Length];
            for (int i = 0; i < ghosts.Length; i++)
                ghostInitialPositions[i] = ghosts[i] ? ghosts[i].transform.position : Vector3.zero;
        }
    }

    void Start()
    {
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

        yield break;
    }

    IEnumerator DieAndRespawn()
    {
        dying = true;

        if (pacController) pacController.enabled = false;
        if (pacRb) pacRb.simulated = false;
        if (pacCollider) pacCollider.enabled = false;

        if (pacAnimator) pacAnimator.SetBool("IsDead", true);

        // Freeze all ghosts: disable their controllers AND freeze animations
        foreach (var gg in ghosts)
        {
            if (!gg) continue;
            if (gg.animator) gg.animator.speed = 0f;
            gg.enabled = false; // Disable GhostController to prevent movement during death sequence
        }

        // Play particle effect at death position
        if (deathVfxPrefab)
        {
            var vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            var main = vfx.main; main.stopAction = ParticleSystemStopAction.Destroy;
            vfx.Play();
        }
        if (deathSfx) AudioSource.PlayClipAtPoint(deathSfx, transform.position);

        yield return new WaitForSecondsRealtime(deathFreezeSeconds);

        if (game) game.OnPlayerCaught();

        if (pacAnimator) pacAnimator.SetBool("IsDead", false);

        HardResetPositions();

        if (pacRb) pacRb.simulated = true;
        if (pacCollider) pacCollider.enabled = true;

        yield return WaitForAnyKey();

        if (pacController) pacController.enabled = true;
        
        // Re-enable all ghosts after waiting for input
        foreach (var gg in ghosts)
        {
            if (!gg) continue;
            gg.enabled = true; // Re-enable GhostController
            if (gg.animator) gg.animator.speed = 1f;
        }

        dying = false;
    }

    void HardResetPositions()
    {
        if (pacController) pacController.ResetTo(pacSpawnWorld);
        else transform.position = pacSpawnWorld;
        if (pacRb) pacRb.linearVelocity = Vector2.zero;

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
            if (grb) { grb.linearVelocity = Vector2.zero; grb.angularVelocity = 0f; }

            g.SetNormal();
            if (g.animator) g.animator.speed = 1f;

            if (g.returnPoint == null && ghostStartPoints != null && i < ghostStartPoints.Length && ghostStartPoints[i])
                g.returnPoint = ghostStartPoints[i];
        }
    }

    IEnumerator WaitForAnyKey()
    {
        while (!Input.anyKeyDown) yield return null;
    }
}
