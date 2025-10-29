using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    public string level1SceneName = "Level1";
    public string innovationSceneName = "InnovationScene";

    public void PlayLevel1()  => LoadIfAvailable(level1SceneName);
    public void PlayLevel2()  => LoadIfAvailable(innovationSceneName);

    void LoadIfAvailable(string name)
    {
        if (Application.CanStreamedLevelBeLoaded(name)) SceneManager.LoadScene(name);
        else Debug.LogWarning($"Scene '{name}' not found in Build Profiles.");
    }
}
