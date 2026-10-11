using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>
    /// The mod's own pictures. Put PNG or JPG files in an <c>Images</c> folder next to the .csproj: the build packs them into
    /// the mod, and <see cref="Load"/> turns one into a texture for <see cref="Ui.Image"/> or <c>UImage.SetBrushFromTexture</c>.
    /// <code>
    /// var icon = Images.Load(this, "icon.png");
    /// </code>
    /// Loading decodes the file, so load each picture once and keep the texture in a field.
    /// </summary>
    public static class Images
    {
        /// <summary>A picture from the mod's Images folder (e.g. "icon.png", "icons/sword.png"), or null if it isn't there.</summary>
        public static UTexture2D? Load(UObject context, string file) =>
            UKismetRenderingLibrary.ImportFileAsTexture2D(context, $"{UKismetSystemLibrary.GetProjectContentDirectory()}Mods/{Unreal.ModName}/Images/{file}");
    }
}
