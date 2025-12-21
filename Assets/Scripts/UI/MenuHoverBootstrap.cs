using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuHoverBootstrap
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        Apply(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        string sceneName = scene.name;
        bool isRelevantScene = sceneName == SceneNames.StartMenu
                              || sceneName == SceneNames.TrackSelect
                              || sceneName == SceneNames.Track1
                              || sceneName == SceneNames.Track2
                              || sceneName == SceneNames.Track3;

        if (!isRelevantScene)
            return;

        MenuResizer.DisableIntroAnimations();
        MenuResizer.EnsureHoverAnimators();
    }
}
