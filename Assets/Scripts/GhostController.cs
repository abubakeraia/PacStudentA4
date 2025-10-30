using UnityEngine;

public class GhostController : MonoBehaviour
{
    public Animator animator;

    [Header("Startup")]
    [Range(0, 3)] public int startDir = 3; 

    static readonly int HashDir = Animator.StringToHash("Dir");
    static readonly int HashIsScared = Animator.StringToHash("IsScared");
    static readonly int HashIsDead = Animator.StringToHash("IsDead");
    static readonly int HashIsRecovering = Animator.StringToHash("IsRecovering");

    bool hasRecovering;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);

        if (animator)
        {
            foreach (var p in animator.parameters)
                if (p.nameHash == HashIsRecovering) { hasRecovering = true; break; }
        }
    }

    void Start()
    {
        
        if (animator) animator.SetInteger(HashDir, startDir);
    }

    public int CurrentDir => animator ? animator.GetInteger(HashDir) : startDir;

    public void SetDir(int dir)              
    {
        if (!animator) return;
        animator.SetInteger(HashDir, Mathf.Clamp(dir, 0, 3));
    }

    public bool IsDead => animator && animator.GetBool(HashIsDead);

    public void SetNormal()
    {
        if (!animator || IsDead) return;
        animator.SetBool(HashIsScared, false);
        if (hasRecovering) animator.SetBool(HashIsRecovering, false);
        // DO NOT touch Dir
    }

    public void SetScared()
    {
        if (!animator || IsDead) return;
        animator.SetBool(HashIsScared, true);
        if (hasRecovering) animator.SetBool(HashIsRecovering, false);
    }

    public void SetRecovering()
    {
        if (!animator || IsDead) return;
        animator.SetBool(HashIsScared, true);
        if (hasRecovering) animator.SetBool(HashIsRecovering, true);
    }
}
