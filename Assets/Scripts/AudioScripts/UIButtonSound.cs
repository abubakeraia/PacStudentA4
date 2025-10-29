// UIButtonSounds.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Attach to any UI Selectable (Button, Toggle, Dropdown, etc.).
/// Assign one shared AudioSource (on your Canvas or an Audio Manager) and your clips.
/// Plays on hover/highlight (pointer enter or selection) and on click/submit.
public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("Audio")]
    public AudioSource audioSource;      
    public AudioClip hoverClip;          
    public AudioClip clickClip;          

    [Header("Volumes")]
    [Range(0f, 1f)] public float hoverVolume = 0.9f;
    [Range(0f, 1f)] public float clickVolume = 1.0f;

    private bool hasPointer;

    void Reset()
    {
        if (audioSource == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) audioSource = canvas.GetComponent<AudioSource>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hasPointer = true;
        Play(hoverClip, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(clickClip, clickVolume);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!hasPointer) Play(hoverClip, hoverVolume);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Play(clickClip, clickVolume);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    void OnDisable()
    {
        hasPointer = false;
    }
}
