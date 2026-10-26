using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Purpose: Editor window that checks every level in a GameLevelsDatabaseSO and reports actionable issues.
public sealed class PrismShiftLevelValidatorWindow : EditorWindow
{
    private GameLevelsDatabaseSO selectedDatabase;
    private Vector2 resultsScrollPosition;
    private readonly List<DatabaseReport> reports = new List<DatabaseReport>();

    [MenuItem("Tools/Prism Shift/Level Validator")]
    private static void OpenWindow()
    {
        PrismShiftLevelValidatorWindow window = GetWindow<PrismShiftLevelValidatorWindow>();
        window.titleContent = new GUIContent("Level Validator");
        window.minSize = new Vector2(480f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Level Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prism Shift Level Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Check the level database before playing or building. Errors can prevent a level from loading; warnings point out data that may be intentional.",
            MessageType.Info);

        selectedDatabase = (GameLevelsDatabaseSO)EditorGUILayout.ObjectField(
            "Level Database", selectedDatabase, typeof(GameLevelsDatabaseSO), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(selectedDatabase == null))
            {
                if (GUILayout.Button("Validate Selected Database", GUILayout.Height(28f)))
                    AddOrReplaceReport(ValidateDatabase(selectedDatabase));
            }

            if (GUILayout.Button("Validate All Project Databases", GUILayout.Height(28f)))
                ValidateAllDatabases();
        }

        if (reports.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Choose a GameLevelsDatabaseSO asset, then validate it. You can also scan every database in the project.",
                MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Latest Results", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Results", GUILayout.Width(100f)))
                reports.Clear();
        }

        resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition);
        for (int i = 0; i < reports.Count; i++)
            DrawReport(reports[i]);
        EditorGUILayout.EndScrollView();
    }

    private void ValidateAllDatabases()
    {
        reports.Clear();
        string[] assetGuids = AssetDatabase.FindAssets("t:GameLevelsDatabaseSO");
        Array.Sort(assetGuids, StringComparer.Ordinal);

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            GameLevelsDatabaseSO database = AssetDatabase.LoadAssetAtPath<GameLevelsDatabaseSO>(assetPath);
            if (database == null)
                continue;

            reports.Add(ValidateDatabase(database));
        }

        if (reports.Count == 0)
            ShowNotification(new GUIContent("No GameLevelsDatabaseSO assets found."));
    }

    private void AddOrReplaceReport(DatabaseReport report)
    {
        for (int i = 0; i < reports.Count; i++)
        {
            if (reports[i].Database == report.Database)
            {
                reports[i] = report;
                return;
            }
        }

        reports.Insert(0, report);
    }

    private DatabaseReport ValidateDatabase(GameLevelsDatabaseSO database)
    {
        DatabaseReport report = new DatabaseReport(database);
        List<LevelEntry> levels = database.Levels;

        if (levels == null || levels.Count == 0)
        {
            report.AddIssue(true, -1, null, "The level database is empty.");
            return report;
        }

        Dictionary<string, int> firstLevelByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<Texture2D, int> firstLevelByTexture = new Dictionary<Texture2D, int>();

        for (int i = 0; i < levels.Count; i++)
        {
            LevelEntry level = levels[i];
            if (level == null)
            {
                report.AddIssue(true, i, null, "The level entry is missing.");
                continue;
            }

            ValidateName(report, level, i, firstLevelByName);
            ValidateTextureReference(report, level, i, firstLevelByTexture);
            ValidateSettings(report, level, i);

            if (level.levelTexture != null)
            {
                ValidateBoardArea(report, level, i);
                ValidateTextureCells(report, level, i);
            }
        }

        return report;
    }

    private static void ValidateName(
        DatabaseReport report, LevelEntry level, int index, Dictionary<string, int> firstLevelByName)
    {
        string normalizedName = string.IsNullOrWhiteSpace(level.levelName) ? string.Empty : level.levelName.Trim();
        if (normalizedName.Length == 0)
        {
            report.AddIssue(false, index, level.levelName,
                "The level name is blank, so it will be harder to identify in reports.");
            return;
        }

        if (firstLevelByName.TryGetValue(normalizedName, out int firstIndex))
        {
            report.AddIssue(false, index, level.levelName,
                $"The name is also used by Level {firstIndex + 1:00}.");
        }
        else
        {
            firstLevelByName.Add(normalizedName, index);
        }
    }

    private static void ValidateTextureReference(
        DatabaseReport report, LevelEntry level, int index, Dictionary<Texture2D, int> firstLevelByTexture)
    {
        Texture2D texture = level.levelTexture;
        if (texture == null)
        {
            report.AddIssue(true, index, level.levelName, "No texture is assigned.");
            return;
        }

        if (firstLevelByTexture.TryGetValue(texture, out int firstIndex))
        {
            report.AddIssue(false, index, level.levelName,
                $"This level reuses the texture assigned to Level {firstIndex + 1:00}.");
        }
        else
        {
            firstLevelByTexture.Add(texture, index);
        }

        if (!texture.isReadable)
        {
            report.AddIssue(true, index, level.levelName,
                "The texture must have Read/Write enabled in its import settings.");
        }
    }

    private static void ValidateSettings(DatabaseReport report, LevelEntry level, int index)
    {
        if (!IsPositiveFinite(level.targetBoardAreaSize))
        {
            report.AddIssue(true, index, level.levelName,
                "Board area width and height must be finite positive values.");
        }

        if (!IsFinite(level.spacing.x) || !IsFinite(level.spacing.y) ||
            level.spacing.x < 0f || level.spacing.y < 0f)
        {
            report.AddIssue(true, index, level.levelName,
                "Spacing values must be finite and cannot be negative.");
        }

        if (!IsFinite(level.boardOffset.x) || !IsFinite(level.boardOffset.y))
        {
            report.AddIssue(true, index, level.levelName,
                "Board offset values must be finite numbers.");
        }

        if (!IsFinite(level.socketScaleFactor) ||
            level.socketScaleFactor <= 0f || level.socketScaleFactor > 1f)
        {
            report.AddIssue(true, index, level.levelName,
                "Socket scale factor must be greater than zero and no more than one.");
        }

        if (level.manualSlotSize != Vector2.zero)
        {
            if (!IsPositiveFinite(level.manualSlotSize) || !IsPositiveFinite(level.manualSocketSize))
            {
                report.AddIssue(true, index, level.levelName,
                    "Manual slot and socket widths and heights must be finite positive values.");
            }
            else if (level.manualSocketSize.x > level.manualSlotSize.x ||
                     level.manualSocketSize.y > level.manualSlotSize.y)
            {
                report.AddIssue(false, index, level.levelName,
                    "The manual socket is larger than its slot and may overlap neighboring cells.");
            }
        }
        else if (level.manualSocketSize != Vector2.zero)
        {
            report.AddIssue(false, index, level.levelName,
                "Manual socket size is set but ignored because manual slot size is zero.");
        }
    }

    private static void ValidateBoardArea(DatabaseReport report, LevelEntry level, int index)
    {
        if (level.levelTexture == null || !IsPositiveFinite(level.targetBoardAreaSize) ||
            !IsFinite(level.spacing.x) || !IsFinite(level.spacing.y) ||
            level.spacing.x < 0f || level.spacing.y < 0f || level.manualSlotSize != Vector2.zero)
            return;

        float availableWidth = level.targetBoardAreaSize.x -
                               (level.levelTexture.width - 1) * level.spacing.x;
        float availableHeight = level.targetBoardAreaSize.y -
                                (level.levelTexture.height - 1) * level.spacing.y;
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            report.AddIssue(true, index, level.levelName,
                "Spacing leaves no room for the texture's cells inside the target board area.");
        }
    }

    private static void ValidateTextureCells(DatabaseReport report, LevelEntry level, int index)
    {
        Texture2D texture = level.levelTexture;
        if (texture == null || !texture.isReadable)
            return;

        try
        {
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a >= 0.1f)
                    return;
            }

            report.AddIssue(true, index, level.levelName,
                "The texture has no active cells (pixels with alpha of at least 0.1).");
        }
        catch (Exception exception)
        {
            report.AddIssue(true, index, level.levelName,
                $"The texture pixels could not be read: {exception.Message}");
        }
    }

    private static void DrawReport(DatabaseReport report)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(report.Database.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Asset", GUILayout.Width(90f)))
                {
                    Selection.activeObject = report.Database;
                    EditorGUIUtility.PingObject(report.Database);
                }
            }

            EditorGUILayout.LabelField(
                $"{report.Database.TotalLevels} levels • {report.ErrorCount} errors • {report.WarningCount} warnings");

            if (report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All levels passed validation.", MessageType.Info);
                return;
            }

            for (int i = 0; i < report.Issues.Count; i++)
            {
                ValidationIssue issue = report.Issues[i];
                EditorGUILayout.HelpBox(issue.FormatMessage(), issue.IsError ? MessageType.Error : MessageType.Warning);
            }
        }
    }

    private static bool IsPositiveFinite(Vector2 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && value.x > 0f && value.y > 0f;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private sealed class DatabaseReport
    {
        public readonly GameLevelsDatabaseSO Database;
        public readonly List<ValidationIssue> Issues = new List<ValidationIssue>();
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        public DatabaseReport(GameLevelsDatabaseSO database)
        {
            Database = database;
        }

        public void AddIssue(bool isError, int levelIndex, string levelName, string message)
        {
            Issues.Add(new ValidationIssue(isError, levelIndex, levelName, message));
            if (isError)
                ErrorCount++;
            else
                WarningCount++;
        }
    }

    private sealed class ValidationIssue
    {
        private readonly int levelIndex;
        private readonly string levelName;
        private readonly string message;

        public bool IsError { get; }

        public ValidationIssue(bool isError, int levelIndex, string levelName, string message)
        {
            IsError = isError;
            this.levelIndex = levelIndex;
            this.levelName = levelName;
            this.message = message;
        }

        public string FormatMessage()
        {
            if (levelIndex < 0)
                return message;

            string displayName = string.IsNullOrWhiteSpace(levelName)
                ? $"Level {levelIndex + 1:00}"
                : $"Level {levelIndex + 1:00} — {levelName}";
            return $"{displayName}: {message}";
        }
    }
}
