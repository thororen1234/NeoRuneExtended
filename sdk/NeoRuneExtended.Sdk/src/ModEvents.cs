using UE.CoreUObject;
using UE.Engine;
using UE.UMG;

namespace NeoRune
{
    /// <summary>The handler of <see cref="ModEventListener.Received"/>: a method taking the message's name and value.</summary>
    [UDelegate("/Script/WebBrowserWidget.WebBrowser:OnBeforePopup__DelegateSignature")]
    public delegate void ModMessage(string name, string value);

    /// <summary>
    /// Messages between NeoRune mods: one mod sends a named message with a text value, every mod listening gets it right
    /// away (no polling).
    /// <code>
    /// // ItemStats
    /// ModEvents.Send(this, "ItemStats.Refreshed", itemId);
    ///
    /// // another mod
    /// listener = ModEvents.Listen(this);
    /// if (listener != null) listener.Received += OnModMessage;
    /// void OnModMessage(string name, string value) { if (name == "ItemStats.Refreshed") ... }
    /// </code>
    /// Prefix names with your mod's name. The value is text: send numbers with ToString and read them with int.Parse,
    /// or several values joined with a separator. A listener ends with its owner (ModActor: the level), so listen in
    /// ReceiveBeginPlay. Every mod built with NeoRune 1.0 or later can talk to every other.
    /// </summary>
    public static class ModEvents
    {
        /// <summary>Sends a message to every listening mod (including this one).</summary>
        public static void Send(UObject context, string name, string value)
        {
            var channel = Channel(context);
            if (channel != null) Raise(channel, name + "\n" + value);
        }

        /// <summary>A listener: subscribe to its Received. Null if the game isn't ready (no game instance).</summary>
        public static ModEventListener? Listen(UObject owner)
        {
            var channel = Channel(owner);
            if (channel == null) return null;
            var listener = UGameplayStatics.SpawnObject(Unreal.ClassOf<ModEventListener>(), owner) as ModEventListener;
            if (listener == null) return null;
            listener.Attach(channel);
            return listener;
        }

        /// <summary>
        /// The shared channel: the editable text in a ModEventBus widget. Every mod has its own copy of the class, so the
        /// first bus found by class name is used by all; it belongs to the game instance and lives the whole session.
        /// </summary>
        static UEditableText? Channel(UObject context)
        {
            UWidgetBlueprintLibrary.GetAllWidgetsOfClass(context, out var widgets, Unreal.ClassOf<UUserWidget>(), false);
            foreach (var widget in widgets)
                if (UKismetSystemLibrary.GetClassDisplayName(UGameplayStatics.GetObjectClass(widget)).StartsWith("ModEventBus"))
                    return GameUI.FindOfClass(widget, Unreal.ClassOf<UEditableText>()) as UEditableText;
            var instance = UGameplayStatics.GetGameInstance(context);
            if (instance == null) return null;
            var bus = UWidgetBlueprintLibrary.Create(instance, Unreal.ClassOf<ModEventBus>(), null) as ModEventBus;
            if (bus == null) return null;
            var channel = bus.Build();
            // Nothing else holds the bus: the game instance keeps it for the session.
            var kept = instance.ReferencedObjects;
            kept.Add(bus);
            instance.ReferencedObjects = kept;
            return channel;
        }

        /// <summary>Raises the channel's OnTextChanged with the message (compiler built-in: events of game classes can't be raised from C#).</summary>
        [Intrinsic("raise:OnTextChanged")]
        static void Raise(UEditableText channel, FText message) => throw null!;
    }

    /// <summary>Receives <see cref="ModEvents"/> messages: subscribe to <see cref="Received"/>.</summary>
    public class ModEventListener : UObject
    {
        public event ModMessage Received;

        /// <summary>Starts receiving the channel's messages (delegates bind methods of the object that binds them).</summary>
        public void Attach(UEditableText channel) => channel.OnTextChanged += OnText;

        public void OnText(FText text)
        {
            var message = text.ToString();
            var at = message.IndexOf("\n");
            if (at < 0) Received(message, "");
            else Received(message.Substring(0, at), message.Substring(at + 1));
        }
    }

    /// <summary>The widget holding the shared channel of <see cref="ModEvents"/> (never shown).</summary>
    public class ModEventBus : UUserWidget
    {
        public UEditableText? Build()
        {
            var tree = WidgetTree;
            if (tree == null)
            {
                tree = UGameplayStatics.SpawnObject(Unreal.ClassOf<UWidgetTree>(), this) as UWidgetTree;
                WidgetTree = tree;
            }
            if (tree == null) return null;
            var channel = UGameplayStatics.SpawnObject(Unreal.ClassOf<UEditableText>(), tree) as UEditableText;
            tree.RootWidget = channel;
            return channel;
        }
    }

    /// <summary>Other mods: whether one is installed (its ModActor class exists).</summary>
    public static class Mods
    {
        public static bool IsInstalled(string mod) => Unreal.LoadClass<AActor>("/Game/Mods/" + mod + "/ModActor.ModActor_C") != null;
    }
}
