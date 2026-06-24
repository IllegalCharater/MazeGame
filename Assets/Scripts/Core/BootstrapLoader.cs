using UnityEngine;
using UnityEngine.SceneManagement;

public static class BootstrapLoader
{
    private const string BootstrapSceneName = "Bootstrap";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapLoaded()
    {
        if (IsSceneLoaded(BootstrapSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(BootstrapSceneName))
        {
            Debug.LogWarning($"[BootstrapLoader] Scene '{BootstrapSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Additive);
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
                return true;
        }

        return false;
    }
}
