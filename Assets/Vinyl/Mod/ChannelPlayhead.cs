// MIT License
// 
// Copyright (c) 2021 Pixel Precision, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vinyl.Mod
{
    /// <summary>
    /// The playing context for each channel of a mod song being played.
    /// Behaviour spect at http://www.aes.id.au/modformat.html
    /// </summary>
    /// <remarks>See ModplayCtx for more details - this class is only meant to 
    /// be contained inside of ModPlayCtx.</remarks>
    public class ChannelPlayhead
    {
        public enum EffectWaveform
        {
            /// <summary>
            /// A sine wave.
            /// </summary>
            Sine,
            /// <summary>
            /// An LFO wave of a ramp down (backslash-like)
            /// </summary>
            RampDown,
            /// <summary>
            /// A square wave.
            /// </summary>
            Square,

            /// <summary>
            /// Randomly chosen per-division.
            /// </summary>
            Random
        }

        /// <summary>
        /// Curve the volume for compression if desired. Value should be in the range of (0.0, 1.0],
        /// with a value of 1.0 representing no curve and using linear volume.
        /// </summary>
        public const float VolCurve = 1.0f;

        /// <summary>
        /// The player context we're a part of. This is used to get current
        /// playback information and access to song data.
        /// </summary>
        public readonly ModPlayCtx ctx;

        /// <summary>
        /// Self-identification for what channel the ChannelPlayhead is 
        /// in charge on.
        /// </summary>
        public readonly int channel;

        /// <summary>
        /// The current format volume. This is not used directly, but used to
        /// generate vol.
        /// </summary>
        /// <remarks>If this value is updated, UpdateVolume() should be called to 
        /// recalculate the final volume.</remarks>
        public int vol64 {get; protected set; } = 64;

        /// <summary>
        /// The actual used volume in floating point [0.0, 1.0] range.
        /// </summary>
        public float vol {get; protected set; } = 1.0f;

        /// <summary>
        /// Reference to the song being played.
        /// </summary>
        ModFile song { get => this.ctx.song; }

        /// <summary>
        /// The channel data in the song for our channel that we're currently processing.
        /// </summary>
        public ModFile.Channel channelData {get; protected set; } = null;

        /// <summary>
        /// The sample that's currently being played, or the last played sample that's 
        /// being remembered for a possible later operation.
        /// </summary>
        public ModFile.Sample curSample {get; protected set; } = null;

        /// <summary>
        /// If true, curSample is currently being written out into the audio buffer. If
        /// false, no audio is being streamed out. Either curSample is invalid, or it
        /// has already finished playing and doesn't loop, or it's being suppressed by
        /// an effect.
        /// </summary>
        public bool streamingSample {get; protected set; } = false;

        /// <summary>
        /// The current PCM sample being written from our audio sample (curSample). This
        /// is a double precision because we need fractional values for arbitrary movement
        /// distances and for lerping - and we need to do it with high precision.
        /// </summary>
        double sampleTime = 0;

        /// <summary>
        /// The value to increment sampleTime every time a PCM value is written.
        /// </summary>
        double sampleIncr = 0.0;

        /// <summary>
        /// When we're at the end of the sample and ready to check for looping.
        /// 
        /// The first time we play, the same plays to the end, and then from there if
        /// we're looping, we only play back to the sample's loopback + loopend. 
        /// Usually it will be authored to be the as as the sample length, but that's
        /// not guaranteed.
        /// </summary>
        int sampleEnd = 0;

        /// <summary>
        /// The current period we're playing samples in.
        /// </summary>
        public int period {get; protected set; } = 0;

        /// <summary>
        /// The ideal period we want to be playing. Usually period and targetPeriod will
        /// be the same, but may differ, such as during a note slide, when the period 
        /// incrementally moves to period over time.
        /// </summary>
        int targetPeriod = 0;

        /// <summary>
        /// When doing a note slide, at what amount does period add/subtract to move
        /// toward targetPeriod every tick?
        /// </summary>
        int periodSlide = 0;

        /// <summary>
        /// If we're in the middle of a fine tune effect, what's the converted pitch shift
        /// factor?
        /// </summary>
        double finetune = 1.0;

        /// <summary>
        /// If the same sample has been playing repeatedly (on the same channel), how many
        /// times has it consecutively repeated?
        /// </summary>
        int divHolds = 0;

        /// <summary>
        /// The current effect being processed.
        /// </summary>
        Effect lastEffect = Effect.None;

        /// <summary>
        /// The parameter for the effect.
        /// </summary>
        /// <remarks>The original parameter was represented as a nibble, but 
        /// there's no good motivation for us to not expand it into a more
        /// effective data type.</remarks>
        int effectParam1 = 0;

        /// <summary>
        /// The other parameter for the effect.
        /// </summary>
        /// <remarks>The original parameter was represented as a nibble, but 
        /// there's no good motivation for us to not expand it into a more
        /// effective data type.</remarks>
        int effectParam2 = 0;

        /// <summary>
        /// If false, when streaming data we can attempt to stream the entire
        /// division PCM uninterrupted. If true, we need to stop streaming and
        /// check our effects between ticks.
        /// </summary>
        bool perTickEffect = false;

        /// <summary>
        /// If processing a tick effect, this is the last tick we remember
        /// processing for.
        /// </summary>
        int curTick = 0;

        // UNIMPLEMENTED
        bool glissando = false;

        // Vibrato state information
        EffectWaveform vibratoForm = EffectWaveform.Sine;
        EffectWaveform vibratoUsed = EffectWaveform.Sine;
        bool vibratoRetrigger = true;
        double vibratoPos = 0.0;
        double vibratoIncr = 0.0;
        double vibratoSemi = 0.0;

        // Tremolo state information
        EffectWaveform tremoloForm = EffectWaveform.Sine;
        EffectWaveform tremoloUsed = EffectWaveform.Sine;
        bool tremoloRetrigger = true;
        double tremoloPos = 0.0;
        double tremoloIncr = 0.0;
        double tremoloAmp = 0.0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ctx">The parent song context.</param>
        /// <param name="channel">The channel the object is in charge of.</param>
        public ChannelPlayhead(ModPlayCtx ctx, int channel)
        {
            this.channel = channel;
            this.ctx = ctx;
        }

        /// <summary>
        /// Reset state variables to how they were when we first started streaming.
        /// This is to rectify the state when forcing a restart from the very beginning
        /// of the song.
        /// </summary>
        public void Reset()
        {
            this.period = 0;
            this.targetPeriod = 0;

            this.finetune = 1;
            this.divHolds = 0;

            this.lastEffect = Effect.None;
            this.effectParam1 = 0;
            this.effectParam2 = 0;

            this.perTickEffect = false;
            this.curTick = 0;

            this.glissando = false;

            this.vibratoForm = EffectWaveform.Sine;
            this.vibratoUsed = EffectWaveform.Sine;
            this.vibratoRetrigger = true;
            this.vibratoPos = 0.0;
            this.vibratoIncr = 0.0;
            this.vibratoSemi = 0.0;

            this.tremoloForm = EffectWaveform.Sine;
            this.tremoloUsed = EffectWaveform.Sine;
            this.tremoloRetrigger = true;
            this.tremoloPos = 0.0;
            this.tremoloIncr = 0.0;
            this.tremoloAmp = 0.0;
        }

        /// <summary>
        /// Called from the player context when the player
        /// has moved into a new sequence.
        /// </summary>
        /// <param name="sequence">The new sequency the song has entered into.</param>
        public void OnChangeSequence(int sequence)
        {
            int pattern = this.song.sequences[sequence];
            this.channelData = this.song.patternData[pattern].channels[this.channel];
        }

        /// <summary>
        /// Recalculate the volume if any changs were made to the values that
        /// the volume is dependent on.
        /// </summary>
        void UpdateVolume()
        {
            this.vol = Mathf.Pow(this.vol64 / 64.0f, VolCurve);

            if(this.lastEffect == Effect.InvertLoop)
                this.vol = -this.vol;
        }

        /// <summary>
        /// Recalculate the speed at which the player moves through a sample if
        /// there were changes made to values that it depends on.
        /// </summary>
        void UpdateSamplePlayIncrease()
        {
            if (this.period == 0)
            {
                this.sampleIncr = 0.0f;
                return;
            }

            double sampleft = (this.curSample != null) ? this.curSample.finetune : 1.0;

            // We syncronize our audio to our samplerate to the documentation
            // that the samples stream at the note C2 at an specific expected
            // samplerate and frequency.
            this.sampleIncr =
                (double)ModFile.Sample.C2SampleRate / this.ctx.sampleRate *     // Remap the sample rate
                (double)ModFile.Sample.C2Period / this.period *                 // Adjust for frequency difference
                sampleft;

            this.sampleIncr *= this.finetune;

            if (this.lastEffect == Effect.Arpegio)
            {
                int mod = this.divHolds % 3;
                if (mod == 0) { }
                else if (mod == 1)
                    this.sampleIncr *= System.Math.Pow(2.0, this.effectParam1 / 12.0);
                else
                    this.sampleIncr *= System.Math.Pow(2.0, this.effectParam2 / 12.0);
            }
            else if (this.lastEffect == Effect.InvertLoop)
            {
                if (this.effectParam2 == 0)
                    this.streamingSample = false;
                else
                    this.sampleIncr *= this.effectParam2;
            }
        }

        /// <summary>
        /// Notifcation when a division is entered into - used to ready the
        /// state of the object and cache values for streaming.
        /// </summary>
        /// <param name="sequence">The sequence the division belong to.</param>
        /// <param name="div">The div about to be streamed.</param>
        /// <returns>A message to the managing player context. If there is nothing
        /// significant to notify the manager context, send back Continue.</returns>
        public NoteChangeEffect OnChangeDiv(int sequence, int div)
        {
            NoteChangeEffect ret = NoteChangeEffect.Continue();
            if (this.channelData == null)
                return ret;

            ModFile.Div n = this.channelData.divs[div];

            bool setPeriodToTarget = true;

            // If we were recently inverted, chances are we're not anymore.
            bool shouldResetVolume = (this.lastEffect == Effect.InvertLoop);
            bool shouldUpdateVolume = false;
            bool resetVib = true;
            bool resetTrem = true;

            this.curTick = 0;

            if (n.sampleIdx == -1)
            {
                ++this.divHolds;
            }
            else if (
                n.sampleIdx >= 0 &&
                n.sampleIdx < this.song.samples.Count)
            {
                this.curSample = this.song.samples[n.sampleIdx];
                this.streamingSample = this.curSample.length > 0;
                this.sampleTime = 0.0;
                this.sampleEnd = this.curSample.length;

                this.targetPeriod = n.period;

                this.divHolds = 0;
                this.finetune = 1.0;

                shouldResetVolume = true;
            }
            else
            {
                this.curSample = null;
                this.streamingSample = false;
                this.sampleIncr = 0.0;
            }

            if (n.effectCommand != 0)
            {
                int cmd = (n.effectCommand & ModFile.exHiNibble) >> 8;
                int mn = (n.effectCommand & ModFile.hiNibble) >> 4;
                int ln = n.effectCommand & ModFile.loNibble;

                if (cmd == 14)
                    cmd = (cmd << 4) | mn;

                this.lastEffect = (Effect)cmd;
                this.effectParam1 = mn;
                this.effectParam2 = ln;
                switch ((Effect)cmd)
                {
                    case Effect.Arpegio:
                        break;

                    case Effect.SlideUp:
                        this.perTickEffect = true;
                        break;

                    case Effect.SlideDown:
                        this.perTickEffect = true;
                        break;

                    case Effect.SlideNote:
                        this.perTickEffect = true;
                        setPeriodToTarget = false;

                        // We store this because we may need it afterwards if 
                        // a continue slide effect is used.
                        this.periodSlide = this.effectParam1 * 16 + this.effectParam2;
                        break;

                    case Effect.Vibrato:
                        if (this.effectParam1 != 0)
                            this.vibratoIncr = (this.effectParam1 * this.ctx.ticksPerDiv) / 64.0 / this.ctx.samplesPerDiv;

                        if (this.effectParam2 != 0)
                            this.vibratoSemi = this.effectParam2 / 16.0;

                        if (this.vibratoSemi == 0.0 || this.vibratoSemi == 0.0)
                            this.lastEffect = Effect.None;
                        else if (this.vibratoRetrigger == false)
                            resetVib = false;

                        this.vibratoUsed = ProcessWaveform(this.vibratoUsed);

                        break;

                    case Effect.SlideCont:
                        this.perTickEffect = true;
                        break;

                    case Effect.VibratoCont:
                        this.perTickEffect = true;
                        break;

                    case Effect.Tremolo:
                        if (this.effectParam1 != 0)
                            this.tremoloIncr = (this.effectParam1 * this.curTick) / 64.0 / this.ctx.samplesPerDiv;

                        if (this.effectParam2 != 0)
                            this.tremoloAmp = this.effectParam1 * (this.ctx.ticksPerDiv - 1) / 64.0;

                        if (this.vibratoSemi == 0.0 || this.vibratoSemi == 0.0)
                            this.lastEffect = Effect.None;
                        else if (this.tremoloRetrigger == false)
                            resetTrem = false;

                        this.tremoloUsed = ProcessWaveform(this.tremoloForm);
                        break;

                    case Effect.SetPan:
                        // Are you kidding me!? We're only supporting mono - We got enough going on as it is!
                        break;

                    case Effect.SetSampleOffset:
                        if (this.curSample != null && this.curSample.pcm != null)
                        {
                            int offset = this.effectParam1 * 4096 + this.effectParam2 * 256;
                            if (this.curSample != null && this.curSample.length > 0)
                            {
                                if (offset < this.curSample.length)
                                {
                                    this.sampleTime = offset;
                                    this.streamingSample = true;
                                }
                                else if (this.curSample.loopLength > 0)
                                {
                                    this.sampleTime -= this.curSample.length;
                                    this.sampleTime = this.curSample.loopBack + this.curSample.loopLength % this.sampleTime;
                                    this.streamingSample = true;
                                }
                                else
                                {
                                    this.sampleTime = this.curSample.length - 1;
                                    this.streamingSample = false;
                                }
                            }

                        }
                        break;

                    case Effect.VolumeSlide:
                        this.perTickEffect = true;
                        break;

                    case Effect.PositionJump:
                        ret = new NoteChangeEffect(NoteChangeEffect.Command.JumpToPattern, this.effectParam1 * 16 + this.effectParam2);
                        break;

                    case Effect.SetVolume:
                        {
                            int paramVol = mn * 16 + ln;
                            this.vol64 = paramVol;
                            shouldResetVolume = false;
                            shouldUpdateVolume = true;
                        }
                        break;

                    case Effect.PatternBreak:
                        ret = new NoteChangeEffect(NoteChangeEffect.Command.BreakPattern, mn * 10 + ln);
                        break;

                    case Effect.SetFilter:
                        // Documented as a used feature, but better off ignoring.
                        break;

                    case Effect.SetFinSlideUp:
                        this.period = Mathf.Max(113, this.period - ln);
                        this.UpdateSamplePlayIncrease();
                        shouldResetVolume = false;
                        break;

                    case Effect.SetFinSlideDown:
                        this.period = Mathf.Min(856, this.period + ln);
                        this.UpdateSamplePlayIncrease();
                        shouldResetVolume = false;
                        break;

                    case Effect.SetGlissando:
                        glissando = (ln != 0);
                        break;

                    case Effect.SetVibratoForm:
                        GetFormData(effectParam2, out this.vibratoRetrigger, out this.vibratoForm);
                        this.vibratoUsed = ProcessWaveform(this.vibratoForm);
                        break;

                    case Effect.SetFinetune:
                        this.finetune = ModFile.NibbleToFinetune(this.effectParam2);
                        break;

                    case Effect.LoopPattern:
                        break;

                    case Effect.SetTremoloForm:
                        GetFormData(effectParam2, out this.vibratoRetrigger, out this.tremoloForm);
                        this.vibratoUsed = ProcessWaveform(this.tremoloForm);
                        break;

                    case Effect.Unused:
                        break;

                    case Effect.Retrigger:
                        this.perTickEffect = true;
                        break;

                    case Effect.FineVolumeUp:
                        this.vol64 = Mathf.Clamp(this.vol64 + ln, 0, 64);
                        this.UpdateVolume();
                        break;

                    case Effect.FineVolumeDown:
                        this.vol64 = Mathf.Clamp(this.vol64 - ln, 0, 64);
                        this.UpdateVolume();
                        break;

                    case Effect.CutSample:
                        {
                            if (ln == 0)
                                this.vol = 0.0f;

                            this.perTickEffect = true;
                        }
                        break;

                    case Effect.DelaySample:
                        perTickEffect = true;
                        if (n.sampleIdx > 0 && this.effectParam2 > 1)
                        {
                            // We delay it by turning off streaming for a while
                            this.streamingSample = false;
                            this.perTickEffect = true;
                        }
                        break;

                    case Effect.DelayPattern:
                        ret = new NoteChangeEffect(NoteChangeEffect.Command.DelayPattern, this.effectParam2);
                        break;

                    case Effect.InvertLoop:
                        shouldResetVolume = true;
                        // The speed change is handled in UpdateSamplePlayIncrease().
                        break;

                    case Effect.SetSpeed:
                        {
                            int paramSpeed = mn * 16 + ln;
                            if (paramSpeed == 0)
                                paramSpeed = 1;

                            if (paramSpeed <= 32) // Ticks/Division
                                ret = new NoteChangeEffect(NoteChangeEffect.Command.SetTPD, paramSpeed);
                            else // BPM
                                ret = new NoteChangeEffect(NoteChangeEffect.Command.SetBPM, paramSpeed);
                        }
                        break;
                }
            }
            else
            {
                this.lastEffect = Effect.None;
                this.effectParam1 = 0;
                this.effectParam2 = 0;
            }

            if (setPeriodToTarget == true)
                this.period = this.targetPeriod;

            if (resetVib == true)
                this.vibratoPos = 0.0;

            if (resetTrem == true)
                this.tremoloPos = 0.0;

            if (shouldResetVolume == true && this.curSample != null) 
            {
                this.vol64 = this.curSample.defaultVolume;
                shouldUpdateVolume = true;
            }
            if(shouldUpdateVolume)
                this.UpdateVolume();

            this.UpdateSamplePlayIncrease();

            return ret;
        }

        /// <summary>
        /// Given a waveform value from an effect parameter, turn it into
        /// a form we're familiar with during audio processing.
        /// </summary>
        /// <param name="param">The waveform effect value.</param>
        /// <param name="retrigger">The destination of the extracted retrigger information.</param>
        /// <param name="form">The destination of the extracted waveform information.</param>
        static void GetFormData(int param, out bool retrigger, out EffectWaveform form)
        {
            // There's a byte pattern we could take advantage of, but for now
            // we're doing it by directly exacting from all possible values.
            switch (param)
            {
                default:
                case 0:
                    form = EffectWaveform.Sine;
                    retrigger = false;
                    break;

                case 1:
                    form = EffectWaveform.RampDown;
                    retrigger = false;
                    break;

                case 2:
                    form = EffectWaveform.Square;
                    retrigger = false;
                    break;

                case 3:
                    form = EffectWaveform.Random;
                    retrigger = false;
                    break;

                case 4:
                    form = EffectWaveform.Sine;
                    retrigger = false;
                    break;

                case 5:
                    form = EffectWaveform.RampDown;
                    retrigger = false;
                    break;

                case 6:
                    form = EffectWaveform.Square;
                    retrigger = false;
                    break;

                case 7:
                    form = EffectWaveform.Random;
                    retrigger = false;
                    break;
            }
        }

        /// <summary>
        /// Given an EffectWaveform, return its usable value.
        /// 
        /// This function's entire purpose is to resolve 
        /// a value for the Random value.
        /// </summary>
        /// <param name="ew">The file format waveform.</param>
        /// <returns>The runtime value, valid for an entire div.</returns>
        static EffectWaveform ProcessWaveform(EffectWaveform ew)
        {
            if (ew == EffectWaveform.Random)
            {
                switch (Random.Range(0, 3))
                {
                    default:
                    case 0:
                        return EffectWaveform.Sine;
                    case 1:
                        return EffectWaveform.Square;
                    case 2:
                        return EffectWaveform.RampDown;
                }

            }
            return ew;
        }

        /// <summary>
        /// Generate the PCM data for the channel.
        /// </summary>
        /// <param name="dst">The PCM destination to ADD our generated PCM into. It's important
        /// to note we're adding into the array, not setting it.</param>
        /// <param name="start">The start index of dst to start writing.</param>
        /// <param name="len">The number of samples to write.</param>
        public void Accum(float[] dst, int start, int len)
        {
            long sis = this.ctx.samplesInSong;
            while (len > 0)
            {
                int writeAmt = len;
                if (this.perTickEffect == true)
                {
                    double dTick = (double)(sis - this.ctx.curDivStart) / (double)(this.ctx.curDivEnd - this.ctx.curDivStart) * this.ctx.ticksPerDiv;
                    int tick = (int)dTick;
                    long endTick = this.ctx.curDivStart + (long)((tick + 1.0) / this.ctx.ticksPerDiv * (this.ctx.curDivEnd - this.ctx.curDivStart)) + 1;

                    writeAmt = Mathf.Min(writeAmt, (int)(endTick - sis));

                    if (tick != this.curTick)
                    {

                        switch (this.lastEffect)
                        {
                            case Effect.SlideUp:
                                this.period = Mathf.Max(this.period - (this.effectParam1 * 16 + this.effectParam2), 113);
                                this.UpdateSamplePlayIncrease();
                                break;

                            case Effect.SlideDown:
                                this.period = Mathf.Max(this.period + (this.effectParam1 * 16 + this.effectParam2), 856);
                                this.UpdateSamplePlayIncrease();
                                break;

                            case Effect.SlideNote:
                                if (this.period > this.targetPeriod)
                                {
                                    this.period = Mathf.Max(this.targetPeriod, this.period - this.periodSlide);
                                    this.UpdateSamplePlayIncrease();
                                }
                                else if (this.period < this.targetPeriod)
                                {
                                    int speed = this.effectParam1 * 16 + this.effectParam2;
                                    this.period = Mathf.Min(this.targetPeriod, this.period + this.periodSlide);
                                    this.UpdateSamplePlayIncrease();
                                }
                                break;

                            case Effect.SlideCont:
                                {
                                    // Duplicate of case ModFile.Note.Effect.SlideNote
                                    if (this.period > this.targetPeriod)
                                    {
                                        this.period = Mathf.Max(this.targetPeriod, this.period - this.periodSlide);
                                        this.UpdateSamplePlayIncrease();
                                    }
                                    else if (this.period < this.targetPeriod)
                                    {
                                        int speed = this.effectParam1 * 16 + this.effectParam2;
                                        this.period = Mathf.Min(this.targetPeriod, this.period + this.periodSlide);
                                        this.UpdateSamplePlayIncrease();
                                    }

                                    // Duplicate of case ModFile.Note.Effect.VolumeSlide
                                    if (this.effectParam1 != 0)
                                    {
                                        this.vol64 = Mathf.Min(64, this.vol64 + this.effectParam1);
                                        this.UpdateVolume();
                                    }
                                    else if (this.effectParam2 != 0)
                                    {
                                        this.vol64 = Mathf.Max(0, this.vol64 - this.effectParam2);
                                        this.UpdateVolume();
                                    }
                                }
                                break;

                            case Effect.VibratoCont:
                            case Effect.VolumeSlide:
                                if (this.effectParam1 != 0)
                                {
                                    this.vol64 = Mathf.Min(64, this.vol64 + this.effectParam1);
                                    this.UpdateVolume();
                                }
                                else if (this.effectParam2 != 0)
                                {
                                    this.vol64 = Mathf.Max(0, this.vol64 - this.effectParam2);
                                    this.UpdateVolume();
                                }
                                break;

                            case Effect.CutSample:
                                if (this.effectParam2 >= 0 && this.curTick >= this.effectParam2)
                                {
                                    this.vol64 = 0;
                                    this.UpdateVolume();
                                }
                                break;

                            case Effect.DelaySample:
                                {
                                    if (tick == this.effectParam2)
                                    {
                                        if (this.CanStream())
                                            this.streamingSample = true;
                                    }
                                }
                                break;

                            case Effect.Retrigger:
                                {
                                    if (this.effectParam2 == 0 || (tick % effectParam2) == 0)
                                    {
                                        if (this.CanStream())
                                            this.sampleTime = 0.0f;
                                    }
                                }
                                break;
                        }
                    }
                    this.curTick = tick;

                }

                if (this.streamingSample == true)
                {
                    if (this.vol == 0.0)
                    {
                        this.sampleTime += this.sampleIncr * writeAmt;

                        if (this.sampleTime > this.curSample.length)
                        {
                            if (this.curSample.loopLength > 0)
                            {
                                this.sampleTime -= this.curSample.length;
                                this.sampleTime = this.curSample.loopBack + this.sampleTime % this.curSample.loopLength;
                            }
                            else
                                this.streamingSample = false;
                        }
                    }
                    else
                    {
                        int end = start + writeAmt;

                        // To reduce overhead when doing normal playback, and to specialize the tight loops
                        // for the various high-resolution changes that don't happen on tick boundaries,
                        // vibrato and tremolo get their own implementations.
                        if (this.lastEffect == Effect.Vibrato || this.lastEffect == Effect.VibratoCont)
                        {
                            // A copy of normal execution, but with extra processing.
                            for (int i = start; i < end; ++i)
                            {
                                int ns0 = (int)this.sampleTime;
                                int ns1 = ns0 + 1;

                                if (ns1 >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                        ns1 = this.curSample.loopBack;

                                    else
                                        ns1 = this.curSample.length - 1;

                                }

                                float linterp = (float)(this.sampleTime % 1.0);

                                float s1 = this.curSample.pcm[ns0];
                                float s2 = this.curSample.pcm[ns1];

                                dst[i] += (s1 + (s2 - s1) * linterp) * this.vol;


                                double wavePortion = 0.0;
                                switch (this.vibratoUsed)
                                {
                                    case EffectWaveform.Random:
                                        break;

                                    case EffectWaveform.Sine:
                                        const double tau = 6.28318530717958647692528676655900576839433879875021; // 2PI
                                        wavePortion = System.Math.Sin(this.vibratoPos * tau); // Small optimization possible here if we cache the value of 
                                        break;

                                    case EffectWaveform.Square:
                                        wavePortion = (this.vibratoPos % 1.0 > 0.5) ? -1.0 : 1.0;
                                        break;

                                    case EffectWaveform.RampDown:
                                        wavePortion = 1.0 - (this.vibratoPos % 1.0) * 2.0;
                                        break;
                                }
                                double incr = this.sampleIncr * System.Math.Pow(2.0, wavePortion * this.vibratoIncr / 12.0);
                                this.sampleTime += incr;
                                this.vibratoPos += this.vibratoIncr;

                                if (this.sampleTime >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                    {
                                        this.sampleTime = this.curSample.loopBack + (this.sampleTime - this.sampleEnd) % this.curSample.loopLength;
                                        this.sampleEnd = this.curSample.loopBack + this.curSample.loopLength;
                                    }
                                    else
                                    {
                                        streamingSample = false;
                                        break;
                                    }
                                }

                            }
                        }
                        else if (this.lastEffect == Effect.Tremolo)
                        {
                            for (int i = start; i < end; ++i)
                            {
                                int ns0 = (int)this.sampleTime;
                                int ns1 = ns0 + 1;

                                if (ns1 >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                    {
                                        ns1 = this.curSample.loopBack;
                                        this.sampleEnd = this.curSample.loopBack + this.curSample.loopLength;
                                    }
                                    else
                                        ns1 = this.curSample.length - 1;

                                }

                                float linterp = (float)(this.sampleTime % 1.0);

                                float s1 = this.curSample.pcm[ns0];
                                float s2 = this.curSample.pcm[ns1];

                                double wavePortion = 0.0;
                                switch (this.tremoloUsed)
                                {
                                    case EffectWaveform.Random:
                                        break;

                                    case EffectWaveform.Sine:
                                        const double tau = 6.28318530717958647692528676655900576839433879875021; // 2PI
                                        wavePortion = 0.5 + System.Math.Sin(this.tremoloPos * tau) * 0.5; // Small optimization possible here if we cache the value of 
                                        break;

                                    case EffectWaveform.Square:
                                        wavePortion = (this.tremoloPos % 1.0 > 0.5) ? 0.0 : 1.0;
                                        break;

                                    case EffectWaveform.RampDown:
                                        wavePortion = 1.0 - (this.tremoloPos % 1.0);
                                        break;
                                }

                                dst[i] += (s1 + (s2 - s1) * linterp) * this.vol * (float)(wavePortion * this.tremoloAmp);

                                this.sampleTime += this.sampleIncr;
                                this.tremoloPos += this.tremoloIncr;

                                if (this.sampleTime >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                    {
                                        this.sampleTime = this.curSample.loopBack + (this.sampleTime - this.sampleEnd) % this.curSample.loopLength;
                                        this.sampleEnd = this.curSample.loopBack + this.curSample.loopLength;
                                    }
                                    else
                                    {
                                        streamingSample = false;
                                        break;
                                    }
                                }

                            }
                        }
                        else
                        {
                            // Normal execution
                            for (int i = start; i < end; ++i)
                            {
                                int ns0 = (int)this.sampleTime;
                                int ns1 = ns0 + 1;

                                if (ns1 >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                        ns1 = this.curSample.loopBack;
                                    else
                                        ns1 = this.curSample.length - 1;

                                }

                                float linterp = (float)(this.sampleTime % 1.0);

                                float s1 = this.curSample.pcm[ns0];
                                float s2 = this.curSample.pcm[ns1];

                                dst[i] += (s1 + (s2 - s1) * linterp) * this.vol;

                                this.sampleTime += this.sampleIncr;

                                if (this.sampleTime >= this.sampleEnd)
                                {
                                    if (this.curSample.loopLength > 0)
                                    {
                                        this.sampleTime = this.curSample.loopBack + (this.sampleTime - this.sampleEnd) % this.curSample.loopLength;
                                        this.sampleEnd = this.curSample.loopBack + this.curSample.loopLength;
                                    }
                                    else
                                    {
                                        streamingSample = false;
                                        break;
                                    }
                                }

                            }
                        }
                    }
                }
                len -= writeAmt;
                start += writeAmt;
                sis += writeAmt;
            }
        }

        /// <summary>
        /// Queries whether our state would allow for the current sample to be played.
        /// </summary>
        /// <returns>True if curSample can be saftely and validly streamed. Else, false.</returns>
        bool CanStream()
        {
            return this.curSample != null && this.sampleTime < this.curSample.length && this.sampleIncr > 0.0;
        }
    }
}