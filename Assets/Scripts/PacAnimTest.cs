using UnityEngine;
public class PacAnimTest : MonoBehaviour
{
    public Animator anim;
    void Reset() { anim = GetComponent<Animator>(); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) { anim.SetBool("IsDead", false); anim.SetInteger("Dir", 3); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { anim.SetBool("IsDead", false); anim.SetInteger("Dir", 0); }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { anim.SetBool("IsDead", false); anim.SetInteger("Dir", 1); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { anim.SetBool("IsDead", false); anim.SetInteger("Dir", 2); }
        if (Input.GetKeyDown(KeyCode.Alpha9)) { anim.SetBool("IsDead", true); }
    }
}
