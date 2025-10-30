using UnityEngine;

public class GhostController : MonoBehaviour
{
    public Animator animator;

    int hashIsScared, hashIsDead, hashIsRecovering;

    void Awake()
    {
        hashIsScared = Animator.StringToHash("IsScared");
        hashIsDead = Animator.StringToHash("IsDead");
        hashIsRecovering = Animator.StringToHash("IsRecovering");
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
    }

    public void SetScared()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, false);
        animator.SetBool(hashIsScared, true);
        animator.SetBool(hashIsRecovering, false);
    }

    public void SetRecovering()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, false);
        animator.SetBool(hashIsScared, true);
        animator.SetBool(hashIsRecovering, true);
    }

    public void SetDead()
    {
        if (!animator) return;
        animator.SetBool(hashIsDead, true);
        animator.SetBool(hashIsScared, false);
        animator.SetBool(hashIsRecovering, false);
    }
}
