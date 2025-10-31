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
        // Ensure animator is found if not already set
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        
        if (animator)
        {
            animator.SetBool(hashIsDead, false);
            animator.SetBool(hashIsScared, false);
            animator.SetBool(hashIsRecovering, false);
        }
        
        // Always call mover.SetNormal() to ensure mode is updated and animator is applied
        if (mover) mover.SetNormal();
    }

    public void SetScared()
    {
        // Ensure animator is found if not already set
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        
        if (animator)
        {
            animator.SetBool(hashIsDead, false);
            animator.SetBool(hashIsScared, true);
            animator.SetBool(hashIsRecovering, false);
        }
        
        // Always call mover.SetScared() to ensure mode is updated and animator is applied
        if (mover) mover.SetScared();
    }

    public void SetRecovering()
    {
        // Ensure animator is found if not already set
        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        
        if (animator)
        {
            animator.SetBool(hashIsDead, false);
            animator.SetBool(hashIsScared, true);
            animator.SetBool(hashIsRecovering, true);
        }
        
        // Always call mover.SetRecovering() to ensure mode is updated and animator is applied
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
