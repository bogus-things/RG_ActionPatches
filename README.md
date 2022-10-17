# RG_ActionPatches
A collection of Harmony patches to unlock &amp; add restricted or missing character actions

## Features
- Adds "Talk to someone" action for characters outside their workplaces (i.e. visitng the casino or clinic)
- Adds all currently available actors in a scene to the target list for "talk to someone", allowing characters to talk to non-coworkers
- Adds support for "Talk to someone" at date spots (cafe, casino, park), allowing single visitors to talk to each other and single visitors to steal seats from characters in the bathroom

## Requirements
This plugin was developed exclusively using the BetterRepack repacks. Compatibility/support is not guaranteed for other types of game installations.

## Installation
1. Download the plugin from [Releases](https://github.com/bogus-things/RG_ActionPatches/releases) (Check the "Compatibility" section to ensure the plugin will work for you)
2. Extract the `BepInEx` folder from the `.zip` and place it in your game's root directory

## Reporting an issue
If you believe you've found a bug with RG_ActionPatches, please use the following process to let me know!
1. Do your best to ensure the bug is with RG_ActionPatches
    1. Check the log for errors referencing this specific plugin
    2. If the logs show errors for other plugins, disable those with KKManager and try again
    3. Disable RG_ActionPatches with KKManager and see if the bug persists
2. Check [Issues](https://github.com/bogus-things/RG_ActionPatches/issues) for any open issues (notably the pinned issues at the top)
    1. If you see an open issue for a bug matching the behavior you're seeing, please add a comment there instead of creating a new issue. And when adding a new comment, check the top post to confirm what information you should provide
    2. If you don't, feel free to create a new issue describing the bug you've found
3. When creating a new issue, please provide the following:
    1. A description of the behavior
    2. Your current BetterRepack version and your current RG_ActionPatches version
    3. If you're able to reproduce the bug consistently, provide the steps you take to do so
    4. If there is an error in the logs, provide your game logs as an attached `.txt` file (please don't copy/paste it into the issue description)
  
  ## Contributing
  If you'd like to contribute to feature development or bug fixing, pull requests are welcome! For convenience in getting set up, here's a table mapping out the project references (paths are relative to your game root directory):
| Reference                | Path                                             |
|--------------------------|--------------------------------------------------|
| `0Harmony`               |  `BepInEx\core\0Harmony.dll`                     |
| `Assembly-Csharp`        |  `BepInEx\unhollowed\Assembly-CSharp.dll`        |
| `BepInEx.Core`           |  `BepInEx\core\BepInEx.Core.dlll`                |
| `BepInEx.IL2CPP`         |  `BepInEx\core\BepInEx.IL2CPP.dll`               |
| `IL`                     |  `BepInEx\unhollowed\IL.dll`                     |
| `Il2Cppmscorlib`         |  `BepInEx\unhollowed\Il2Cppmscorlib.dll`         |
| `Il2CppSystem`           |  `BepInEx\unhollowed\Il2CppSystem.dll`           |
| `UnhollowerBaseLib`      |  `BepInEx\core\UnhollowerBaseLib.dll`            |
| `UnityEngine.CoreModule` |  `BepInEx\unhollowed\UnityEngine.CoreModule.dll` |  
