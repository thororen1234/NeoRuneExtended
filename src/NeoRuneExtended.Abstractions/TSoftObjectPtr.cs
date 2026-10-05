namespace NeoRune;

/// <summary>A soft (path) reference to an asset of type T; load it with UKismetSystemLibrary.LoadAsset_Blocking.</summary>
public interface TSoftObjectPtr<out T> where T : class
{
}
