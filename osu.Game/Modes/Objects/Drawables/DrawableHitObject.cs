﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Game.Beatmaps.Samples;
using osu.Game.Modes.Judgements;
using Container = osu.Framework.Graphics.Containers.Container;
using osu.Game.Modes.Objects.Types;
using OpenTK.Graphics;

namespace osu.Game.Modes.Objects.Drawables
{
    public abstract class DrawableHitObject<TJudgement> : Container
        where TJudgement : Judgement
    {
        public override bool HandleInput => Interactive;

        public bool Interactive = true;

        public TJudgement Judgement;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // We may be setting a custom judgement in test cases or what not
            if (Judgement == null)
                Judgement = CreateJudgement();
        }

        protected abstract TJudgement CreateJudgement();
    }

    public abstract class DrawableHitObject<TObject, TJudgement> : DrawableHitObject<TJudgement>, IStateful<ArmedState>
        where TObject : HitObject
        where TJudgement : Judgement
    {
        public event Action<DrawableHitObject<TObject, TJudgement>> OnJudgement;

        /// <summary>
        /// The colour used for various elements of this DrawableHitObject.
        /// </summary>
        public Color4 AccentColour { get; protected set; }

        public TObject HitObject;

        private readonly List<SampleChannel> samples = new List<SampleChannel>();

        protected DrawableHitObject(TObject hitObject)
        {
            HitObject = hitObject;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            foreach (var bank in HitObject.SampleBanks)
            foreach (var sample in bank.Samples)
                samples.Add(audio.Sample.Get($@"Gameplay/{bank.Name}-hit{sample.Type.ToString().ToLower()}"));
        }

        private ArmedState state;
        public ArmedState State
        {
            get { return state; }

            set
            {
                if (state == value)
                    return;
                state = value;

                if (!IsLoaded)
                    return;

                UpdateState(state);

                if (State == ArmedState.Hit)
                    PlaySamples();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Force application of the state that was set before we loaded
            UpdateState(State);
        }

        /// <summary>
        /// Process a hit of this hitobject. Carries out judgement.
        /// </summary>
        /// <returns>Whether a hit was processed.</returns>
        protected bool UpdateJudgement(bool userTriggered)
        {
            IPartialJudgement partial = Judgement as IPartialJudgement;

            // Never re-process non-partial hits
            if (Judgement.Result != HitResult.None && partial == null)
                return false;

            // Update the judgement state
            double endTime = (HitObject as IHasEndTime)?.EndTime ?? HitObject.StartTime;
            Judgement.TimeOffset = Time.Current - endTime;

            // Update the judgement state
            bool hadResult = Judgement.Result != HitResult.None;
            CheckJudgement(userTriggered);

            // Don't process judgements with no result
            if (Judgement.Result == HitResult.None)
                return false;

            // Don't process judgements that previously had results but the results were unchanged
            if (hadResult && partial?.Changed != true)
                return false;

            switch (Judgement.Result)
            {
                default:
                    State = ArmedState.Hit;
                    break;
                case HitResult.Miss:
                    State = ArmedState.Miss;
                    break;
            }

            OnJudgement?.Invoke(this);

            if (partial != null)
                partial.Changed = false;

            return true;
        }

        protected virtual void CheckJudgement(bool userTriggered)
        {
            if (NestedHitObjects != null)
            {
                foreach (var d in NestedHitObjects)
                    d.CheckJudgement(userTriggered);
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            UpdateJudgement(false);
        }

        protected virtual void PlaySamples()
        {
            samples.ForEach(s => s?.Play());
        }

        private List<DrawableHitObject<TObject, TJudgement>> nestedHitObjects;

        protected IEnumerable<DrawableHitObject<TObject, TJudgement>> NestedHitObjects => nestedHitObjects;

        protected void AddNested(DrawableHitObject<TObject, TJudgement> h)
        {
            if (nestedHitObjects == null)
                nestedHitObjects = new List<DrawableHitObject<TObject, TJudgement>>();

            h.OnJudgement += d => OnJudgement?.Invoke(d);
            nestedHitObjects.Add(h);
        }

        protected abstract void UpdateState(ArmedState state);
    }
}
