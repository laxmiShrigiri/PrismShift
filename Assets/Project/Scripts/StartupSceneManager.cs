using UnityEngine;
using UnityEngine.SceneManagement;

// Purpose: Redirects direct launches from other scenes to HomeScene, then allows normal scene changes.
/// <summary>
/// Enforces HomeScene as the application's entry point while allowing normal
/// scene changes after the first scene has loaded.
/// </summary>
public static class StartupSceneManager
{
    private static bool initialSceneValidated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewPlaySession()
    {
        initialSceneValidated = false;
    }

    public static bool ValidateInitialScene(Scene scene)
    {
        if (initialSceneValidated)
            return true;

        initialSceneValidated = true;
        if (scene.name == SceneFlowBootstrap.HomeSceneName)
            return true;

        Debug.LogWarning(
            $"[StartupSceneManager] Start from {SceneFlowBootstrap.HomeSceneName}. Returning to the Home scene.");
        SceneManager.LoadSceneAsync(SceneFlowBootstrap.HomeSceneName);
        return false;
    }
}
