# NeoRuneExtended

**Write Minecraft Dungeons II mods in C#, using the game's own UI.** NeoRuneExtended compiles C# into Blueprint bytecode and packs it as an ordinary Blueprint mod, loaded by [BetterBlueprintLoader](https://github.com/thororen1234/MCDII-JustKeepRollin/tree/main/BetterBlueprintLoader) or [Blueprint Loader](https://www.nexusmods.com/minecraftdungeons2/mods/2). You only need the .NET SDK: no Unreal Engine and no injection.

It's a fork of [NeoRune](https://www.nuget.org/packages/NeoRune.Sdk) 1.0. Everything NeoRune does works the same way (the game's UI slots and menus, AngelScript calls, coroutines, live tuning, `neorunex pack`/`update`/`crash`...), and mods built with it compile to the same packages. NeoRuneExtended adds:

- **Assets at fixed paths.** `[Asset("/Game/...")]` puts a class, a Blueprint interface, a user struct (with fixed field names and GUID) or a user enum at a path of your choosing, so a mod can provide assets other mods and Blueprints load by path, e.g. a stand-in for Blueprint Loader's.
- **Blueprint function libraries.** Public static methods of a `UBlueprintFunctionLibrary` class become static Blueprint functions other mods can call.
- **Fixes:** casting to or testing for a Blueprint interface (`x is IModSettings`) no longer corrupts the object (NeoRune's cast writes 16 bytes into an 8-byte variable), a field or local called `none` no longer becomes the empty name, and `base.X()` inside an override calls the base's code instead of the override again.
- **BetterBlueprintLoader** is recognised as a mod loader by `neorunex doctor`.

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
<Project Sdk="NeoRuneExtended.Sdk/1.0.0">
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
powershell -ExecutionPolicy Bypass -File eng/build.ps1 [-Version 1.0.0]
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
