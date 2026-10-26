# Prism Shift

**A data-driven color-group puzzle built with Unity.** Move connected tiles into matching sockets, use the tray to stage pieces, and restore each pattern.

## Player flow

- The game opens on the Home scene. Select **Play** to open level selection.
- Level 1 is unlocked for a new player. Completing a level unlocks and automatically loads the next one after a short completion popup.
- Select any unlocked level to play it. Completed and in-progress levels can be replayed.
- Use **Back to Levels** in gameplay to return to level selection. The game scene has no level-skipping controls.
- If you close the game mid-level, the current level's board and tray arrangement are restored when you return.
- Use **Restart** to reset the current puzzle. The level selection **Back** button returns to Home.

Mouse and touch input are supported through Unity's EventSystem.

## Open the project

1. Install Unity **6000.0.69f1** with the 2D and Universal Render Pipeline modules.
2. Open this folder as a Unity project.
3. Open `Assets/Scenes/HomeScene.unity` and press Play.

The project uses the Input System, TextMesh Pro, and URP 2D. Home, level selection, and gameplay scenes are included in `ProjectSettings/EditorBuildSettings.asset` in launch order.

## Technical highlights

- Levels are authored as readable RGBA textures and stored in a ScriptableObject database. Transparent pixels are empty cells; visible pixels define the target color pattern.
- The board and tray are sized and built at runtime from each level's texture dimensions.
- Eight-direction flood fill selects connected tile groups and matching target regions.
- The shuffled tile set preserves the target color counts, so every generated level has the pieces required to solve it.
- The Editor-only level validator scans one or all `GameLevelsDatabaseSO` assets and reports data errors and warnings. Open it from **Tools → Prism Shift → Level Validator**.
- `GameplayZoomPan` adds mouse-wheel and touch-pinch zoom plus panning for gameplay content, while the game UI remains fixed.
- `LevelProgressStore` keeps the current level and highest unlocked level in `PlayerPrefs`.
- `LevelSnapshotStore` stores each level's board and tray arrangement, including partially solved states.
- `LevelSelectionController` fills the scene-authored level grid with buttons from the shared ScriptableObject database, marks locked and completed levels, and validates selections.
- `GameSession` passes copied campaign settings and the selected index to gameplay for the current play session. The `GameLevelsDatabaseSO` is assigned only in `SelectionScene`; gameplay can load later levels from the copied campaign without another database reference.
- `LevelManager` stays in `PrismShift` and builds the selected level from that session data.
- Completing a level saves the unlock and snapshot, displays a brief completion popup, then loads the next level. Completing the final level returns to level selection.
- `StartupSceneManager` routes direct launches from Selection or gameplay back to Home while allowing normal scene transitions after startup.
- `HomeSceneController` loads `SelectionScene` from the Play button in the scene-authored Home UI. The Selection Scene keeps its authored layout while `LevelSelectionController` generates the level buttons inside its `Level Grid` and handles the Back button.

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/Project/Scripts/` | Runtime gameplay, scene flow, level data, progress storage, and UI |
| `Assets/Project/Prefab/` | Board and tray prefabs plus the level database |
| `Assets/Project/Sprites/Levels/` | Pixel-art level textures |
| `Assets/Scenes/HomeScene.unity` | Home screen |
| `Assets/Scenes/SelectionScene.unity` | Level selection screen |
| `Assets/Scenes/PrismShift.unity` | Playable game scene |
| `Docs/ARCHITECTURE.md` | Runtime responsibilities and scene flow |

## Build

Open **File → Build Profiles**, choose a platform installed in your Unity Editor, and build the project. Generated players and Unity's cache folders are excluded from version control; build a fresh player locally when you are ready to share one.

## Tests

The Edit Mode tests are in `Assets/Editor/Tests/PrismShiftEditModeTests.cs`. In Unity, open **Window → General → Test Runner**, choose **EditMode**, and run the tests. They cover color matching, connected board searches, campaign copying, level progress saving, and puzzle snapshot save/load.

## Controls

| Input | Action |
| --- | --- |
| Click or tap an unsolved board tile | Select its connected same-color group |
| Click or tap a matching empty socket group | Place selected tiles |
| Click or tap a tray slot | Store selected board tiles or select matching stored tiles |
| Mouse wheel or two-finger pinch | Zoom the gameplay board and tray |
| Drag while zoomed in | Pan gameplay content |
| Restart button | Reset the current level |
| Back to Levels button | Save progress and return to level selection |
| Level Selection Back button | Return to Home |
