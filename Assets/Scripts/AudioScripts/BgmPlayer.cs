using Unity.VisualScripting;
using UnityEngine;

public class BgmPlayer : MonoBehaviour
{

    [Header("Refs")]
    [SerializeField] private AudioSource source;

    [Header("If Start")]
    public bool isStartScene = false;
    public AudioClip startBGM;

    [Header("Clips")]
    [SerializeField] private AudioClip introBGM;
    [SerializeField] private AudioClip normalGhostBGM;
    [SerializeField] private AudioClip scaredGhostBGM;
    [SerializeField] private AudioClip deadGhostBGM;

    [Header("Audio Source Behaviour")]
    public float introMaxLength = 3f;
    public bool playOnStart = true;

    float _switchTimer = -1f;
    bool _switchedToNormal = false;

    void Reset()
    {
        if (!source) { source = GetComponent<AudioSource>(); }
        if (!source) { GetComponentInChildren<AudioSource>(); }

        if (source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }
    }

    private void Awake()
    {
        if (!source) { Reset(); }
    }


    void Start()
    {
        if (!playOnStart) { return; };

        if (isStartScene)
        {
            Play(startBGM, loop: true);
            return;
        }

        if (introBGM)
        {
            Play(introBGM, loop: false);

            //getting whichever is shorter & setting timer
            _switchTimer = Mathf.Min(introBGM.length, introMaxLength);

        }

        else
        {
            Play(normalGhostBGM, loop: true);
        }
    }

    private void Update()
    {
        if (_switchTimer >= 0f && !_switchedToNormal)
        {
            _switchTimer -= Time.deltaTime;
        }

        //check timer finishe or audio finished
        if (_switchTimer == 0f || !source.isPlaying)
        {
            Play(normalGhostBGM, true);
            _switchedToNormal = true;
            _switchTimer = -1f;
        }
    }

    void Play(AudioClip clip, bool loop)
    {
        if (!source || !clip) { Debug.LogError("No Audio source or clip assigned.", this);  return; }

        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }
}
