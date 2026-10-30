# Asuna player trial

Source: MSGDI / Markus Schüler, **Free Test Character Asuna**, version 2.2.

- Official Unity listing: https://assetstore.unity.com/packages/3d/characters/humanoids/sci-fi/free-test-character-asuna-205897
- Creator listing: https://www.cgtrader.com/free-3d-models/character/sci-fi-character/free-test-character-asuna
- The user downloaded `FreeTestCharacterAsuna+URP+HDRP.unitypackage` here. Keep that original archive locally; it is ignored by Git.
- The Unity listing specifies the Standard Unity Asset Store EULA; the CGTrader listing specifies its Royalty Free License (no AI). This is licensed third-party content, not CC0. Preserve the license applicable to the actual download and the original archive; do not redistribute the standalone asset pack. `receipt.json` records the supplied archive's hash.

## Integration

The downloaded package, `Assets/FreeTestCharacterAsuna/`, and generated Asuna meshes/clips/prefabs are ignored by Git because this repository is public. A clean checkout uses the original operative. Importing the licensed content activates Asuna locally in both the player and character screen; it does not change saves, collision, or equipment stats. The Asuna-specific test reports an explicit skip when this optional content is absent; the regular gameplay tests still run against the original operative.

To reproduce the local trial, obtain the package from the creator, close Unity, and run from the project directory:

```powershell
python Tools/Art/import_asuna.py 'Art/Asuna/FreeTestCharacterAsuna+URP+HDRP.unitypackage'
```

Then run the Unity editor with this project and execute `VideoGame.Editor.AsunaAssets.Build` (or use **Nexus > Build Asuna trial character** in the editor). For batch mode, pass `-batchmode -nographics -quit -projectPath <project> -executeMethod VideoGame.Editor.AsunaAssets.Build -logFile <log>` to the installed Unity executable. Allow the editor to finish and check its exit code before starting tests or a player build.

The importer selects the official FBX, texture, material, prefab and animation files. Embedded source archives, the nested HDRP package, and demo scenes are not imported. No publisher scripts are executed. Package paths are constrained to the character's asset folder.

`VideoGame.Editor.AsunaAssets.Build` creates the trial prefab, native in-place animation clips, weapon clips, fitted helmet and rifle/pistol prefabs. It converts the supplied URP materials to the project's Standard shader, maps the humanoid bones to the equipment system's canonical names, preserves skin bindings, and adds an intermediate spine transform for existing combat layers. Native clip poses are sampled through Unity's humanoid animation graph and baked to ordinary transform clips; gameplay still owns movement and damage. Curve reduction uses a 0.0001-unit component error threshold.

The trial retains the existing character collision and player stats. It uses Asuna's native movement, hit, death and melee animations, the existing guard/recoil presentation layers baked for this skeleton, and the package's detailed handgun/rifle. Native melee is retimed so the strongest turn and weight transfer coincide with the existing 0.22-second hit frame. Finger-only grip clips preserve the package's authored rifle, pistol and melee hand poses without overwriting arm aim or the opposite hand. The closed helmet is fitted geometry from the existing approved operative; swords/shields retain their current meshes.

This is a character trial, not a new collection of armor, helmet or backpack designs. Body armor uses the supplied attached armor pieces. Further wearable variants and animation polish remain separate work.

## Review

`AsunaAssets.BuildAndRender` rebuilds and writes a studio review image to `artifacts/asuna/character-review.png`. The live player and inventory preview both select `PlayerRig.PrefabPath`. The prior operative resources remain available for comparison.

Validated locally on 2026-10-30 with Unity 6000.3.26f1: 17,420 core assertions, 19 EditMode and 86 PlayMode tests passed (complete suite: 110.0 seconds). Three focused rendered loadout/combat tests passed using Direct3D 11. Windows build succeeded in 23.9 seconds, followed by the eight-second player smoke and a 15-second rendered player capture (23 images), without logged runtime errors. Direct3D 12's broader rendered editor test run encountered a device/resource-creation failure in reflection-probe rendering; the trial game is launched with `-force-d3d11`. No remote CI run or push was performed for this local trial.
