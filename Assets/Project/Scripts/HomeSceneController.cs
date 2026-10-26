using UnityEngine;
using UnityEngine.SceneManagement;

// Purpose: Loads level selection when the scene-authored Home Play button is clicked.
public sealed class HomeSceneController : MonoBehaviour
{
    /// <summary>Connect this method to the Home scene's Play button On Click event.</summary>
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(SceneFlowBootstrap.SelectionSceneName);
    }
}
