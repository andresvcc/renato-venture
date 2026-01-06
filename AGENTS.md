# Repository Guidelines


## Project Structure & Module Organization
- Code: `Assets/Scripts` (`Player`, `Enemies`, `Environment`, `AnimationStates`). One MonoBehaviour per file; filenames mirror class names.
- Visuals & animation: `Assets/Animations` (anim/animator controllers), `Assets/Art/Sprites` (PNG sprites and sprite sheets).
- Prefabs: `Assets/Prefabs` with subfolders `Environment`, `Enemies`, `Particles`, `Weapons`. Keep variants suffixed (`_Mobile`, `_v2`).
- Audio: `Assets/Audio/SFX` for gameplay sounds. Add music later under `Assets/Audio/Music`.
- Materials: `Assets/Materials`; Physics materials (e.g., `Slippery.physicsMaterial2D`) stay here.
- Scenes: `Assets/Scenes/Demo.unity` is the current entry point; duplicate for new levels (`Level01.unity`, `BossArena.unity`).
- Docs & resources: `Assets/Docs/HowToSetup.pdf`; runtime-loadable assets go in `Assets/Resources`.
- Generated/editor caches (`Library`, `Temp`, `Logs`, `UserSettings`) remain untracked and unedited.

## Build, Test, and Development Commands
- Unity version: **6000.3.1f1** (`ProjectSettings/ProjectVersion.txt`). Open with this version to avoid mass reimports.
- Open editor (macOS): `/Applications/Unity/Hub/Editor/6000.3.1f1/Unity.app/Contents/MacOS/Unity -projectPath .`
- Quick play: load `Assets/Scenes/Demo.unity`, press Play. Default controls: Horizontal axes, `Space` jump, `C` dash.
- Headless PlayMode tests (when present): `/Applications/Unity/Hub/Editor/6000.3.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform playmode -logFile Logs/test.log`
- Sample batch build: `/Applications/Unity/Hub/Editor/6000.3.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget macOSUniversal -executeMethod BuildScript.Build` (change target per platform).

## Coding Style & Naming Conventions
- C#: 4-space indent, PascalCase for classes/methods, camelCase for fields/locals. One MonoBehaviour per file; filename == class.
- Inspector exposure via `[SerializeField] private`; keep API surface small.
- Asset naming: `Feature_Object_Action` (e.g., `Player_Run.anim`, `EnemyBat_Idle.controller`, `Projectile.prefab`); variant suffixes (`_Mobile`, `_v2`) instead of overwriting.
- Avoid manual edits to `.meta`/YAML; let Unity serialize. Trim trailing whitespace before commits.

## Testing Guidelines
- No automated tests exist yet. Add Play Mode under `Assets/Tests/PlayMode` and Edit Mode under `Assets/Tests/EditMode`; name test classes `<Feature>Tests`.
- For movement/combat changes, include a manual checklist in PRs: scene, controls, expected behavior (dash distance, enemy knockback, wall slide).

## Commit & Pull Request Guidelines
- Commits: short, imperative, 50–72 chars; one concern per commit (`fix dash cooldown`, `tune enemy patrol speed`).
- Avoid mixing art imports with code tweaks; separate commits ease review and reverts.
- PRs include: summary, affected assets (scenes/prefabs/scripts), screenshots or GIFs for visual/gameplay changes, test notes/commands run, linked issues if any.

## Asset & Configuration Tips
- Moving or renaming assets: commit both file and `.meta` to preserve GUID links (animations, references).
- Physics/Input tweaks: document changed `ProjectSettings` values in the PR to help reproducibility; screenshot inspector diffs when possible.
