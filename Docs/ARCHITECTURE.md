# Runtime architecture

## Main components

- **`GameLevelsDatabaseSO`** stores ordered `LevelEntry` data and validates texture readability, active cells, and layout sizing before a level is built.
- **`LevelManager`** in `PrismShift` coordinates level construction, input decisions, connected-group searches, placements, completion, and progression events.
- **`LevelSelectionController`** reads the shared level database in `SelectionScene`, creates data-driven buttons inside the authored level grid, checks unlock state, and starts the chosen level.
- **`GameSession`** carries a copied campaign and selected level index between scenes for the current play session. Texture assets remain referenced; level settings are copied out of the ScriptableObject entries.
- **`StartupSceneManager`** enforces `HomeScene` as the initial scene and allows normal transitions after launch.
- **`BoardSlot`** stores a grid coordinate, its target color, and the tile currently occupying it.
- **`Tile`** owns its color, current home slot, selection state, and movement animation.
- **`TrayManager` / `TraySlot`** provide temporary storage for tiles between board regions.
- **`GameFlowUI`** presents gameplay progress, restart, return-to-selection, and completion actions while observing `LevelManager` events.
- **`HomeSceneController`** loads `SelectionScene` from the Play button in the scene-authored Home UI.
- **`SceneFlowBootstrap`** centralizes scene names and validates the initial scene; each menu keeps its layout authored in its scene.
- **`LevelProgressStore`** persists the current and highest unlocked level with `PlayerPrefs`.
- **`LevelSnapshotStore`** persists each level's board and tray arrangement, including partially solved states.

## Scene flow

1. `HomeScene` presents the game title and a Play button.
2. `SelectionScene` uses its authored layout. `LevelSelectionController` creates one button per database entry inside the assigned level grid. Locked levels are disabled. Completing a level unlocks the next level.
3. Selecting an unlocked level copies the campaign entries and selected index into `GameSession`, saves its index in `LevelProgressStore`, then opens `PrismShift`.
4. The gameplay scene saves the board and tray state after moves, on pause/quit, and before returning to selection.
5. `Back to Levels` returns to `SelectionScene`; the scene refreshes its unlocked level list from saved progress.

## Level load flow

1. `LevelManager` consumes the copied campaign and selected index from `GameSession`. Gameplay has no `GameLevelsDatabaseSO` reference.
2. It validates the selected entry's texture and its scene references, then builds a grid from that texture. Pixels with alpha below `0.1` are skipped.
3. The active target colors are copied and shuffled. No extra colors are introduced, preserving the target color counts.
4. Board slots, tiles, and tray slots are created from the level sizing settings.
5. `GameFlowUI` receives the `LevelLoaded` event and updates its level label.
6. A compatible per-level snapshot replaces the generated tile arrangement so a previous session can resume.

## Move and completion flow

1. Clicking an unsolved board tile finds its eight-direction connected group of equal-colored tiles.
2. The player places selected tiles into an adjacent connected region of empty sockets with the same target color, or stages them in the tray.
3. The manager checks completion after tile movement animations settle.
4. A newly completed level unlocks the next level, saves progress and its completed snapshot, then raises `LevelCompleted` for the popup. After a short delay, `LevelManager` builds the next entry from the copied campaign. Completing the final level returns to `SelectionScene` after the popup.
5. A level restored from an already-completed snapshot shows replay and return actions without auto-advancing, so revisiting a completed level does not skip it immediately.
6. Tile positions are checkpointed after moves and when the application pauses or closes; restarting a level clears its snapshot.
