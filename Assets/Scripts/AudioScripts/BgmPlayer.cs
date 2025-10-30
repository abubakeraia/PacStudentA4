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
        if (!source) source = GetComponent<AudioSource>();
        if (!source) source = GetComponentInChildren<AudioSource>();

        if (source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }
    }

    void Awake()
    {
        if (!source) Reset();
    }

    void Start()
    {
        if (!playOnStart) return;

        if (isStartScene)
        {
            Play(startBGM, true);
            return;
        }

        if (introBGM)
        {
            Play(introBGM, false);
            _switchTimer = Mathf.Min(introBGM.length, introMaxLength);
            _switchedToNormal = false;
        }
        else
        {
            Play(normalGhostBGM, true);
            _switchedToNormal = true;
        }
    }

    void Update()
    {
        if (_switchTimer >= 0f && !_switchedToNormal)
        {
            _switchTimer -= Time.deltaTime;
            if (_switchTimer <= 0f || !source.isPlaying)
            {
                Play(normalGhostBGM, true);
                _switchedToNormal = true;
                _switchTimer = -1f;
            }
        }
    }

    public void PlayNormalLoop()
    {
        _switchTimer = -1f;
        _switchedToNormal = true;
        Play(normalGhostBGM, true);
    }

    public void PlayScaredLoop()
    {
        _switchTimer = -1f;
        _switchedToNormal = false;
        Play(scaredGhostBGM, true);
    }

    public void PlayDeadLoop()
    {
        _switchTimer = -1f;
        _switchedToNormal = false;
        Play(deadGhostBGM, true);
    }

    void Play(AudioClip clip, bool loop)
    {
        if (!source || !clip) return;
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }

    public void StopAll()
    {
        if (!source) return;
        source.loop = false;
        source.Stop();
        source.clip = null;  
    }

}
