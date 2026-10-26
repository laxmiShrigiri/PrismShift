using UnityEngine;
using UnityEngine.SceneManagement;

// Purpose: Stores scene names and validates that a play session starts from HomeScene.
/// <summary>Central scene-name constants and initial-scene validation.</summary>
public static class SceneFlowBootstrap
{
    public const string HomeSceneName = "HomeScene";
    public const string SelectionSceneName = "SelectionScene";
    public const string GameplaySceneName = "PrismShift";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartupSceneManager.ValidateInitialScene(scene);
    }
}
