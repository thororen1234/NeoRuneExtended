# NeoRuneExtended

**Write Minecraft Dungeons II mods in C#, using the game's own UI.** NeoRuneExtended compiles C# into Blueprint bytecode and packs it as an ordinary Blueprint mod, loaded by [BetterBlueprintLoader](https://github.com/thororen1234/MCDII-JustKeepRollin/tree/main/BetterBlueprintLoader) or [Blueprint Loader](https://www.nexusmods.com/minecraftdungeons2/mods/2). You only need the .NET SDK: no Unreal Engine and no injection.

It's a fork of [NeoRune](https://www.nuget.org/packages/NeoRune.Sdk). Everything NeoRune does works the same way, and mods built with it compile to identical packages. NeoRuneExtended adds the things NeoRune can't do:

- **Add to the game's screens.** Put your widgets into the game's own UI slots (HUD, quest tracker, minimap, toasts, the system menu) or into a named panel of any screen (inventory, collectibles, settings).
- **Real game menus.** Push your menu onto the game's menu layers, so Esc/B closes it, the game's input routing applies and its fades cover it.
- **Call the game's AngelScript functions.** NeoRune can't call them (it crashes the game); NeoRuneExtended calls them the way the game itself does.

```csharp
using NeoRune;
using UE.Engine;

public class ModActor : AActor
{
    protected override void ReceiveBeginPlay()
    {
        Log.Write($"Hello from {World.LevelName(this)}");
    }
}
```

```
dotnet new neorunex-mod -n MyMod
cd MyMod
dotnet build
```

## Moving a mod from NeoRune

Change the first line of the `.csproj`:

```xml
<Project Sdk="NeoRuneExtended.Sdk/0.4.2">
```

The `NeoRune` namespace (`Log`, `Timer`, `World`, `Unreal`...), the `UE.*` game API and the `NeoRune*` MSBuild properties are unchanged.

## Mod info and settings (Blueprint Loader 2.0)

Each mod gets a page in the Mods tab of the game's settings. The title, version, author and description come from the project (`NeoRuneModTitle`, `Version`, `NeoRuneModAuthor`, `NeoRuneModAuthorUrl`, `NeoRuneModDescription`), and the settings come from attributes on `ModActor`:

```csharp
[ModSetting.Toggle("enabled", "Enabled", Default = true)]
[ModSetting.Slider("speed", "Speed", Min = 0, Max = 2, Default = 1)]
[ModSetting.Keybind("toggle", "Toggle", Default = "F8")]
public class ModActor : AActor, IModSettings
{
    bool enabled = true;

    public void OnSettingChanged(string id, string value)
    {
        if (id == "enabled") enabled = ModSettings.ToBool(value);
    }
}
```

There are also `Heading`, `Text`, `Spacer`, `Select`, `TextInput`, `Colour`, `UrlButton`, `EventButton` and `Widget` (your own `UUserWidget` on the page). `ModSettings.Get`/`Set` read and save values through Blueprint Loader; `Settings` keeps values in a save game and works without it.

## Building NeoRuneExtended

```
powershell -ExecutionPolicy Bypass -File eng/build.ps1 [-Version 0.4.2]
```

writes `NeoRuneExtended.Sdk`, `NeoRuneExtended.Tool` and `NeoRuneExtended.Templates` to `artifacts/packages`. To use them before they're on NuGet, add that folder as a package source next to your mods:

```xml
<!-- nuget.config -->
<configuration>
  <packageSources>
    <add key="NeoRuneExtended" value="C:\path\to\NeoRuneExtended\artifacts\packages" />
  </packageSources>
</configuration>
```

## Credits

- [NeoRune](https://www.nuget.org/packages/NeoRune.Sdk) by NeoMakesGames / NeoPlayzGames (MIT), which this is based on.
- [UAssetAPI](https://github.com/atenfyr/UAssetAPI) (MIT), [retoc](https://github.com/trumank/retoc) and [repak](https://github.com/trumank/repak) (MIT/Apache-2.0), [Roslyn](https://github.com/dotnet/roslyn).
