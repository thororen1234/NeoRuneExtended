using System.Collections;
using UE.CoreUObject;
using UE.Engine;

namespace NeoRune
{
    /// <summary>
    /// Coroutines: step-by-step code without chains of timer callbacks. A coroutine is a method of your class returning
    /// <see cref="IEnumerator"/>; each <c>yield return</c> pauses it.
    /// <code>
    /// IEnumerator Countdown(int from)
    /// {
    ///     for (int i = from; i > 0; i--)
    ///     {
    ///         Log.Write($"{i}...");
    ///         yield return Coroutine.Wait(1f);   // one second of game time
    ///     }
    ///     Log.Write("Go!");
    /// }
    ///
    /// Coroutine.Start(Countdown(3));
    /// </code>
    /// <c>yield return null</c> waits for the next frame and <c>yield break</c> ends the coroutine. Waiting uses the game's
    /// timers, so nothing runs while a coroutine waits (and game time stops while the game is paused). Starting a
    /// coroutine that is already running restarts it. Its locals are fields of the class behind the scenes, so each
    /// coroutine runs once at a time per object.
    /// </summary>
    public static class Coroutine
    {
        /// <summary>Runs the coroutine's first step now (until its first yield). Restarts it if it was running.</summary>
        [Intrinsic("co-start")]
        public static void Start(IEnumerator routine) => throw null!;

        /// <summary>Stops the coroutine (the arguments in <c>Stop(Countdown(0))</c> are ignored).</summary>
        [Intrinsic("co-stop")]
        public static void Stop(IEnumerator routine) => throw null!;

        /// <summary>Whether the coroutine is running (waiting counts as running).</summary>
        [Intrinsic("co-running")]
        public static bool IsRunning(IEnumerator routine) => throw null!;

        /// <summary>Use as <c>yield return Coroutine.Wait(seconds)</c>: continues after that much game time.</summary>
        [Intrinsic("co-wait")]
        public static object? Wait(float seconds) => throw null!;

        /// <summary>Used by the compiled coroutines: calls the coroutine's function again after seconds (0: the next frame).</summary>
        public static void ResumeAfter(UObject owner, string functionName, float seconds)
        {
            if (seconds > 0) UKismetSystemLibrary.K2_SetTimer(owner, functionName, seconds, false, false, 0f, 0f);
            else UKismetSystemLibrary.K2_SetTimerForNextTick(owner, functionName);
        }

        /// <summary>Used by the compiled coroutines: cancels a pending step.</summary>
        public static void Cancel(UObject owner, string functionName) => UKismetSystemLibrary.K2_ClearTimer(owner, functionName);
    }
}
