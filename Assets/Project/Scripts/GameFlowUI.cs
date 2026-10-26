using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Purpose: Connects the scene-authored gameplay UI to level progress and game actions.
public sealed class GameFlowUI : MonoBehaviour
{
    [Header("Scene UI References")]
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text levelNameLabel;
    [SerializeField] private TMP_Text completionTitle;
    [SerializeField] private TMP_Text completionMessage;
    [SerializeField] private GameObject completionOverlay;
    [SerializeField] private Button completionBackButton;
    [SerializeField] private Button completionReplayButton;

    private LevelManager levelManager;

    private void Awake()
    {
        levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("[GameFlowUI] A LevelManager is required on the same GameObject.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (levelManager == null)
            levelManager = GetComponent<LevelManager>();

        if (levelManager != null)
        {
            levelManager.LevelLoaded += HandleLevelLoaded;
            levelManager.LevelCompleted += HandleLevelCompleted;
        }
    }

    private void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.LevelLoaded -= HandleLevelLoaded;
            levelManager.LevelCompleted -= HandleLevelCompleted;
        }
    }

    private void Start()
    {
        // The level may already be ready if LevelManager ran before this component.
        if (levelManager != null && levelManager.CurrentLevel != null)
            HandleLevelLoaded(levelManager.CurrentLevel, levelManager.CurrentLevelIndex, levelManager.LevelCount);
    }

    private void HandleLevelLoaded(LevelEntry level, int index, int total)
    {
        if (levelLabel != null)
            levelLabel.text = $"LEVEL {index + 1:00} / {total:00}";

        if (levelNameLabel != null)
            levelNameLabel.text = string.IsNullOrWhiteSpace(level.levelName)
                ? "Restore the pattern"
                : level.levelName;

        if (completionOverlay != null)
            completionOverlay.SetActive(false);
    }

    private void HandleLevelCompleted(bool finalLevel, bool willAutoAdvance)
    {
        if (completionOverlay == null)
            return;

        if (completionTitle != null)
        {
            completionTitle.text = finalLevel && willAutoAdvance
                ? "ALL PATTERNS RESTORED"
                : "LEVEL COMPLETE";
        }

        if (completionMessage != null)
        {
            completionMessage.text = willAutoAdvance
                ? finalLevel
                    ? "All levels complete. Returning to level selection..."
                    : "Next level loading..."
                : "This level was already completed. Replay it or return to level selection.";
        }

        if (completionBackButton != null)
            completionBackButton.gameObject.SetActive(!willAutoAdvance);
        if (completionReplayButton != null)
            completionReplayButton.gameObject.SetActive(!willAutoAdvance);

        completionOverlay.SetActive(true);
    }

    // Hook this public method to the in-game Restart button's OnClick event.
    public void OnRestartButtonClicked()
    {
        if (levelManager == null)
            return;

        if (completionOverlay != null)
            completionOverlay.SetActive(false);

        levelManager.RestartLevel();
    }

    // Hook this public method to the in-game Back button's OnClick event.
    public void OnBackToLevelsButtonClicked()
    {
        if (levelManager == null)
            return;

        levelManager.SaveCurrentLevelSnapshot();
        SceneManager.LoadScene(SceneFlowBootstrap.SelectionSceneName);
    }
}
