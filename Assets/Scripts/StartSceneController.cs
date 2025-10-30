using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartSceneController : MonoBehaviour
{
    [Header("Scene names")]
    public string level1SceneName = "Level1";
    public string innovationSceneName = "InnovationScene";

    public void PlayLevel1() => StartCoroutine(LoadAfterClickSfx(level1SceneName));
    public void PlayLevel2() => StartCoroutine(LoadAfterClickSfx(innovationSceneName));
    public void ReturnToStart() => StartCoroutine(LoadAfterClickSfx("StartScene"));

    private System.Collections.IEnumerator LoadAfterClickSfx(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            yield break;

        var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

        if (selected)
        {
            var btn = selected.GetComponent<Button>();
            if (btn) btn.interactable = false;
        }

        float wait = 0f;
        if (selected)
        {
            var sfx = selected.GetComponent<UIButtonSounds>();
            if (sfx != null && sfx.audioSource != null && sfx.clickClip != null)
            {
                if (!sfx.audioSource.isPlaying)
                    sfx.audioSource.PlayOneShot(sfx.clickClip, sfx.clickVolume);

                wait = sfx.clickClip.length / Mathf.Max(0.01f, sfx.audioSource.pitch);
            }
        }

        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        SceneManager.LoadScene(sceneName);
    }
}
