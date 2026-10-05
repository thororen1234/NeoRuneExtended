using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>Game-time timers that call a method of the mod by name (use nameof). They stop while the game is paused.</summary>
    public static class Timer
    {
        /// <summary>Calls owner.functionName after seconds (repeatedly when loop is true).</summary>
        public static FTimerHandle Start(UObject owner, string functionName, float seconds, bool loop) =>
            UKismetSystemLibrary.K2_SetTimer(owner, functionName, seconds, loop, false, 0f, 0f);

        public static void Stop(UObject owner, string functionName) => UKismetSystemLibrary.K2_ClearTimer(owner, functionName);
    }

    /// <summary>The handler of <see cref="PausableTimer.Fired"/>: a method without parameters.</summary>
    [UDelegate("/Script/UMG.OnButtonClickedEvent__DelegateSignature")]
    public delegate void TimerEvent();

    /// <summary>
    /// A timer that keeps running while the game is paused (the game's timers, and so <see cref="Timer"/>, stop then, e.g.
    /// under pause mods). It counts real time.
    /// <code>
    /// poll = PausableTimer.Start(this, 0.25f, loop: true);
    /// poll.Fired += Poll;
    /// </code>
    /// It's an actor in the level: it ends with the level, or call <see cref="Stop"/>.
    /// </summary>
    public class PausableTimer : AActor
    {
        public event TimerEvent Fired;
        float interval;
        bool loop;
        double next;

        public static PausableTimer? Start(UObject context, float seconds, bool loop)
        {
            var timer = World.Spawn(context, Unreal.ClassOf<PausableTimer>(), new FVector()) as PausableTimer;
            if (timer == null) return null;
            timer.interval = seconds;
            timer.loop = loop;
            timer.next = World.RealTime(timer) + seconds;
            return timer;
        }

        /// <summary>Stops the timer and removes it.</summary>
        public void Stop() => K2_DestroyActor();

        protected override void ReceiveBeginPlay() => SetTickableWhenPaused(true);

        public override void ReceiveTick(float deltaSeconds)
        {
            var now = World.RealTime(this);
            if (interval <= 0 || now < next) return;
            // After a long hitch, fire once and continue from now instead of catching up with a burst.
            next = next + interval > now ? next + interval : now + interval;
            Fired();
            if (!loop) Stop();
        }
    }
}
