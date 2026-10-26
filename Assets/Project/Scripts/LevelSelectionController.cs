using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Purpose: Fills the authored level grid from the database, shows progression, and starts selected levels.
/// <summary>
/// Populates the Selection Scene's authored grid and handles level progression and scene navigation.
/// </summary>
public sealed class LevelSelectionController : MonoBehaviour
{
    [SerializeField] private GameLevelsDatabaseSO levelsDatabase;
    [SerializeField] private RectTransform levelButtonsParent;

    private static readonly Color UnlockedCardColor = new Color32(30, 46, 72, 255);
    private static readonly Color LockedCardColor = new Color32(22, 29, 42, 255);
    private static readonly Color MainTextColor = new Color32(246, 248, 252, 255);
    private static readonly Color MutedTextColor = new Color32(178, 190, 209, 255);
    private static readonly Color AvailableTextColor = new Color32(142, 220, 176, 255);
    private static readonly Color AccentColor = new Color32(255, 193, 74, 255);

    private bool databaseIsValid;

    public int LevelCount => levelsDatabase != null ? levelsDatabase.TotalLevels : 0;
    public bool HasValidDatabase => databaseIsValid;

    private void Awake()
    {
        if (levelsDatabase == null)
        {
            Debug.LogError("[LevelSelectionController] No level database is assigned.");
            return;
        }

        databaseIsValid = levelsDatabase.Validate(out string validationError);
        if (!databaseIsValid)
            Debug.LogError($"[LevelSelectionController] Invalid level database: {validationError}");
    }

    private void Start()
    {
        if (!databaseIsValid)
            return;

        if (levelButtonsParent == null)
        {
            Debug.LogError("[LevelSelectionController] Assign the Level Grid RectTransform in the Inspector.");
            return;
        }

        BuildLevelButtons();
    }

    private void BuildLevelButtons()
    {
        int highestUnlocked = Mathf.Clamp(
            LevelProgressStore.LoadHighestUnlockedLevelIndex(0),
            0,
            levelsDatabase.TotalLevels - 1);

        for (int index = 0; index < levelsDatabase.TotalLevels; index++)
        {
            LevelEntry level = levelsDatabase.GetLevel(index);
            bool unlocked = index <= highestUnlocked;
            bool hasCompletedSnapshot = LevelSnapshotStore.TryLoad(
                index, out LevelSnapshotStore.Snapshot snapshot) && snapshot.isComplete;
            bool completed = index < highestUnlocked || hasCompletedSnapshot;

            CreateLevelButton(index, level, unlocked, completed);
        }
    }

    private void CreateLevelButton(int levelIndex, LevelEntry level, bool unlocked, bool completed)
    {
        GameObject cardObject = new GameObject($"Level {levelIndex + 1:00}", typeof(RectTransform));
        RectTransform card = cardObject.GetComponent<RectTransform>();
        card.SetParent(levelButtonsParent, false);

        Image background = cardObject.AddComponent<Image>();
        background.color = unlocked ? UnlockedCardColor : LockedCardColor;
        background.raycastTarget = true;

        Button button = cardObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.interactable = unlocked;
        button.onClick.AddListener(() => SelectLevel(levelIndex));
        ConfigureButtonColors(button, background.color);

        string title = string.IsNullOrWhiteSpace(level.levelName)
            ? $"LEVEL {levelIndex + 1:00}"
            : level.levelName;
        CreateLabel(card, "Level Name", title, 27,
            unlocked ? MainTextColor : MutedTextColor,
            new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.82f));

        string status = !unlocked ? "LOCKED" : completed ? "COMPLETED" : "AVAILABLE";
        Color statusColor = !unlocked ? MutedTextColor : completed ? AccentColor : AvailableTextColor;
        CreateLabel(card, "Level Status", status, 18, statusColor,
            new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.44f));
    }

    private static void CreateLabel(RectTransform parent, string objectName, string value,
        float fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(parent, false);
        labelRect.anchorMin = anchorMin;
        labelRect.anchorMax = anchorMax;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(14f, fontSize - 6f);
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;
    }

    private static void ConfigureButtonColors(Button button, Color normalColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color32(45, 63, 92, 255);
        colors.pressedColor = new Color32(17, 26, 43, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.75f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
    }

    public void SelectLevel(int levelIndex)
    {
        if (!databaseIsValid)
        {
            Debug.LogError("[LevelSelectionController] The level database is missing or invalid.");
            return;
        }

        if (levelIndex < 0 || levelIndex >= levelsDatabase.TotalLevels)
            return;

        int highestUnlocked = Mathf.Clamp(
            LevelProgressStore.LoadHighestUnlockedLevelIndex(0),
            0,
            levelsDatabase.TotalLevels - 1);
        if (levelIndex > highestUnlocked)
            return;

        if (!GameSession.SetCampaign(levelsDatabase.Levels, levelIndex))
        {
            Debug.LogError("[LevelSelectionController] Could not copy the level campaign for gameplay.");
            return;
        }

        LevelProgressStore.SaveCurrentLevelIndex(levelIndex);
        SceneManager.LoadScene(SceneFlowBootstrap.GameplaySceneName);
    }

    /// <summary>Connect this method to the Selection scene's Back button.</summary>
    public void ReturnToHome()
    {
        SceneManager.LoadScene(SceneFlowBootstrap.HomeSceneName);
    }
}
