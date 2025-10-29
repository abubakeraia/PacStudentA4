using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PacStudentMover : MonoBehaviour
{
    public Transform[] corners;           
    public float speedUnitsPerSec = 4f;   

    public Animator animator;             
    public AudioSource moveSfx;           

    void Reset()
    {
        animator = GetComponent<Animator>();
        moveSfx = GetComponent<AudioSource>();
        if (moveSfx) { moveSfx.loop = true; moveSfx.playOnAwake = false; }
    }

    void OnEnable() { StartCoroutine(Cycle()); }

    IEnumerator Cycle()
    {
        if (corners == null || corners.Length < 2) yield break;
        int i = 0;
        transform.position = corners[0].position;

        while (true)
        {
            Vector3 a = corners[i].position;
            Vector3 b = corners[(i + 1) % corners.Length].position;

            Vector2 d = (b - a);
            SetDir(d);

            if (moveSfx && !moveSfx.isPlaying) moveSfx.Play();

            float dist = d.magnitude;
            float duration = dist / Mathf.Max(0.0001f, speedUnitsPerSec);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(a, b, t);
                yield return null;
            }
            transform.position = b;
            i = (i + 1) % corners.Length;
        }
    }

    void SetDir(Vector2 v)
    {
        if (!animator) return;
        int dir;
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) dir = (v.x >= 0f) ? 3 : 1; 
        else dir = (v.y >= 0f) ? 2 : 0;
        animator.SetInteger("Dir", dir);
    }
}
