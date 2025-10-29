using UnityEngine;
public class FootstepSfx : MonoBehaviour {

    public AudioSource source;              
    public AudioClip step;

    public Vector2 pitch = new(0.95f, 1.05f);

    public float volume = 1f;

    public void Footstep() {
        if (!source) source = GetComponent<AudioSource>();
        if (!step || !source) return;
        source.pitch = Random.Range(pitch.x, pitch.y);
        source.PlayOneShot(step, volume);
    }
}
