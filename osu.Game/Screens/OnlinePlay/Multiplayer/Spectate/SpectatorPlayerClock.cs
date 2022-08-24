// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Timing;
using osu.Game.Screens.Play;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Spectate
{
    /// <summary>
    /// A clock which catches up using rate adjustment.
    /// </summary>
    public class SpectatorPlayerClock : IFrameBasedClock, IAdjustableClock
    {
        /// <summary>
        /// The catch up rate.
        /// </summary>
        public const double CATCHUP_RATE = 2;

        private readonly GameplayClockContainer masterClock;

        public double CurrentTime { get; private set; }

        /// <summary>
        /// Whether this clock is waiting on frames to continue playback.
        /// </summary>
        public Bindable<bool> WaitingOnFrames { get; } = new Bindable<bool>(true);

        /// <summary>
        /// Whether this clock is behind the master clock and running at a higher rate to catch up to it.
        /// </summary>
        /// <remarks>
        /// Of note, this will be false if this clock is *ahead* of the master clock.
        /// </remarks>
        public bool IsCatchingUp { get; set; }

        /// <summary>
        /// Whether this spectator clock should be running.
        /// Use instead of <see cref="Start"/> / <see cref="Stop"/> to control time.
        /// </summary>
        public bool IsRunning { get; set; }

        public SpectatorPlayerClock(GameplayClockContainer masterClock)
        {
            this.masterClock = masterClock;
        }

        public void Reset() => CurrentTime = 0;

        public void Start()
        {
            // Our running state should only be managed by SpectatorSyncManager via IsRunning.
        }

        public void Stop()
        {
            // Our running state should only be managed by an SpectatorSyncManager via IsRunning.
        }

        public bool Seek(double position)
        {
            CurrentTime = position;
            return true;
        }

        public void ResetSpeedAdjustments()
        {
        }

        public double Rate => IsCatchingUp ? CATCHUP_RATE : 1;

        double IAdjustableClock.Rate
        {
            get => Rate;
            set => throw new NotSupportedException();
        }

        double IClock.Rate => Rate;

        public void ProcessFrame()
        {
            ElapsedFrameTime = 0;
            FramesPerSecond = 0;

            masterClock.ProcessFrame();

            if (IsRunning)
            {
                double elapsedSource = masterClock.ElapsedFrameTime;
                double elapsed = elapsedSource * Rate;

                CurrentTime += elapsed;
                ElapsedFrameTime = elapsed;
                FramesPerSecond = masterClock.FramesPerSecond;
            }
        }

        public double ElapsedFrameTime { get; private set; }

        public double FramesPerSecond { get; private set; }

        public FrameTimeInfo TimeInfo => new FrameTimeInfo { Elapsed = ElapsedFrameTime, Current = CurrentTime };
    }
}