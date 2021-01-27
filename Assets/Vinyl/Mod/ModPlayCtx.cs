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
    /// Mod file streaming player context.
    /// 
    /// Behaviour spec at http://www.aes.id.au/modformat.html
    /// </summary>
    public class ModPlayCtx
    {
        /// <summary>
        /// The current play state of the context.
        /// </summary>
        public enum PlayState
        { 
            /// <summary>
            /// The context is not playing. The most likely reason is because
            /// the song has finished and it's not looping.
            /// </summary>
            Stopped,

            /// <summary>
            /// Playing the audio, but not looping. The context will stop at the end
            /// of the song.
            /// </summary>
            Playing,

            /// <summary>
            /// Playing the audio and looping. If there is no looping position in the song,
            /// the song loops from the very beginning.
            /// </summary>
            Looping,

            /// <summary>
            /// Looping, but only successfully loops is if there is information on where to
            /// return to in the song data - or else the stream is stopped.
            /// </summary>
            OnlyIfLoopInFile
        }

        /// <summary>
        /// Reference to the song data to play.
        /// </summary>
        public readonly ModFile song;

        /// <summary>
        /// Cached playback sample rate.
        /// </summary>
        public readonly int sampleRate;

        /// <summary>
        /// While standard music measured beats in BPMs, BPS is a more 
        /// significant ratio when it comes to actually streaming - this is
        /// because our sample rate is samples/sec.
        /// </summary>
        public double bps {get; protected set;} = 125.0 / 60.0;

        /// <summary>
        /// Ticks per division.
        /// </summary>
        public int ticksPerDiv {get; protected set;} = 6;

        /// <summary>
        /// Channel streaming context for each song channel.
        /// </summary>
        public ChannelPlayhead [] channelPlays;

        /// <summary>
        /// Cached precalculation of number of samples per sequence.
        /// </summary>
        double samplesPerSeq = 0.0;
        public double samplesPerDiv {get; protected set; } = 0.0f;

        /// <summary>
        /// The current sequence being played.
        /// </summary>
        public int curSeq = 0;

        /// <summary>
        /// The sample the current sequence started on.
        /// </summary>
        public long curSeqStart = 0;

        /// <summary>
        /// The sample the current sequence is expected to end on.
        /// </summary>
        public long curSeqEnd = 0;

        /// <summary>
        /// The current division being played.
        /// </summary>
        public int curDiv = 0;

        /// <summary>
        /// The sample the current division ended on.
        /// </summary>
        public long curDivStart = 0;

        /// <summary>
        /// The sample the current division is expected to end on.
        /// </summary>
        public long curDivEnd = 0;

        /// <summary>
        /// If not -1, jumps to this division of the next sequence after
        /// the current division is over.
        /// </summary>
        public int positionJump = -1;

        /// <summary>
        /// If not -1, jumps to the start of this sequence after the current division is over. 
        /// </summary>
        public int sequenceJump = -1;

        /// <summary>
        /// If greater than zero, the pattern gets delayed for this many 
        /// playthrough of the division.
        /// </summary>
        public int delayedPattern = -1;

        // How many samples have been played into the song?
        // Resets upon looping
        //
        // With ints at 44100, we can represent up to 13 hours
        // before wrapping.
        public long samplesInSong = 0;

        /// <summary>
        /// The context's play state.
        /// </summary>
        public PlayState playState {get; protected set;} = PlayState.Playing;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="song">The song to play.</param>
        /// <param name="sampleRate">The playback sample rate.</param>
        /// <param name="looping">If true, the song will loop from the beginning</param>
        /// <param name="bpm">The initial BPM</param>
        public ModPlayCtx(ModFile song, int sampleRate, bool looping, float bpm = 125.0f)
        { 
            this.sampleRate = sampleRate;
            this.song = song;

            // Leverage SetBPM, this initial bps value is a dummy to avoid
            // a divide by zero.
            this.bps = 1.0;
            this.UpdateTimingCache(); // Calculate the oldSPD
            this.SetBPM(bpm);

            this.channelPlays = new ChannelPlayhead[song.channelsCt];
            for(int i = 0; i < this.song.channelsCt; ++i)
                this.channelPlays[i] = new ChannelPlayhead(this, i);

            this.UpdateTimingCache();

            this.playState = 
                (looping == true) ? 
                    PlayState.Looping : 
                    PlayState.Playing;
        }

        /// <summary>
        /// Recalculate and cache significant timing variables.
        /// </summary>
        public void UpdateTimingCache()
        {   //                       24 * beats/min
            // Divisions/minute =   ------------------
            //                       ticks/division

            // this.ticksPerDiv = 31; // Uncomment to heavily slow things down during debugging

            this.samplesPerDiv = this.sampleRate / (24.0 * this.bps / System.Math.Max(this.ticksPerDiv, 1.0));
            this.samplesPerSeq = this.samplesPerDiv * 64.0;

            this.curSeqStart    = (long)((this.curSeq + 0) * this.samplesPerSeq);
            this.curSeqEnd      = (long)((this.curSeq + 1) * this.samplesPerSeq);

            this.curDivStart    = this.curSeqStart + (long)((this.curSeqEnd - this.curSeqStart) * ((this.curDiv + 0) / 64.0));
            this.curDivEnd      = this.curSeqStart + (long)((this.curSeqEnd - this.curSeqStart) * ((this.curDiv + 1) / 64.0));
        }

        /// <summary>
        /// Low level function to modify and update the timing of the playback.
        /// </summary>
        /// <param name="bpm">The new BPM. Leave empty if it shouldn't change.</param>
        /// <param name="tpd">The ticks per-division rate. Leave empty if it shouldn't be changed.</param>
        void SetBPM(float ? bpm, int ? tpd)
        { 
            if(bpm == null && tpd == null)
                return;

            double oldSPD = this.samplesPerDiv;

            const float minBPM = 20.0f;
            const float maxBPM = 300.0f;

            if(bpm.HasValue == true)
                this.bps = Mathf.Clamp(bpm.Value, minBPM, maxBPM) / 60.0;

            if(tpd.HasValue == true)
                this.ticksPerDiv = Mathf.Clamp(tpd.Value, 1, 32);

            this.UpdateTimingCache();

            double ratio = this.samplesPerDiv / oldSPD;
            this.samplesInSong = (long)(this.samplesInSong * ratio);
        }

        /// <summary>
        /// Public access to modify the BPM.
        /// </summary>
        /// <param name="bpm">The BPM to play the audio at.</param>
        /// <remarks>The song can also change the BPM, and there's currently no mechanism to ensure
        /// if you use this function that you won't be fighting against the sont to set the desired
        /// play speed.</remarks>
        public void SetBPM(float bpm)
        {
            bpm = Mathf.Clamp(bpm, 20, 300);
            this.SetBPM(bpm, null);
        }

        /// <summary>
        /// Write in streamed samples for a region of a PCM buffer.
        /// </summary>
        /// <param name="pcm">The PCM buffer to write to.</param>
        /// <param name="start">The starting index to start writing from.</param>
        /// <param name="samples">The number of samples to write</param>
        public void Generate(float[] pcm, int start, int samples)
        {
            // Zero the buffer to prepare it so that channels can
            // mix by adding to it.
            for(int i = 0; i < pcm.Length; ++i)
                pcm[i] = 0.0f;

            // If we're not playing, we still wanted to zero out the
            // PCM to silence it in case there's garbage data in it.
            if(this.playState == PlayState.Stopped)
                return;

            while(samples > 0)
            {
                // double note = amtIntoSeq / samplesPerNote;
                // int notefl = (int)System.Math.Floor(note);
                // 
                // 
                // 
                // // The +1 is because of floating point error. 
                // // It's not a hack because it still conceptually makes sense, 
                // // we want to start AFTER the end, not AT the end.
                // long noteend = (long)((notefl + 1) * samplesPerNote) + 1; 
                // 
                // long samplesInNote = amtIntoSeq - noteStart;

                int writeAmt =
                    Mathf.Min(
                        (int)(this.curDivEnd - this.samplesInSong),
                        samples);

                // Accululate all channels into mono audio.
                foreach (ChannelPlayhead cp in this.channelPlays)
                    cp.Accum(pcm, start, writeAmt);

                // Mixer/Compressor
                for(int i = start; i < start+writeAmt; ++i)
                {
                    // Depending on if the old article is still relevant, there might
                    // be 0 additional overhead for using a doubles function.
                    // https://forum.unity.com/threads/missing-mathf-therm.317607/
                    pcm[i] = (float)System.Math.Tanh(pcm[i]);
                }


                samples -= writeAmt;
                this.samplesInSong += writeAmt;

                if(this.samplesInSong >= this.curDivEnd)
                {
                    if(this.delayedPattern > 0)
                    {
                        this.samplesInSong = this.curDivStart;
                        --this.delayedPattern;
                    }
                    else
                    {
                        ++this.curDiv;

                        if(this.curDiv >= 64 || this.positionJump != -1 || this.sequenceJump != -1)
                        { 
                            ++this.curSeq;
                            this.curDiv = 0;

                            if(this.positionJump != -1)
                            { 
                                this.curDiv = Mathf.Clamp(this.positionJump, 0, 63);
                                this.positionJump = -1;
                            }
                            else if(this.sequenceJump != -1)
                            { 
                                this.curSeq = Mathf.Clamp(this.sequenceJump, 0, this.song.sequences.Length);
                                this.sequenceJump = -1;
                            }

                            if (this.curSeq >= this.song.sequences.Length)
                            {
                                bool stopped = true;
                                if ( this.playState == PlayState.Playing || this.playState == PlayState.Stopped)
                                { } // No looping, eat up the possibility of looping with empty if
                                else if(this.song.restartPos < this.song.sequences.Length)
                                {
                                    this.curSeq = this.song.restartPos;
                                    stopped = false;
                                }
                                else if(this.playState == PlayState.Looping)
                                { 
                                    // If nowhere to loop but we're forcing a loop, start from the beginning
                                    this.Restart();

                                    stopped = false;
                                }
                            
                                if(stopped == true)
                                {
                                    this.playState = PlayState.Stopped;
                                    return;
                                }
                            }

                            foreach (ChannelPlayhead cp in this.channelPlays)
                                cp.OnChangeSequence(this.curSeq);
                        }

                        this.UpdateTimingCache();
                        this.PerformChangeDivNotification();
                    }
                }
            }
        }

        /// <summary>
        /// Perform OnChangeDiv notifications to all channels and handle their
        /// responses.
        /// </summary>
        void PerformChangeDivNotification()
        {
            bool restart = false;
            foreach (ChannelPlayhead cp in this.channelPlays)
            {
                NoteChangeEffect nce = cp.OnChangeDiv(this.curSeq, this.curDiv);
                switch (nce.cmd)
                {
                    case NoteChangeEffect.Command.Stop:
                        if (this.playState == PlayState.Looping)
                        {
                            if (this.curSeq != 0 && this.curDiv != 0)
                                restart = true;
                        }
                        else
                        {
                            this.playState = PlayState.Stopped;
                            return;
                        }
                        break;

                    case NoteChangeEffect.Command.BreakPattern:
                        this.positionJump = nce.param;
                        break;

                    case NoteChangeEffect.Command.JumpToPattern:
                        this.sequenceJump = nce.param;
                        break;

                    case NoteChangeEffect.Command.DelayPattern:
                        this.delayedPattern = nce.param;
                        break;

                    case NoteChangeEffect.Command.SetBPM:
                        this.SetBPM(nce.param, null);
                        break;

                    case NoteChangeEffect.Command.SetTPD:
                        this.SetBPM(null, nce.param);
                        break;
                }
            }

            if (restart == true)
                this.Restart();
        }

        /// <summary>
        /// Restart the song.
        /// </summary>
        void Restart()
        {
            this.curSeq = 0;
            this.curDiv = 0;
            this.samplesInSong = 0;
            foreach (ChannelPlayhead cpi in this.channelPlays)
            {
                cpi.Reset();
                cpi.OnChangeSequence(0);
            }

            this.PerformChangeDivNotification();
        }

        /// <summary>
        /// Streaming callback for AudioClip streaming. Currently UNIMPLEMENTED.
        /// </summary>
        /// <param name="position">The PCM position to change the song position to.</param>
        public void PCMSetPositionCallback(int position)
        { 
        }

        /// <summary>
        /// Streaming callback for AudioClip streaming.
        /// </summary>
        /// <param name="data">The buffer to fill in streaming PCM data.</param>
        public void PCMReaderCallback(float [] data)
        {
            this.Generate(data, 0, data.Length);
        }
    }
}