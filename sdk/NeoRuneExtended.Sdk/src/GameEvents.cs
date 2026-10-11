using UE.CoreUObject;
using UE.GameplayMessageRuntime;

namespace NeoRune
{
    /// <summary>
    /// The game's own events: it broadcasts what happens (an item picked up, a level up, a status applied...) as messages on
    /// channels (gameplay tags, see <see cref="GameChannels"/>), each with a struct payload. Listen to one instead of checking
    /// the game state on a timer:
    /// <code>
    /// var equips = GameEvents.Listen(this, GameChannels.ItemEquipped, GameEvents.StructOf&lt;FPlayerItemEntryPayload&gt;(), false);
    /// if (equips != null) equips.OnMessageReceived += OnEquip;
    ///
    /// void OnEquip(UAsyncAction_ListenForGameplayMessage? message, FGameplayTag channel)
    /// {
    ///     if (GameEvents.Read(message, out FPlayerItemEntryPayload equip)) Log.Write($"{equip.Player} equipped an item");
    /// }
    /// </code>
    /// A listener lasts until the level ends or <see cref="Stop"/>. <see cref="LogAll"/> logs every message the game sends,
    /// with its channel and payload type, to find the ones you need.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>The Unreal struct type of a payload struct, for <see cref="Listen"/>.</summary>
        [Intrinsic("struct-of")] public static UScriptStruct StructOf<T>() where T : struct => throw null!;

        /// <summary>
        /// The payload of a received message, in its struct type. Only works on a listener made with that same payload type
        /// (the game checks it): false otherwise, and always false on a listener with no payload type.
        /// </summary>
        [Intrinsic("message-payload")] public static bool Read<T>(UAsyncAction_ListenForGameplayMessage? message, out T payload) where T : struct => throw null!;

        /// <summary>
        /// Listens to a channel: <see cref="UAsyncAction_ListenForGameplayMessage.OnMessageReceived"/> fires for each message
        /// with that payload type. <paramref name="payloadType"/> null gets every message whatever its type, but
        /// then <see cref="Read"/> can't get the payload;
        /// <paramref name="includeSubChannels"/> also gets the channels under it ("SW.GMR.Inventory" gets
        /// "SW.GMR.Inventory.Changed"...).
        /// </summary>
        public static UAsyncAction_ListenForGameplayMessage? Listen(UObject context, string channel, UScriptStruct? payloadType, bool includeSubChannels)
        {
            var listener = UAsyncAction_ListenForGameplayMessage.ListenForGameplayMessages(context, GameUI.Tag(channel), payloadType,
                includeSubChannels ? EGameplayMessageMatch.PartialMatch : EGameplayMessageMatch.ExactMatch);
            listener?.Activate();
            return listener;
        }

        /// <summary>Stops a listener.</summary>
        public static void Stop(UAsyncAction_ListenForGameplayMessage? listener) => listener?.Cancel();

        /// <summary>Logs every message the game sends from now on (its channel), until the level ends. For finding channels.</summary>
        public static GameEventLog? LogAll(UObject context)
        {
            var log = World.Spawn(context, Unreal.ClassOf<GameEventLog>(), new FVector()) as GameEventLog;
            log?.Start();
            return log;
        }
    }

    /// <summary>Made by <see cref="GameEvents.LogAll"/>: logs the channel of every message the game sends.</summary>
    public class GameEventLog : UE.Engine.AActor
    {
        UAsyncAction_ListenForGameplayMessage? listener;

        public void Start()
        {
            listener = GameEvents.Listen(this, "SW.GMR", null, true);
            if (listener != null) listener.OnMessageReceived += OnMessage;
        }

        void OnMessage(UAsyncAction_ListenForGameplayMessage? message, UE.GameplayTags.FGameplayTag channel) => Log.Write($"message {channel.TagName}");
    }
}
