namespace NeoRune;

/// <summary>
/// A class reference restricted to T or its subclasses (TSubclassOf&lt;T&gt;). Covariant like in
/// Unreal: a TSubclassOf&lt;ADerived&gt; can be passed where TSubclassOf&lt;ABase&gt; is expected. Use null for "no class".
/// </summary>
public interface TSubclassOf<out T> where T : class
{
}
