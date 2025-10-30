using UnityEngine;

[RequireComponent(typeof(GhostController))]
public class GhostStateController : MonoBehaviour
{
    public Animator animator;

    int hashIsScared, hashIsDead, hashIsRecovering;
    GhostController mover;

    void Awake()
    {
        hashIsScared = Animator.StringToHash("IsScared");
        hashIsDead = Animator.StringToHash("IsDead");
        hashIsRecovering = Animator.StringToHash("IsRecovering");
        mover = GetComponent<GhostController>();
        if (!animator) animator = GetComponent<Animator>();
    }

    public bool IsDead => animator && animator.GetBool(hashIsDead);
    public bool IsScared => animator && animator.GetBool(hashIsScared);
    public bool IsRecovering => animator && animator.GetBool(hashIsRecovering);

    public void SetNormal()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, false);
        animator.SetBool(hashIsScared, false);
        animator.SetBool(hashIsRecovering, false);
        if (mover) mover.SetNormal();
    }

    public void SetScared()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, false);
        animator.SetBool(hashIsScared, true);
        animator.SetBool(hashIsRecovering, false);
        if (mover) mover.SetScared();
    }

    public void SetRecovering()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, false);
        animator.SetBool(hashIsScared, true);
        animator.SetBool(hashIsRecovering, true);
        if (mover) mover.SetRecovering();
    }

    public void SetDead()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, true);
        animator.SetBool(hashIsScared, false);
        animator.SetBool(hashIsRecovering, false);
        if (mover) mover.SetDead();
    }
}
