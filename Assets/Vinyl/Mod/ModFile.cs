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
    /// The representation of a *.mod file in a form that's usable for
    /// viewing its contents, as well as for a player context to stream
    /// the song.
    /// 
    /// Format details at http://www.aes.id.au/modformat.html
    /// </summary>
    public class ModFile
    {
        public const int loNibble = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
        public const int hiNibble = loNibble << 4;
        public const int exHiNibble = loNibble << 8;

        public class Sample
        {
            // These constants are references to the documented fact that 
            // the PCM sample rate for Samples are a hardcoded rate that
            // maps to 8287 samples a second when played at period 428.
            public const int C2SampleRate = 8287;
            public const int C2Period = 428;

            /// <summary>
            /// The readable name of the sample.
            /// </summary>
            public string name;

            /// <summary>
            /// The number of PCM samples.
            /// </summary>
            public int length;

            /// <summary>
            /// The fine pitch offset.
            /// </summary>
            /// <remarks>We store the value as a double for efficiency when 
            /// using it in the player context.</remarks>
            public double finetune;

            /// <summary>
            /// When played with something that explicitly changes the volume,
            /// what volume does the sample play at? This is the file's direct
            /// [0,64] value.
            /// </summary>
            public int defaultVolume;

            /// <summary>
            /// If looping, what PCM sample do we return to from the end to
            /// loop?
            /// </summary>
            public int loopBack;

            /// <summary>
            /// If looping, what is the length of the number of PCM samples in the
            /// looping region. If 0, the sample doesn't loop.
            /// </summary>
            public int loopLength;

            /// <summary>
            /// The PCM data. This is the PCM data converted from signed bytes to
            /// floating point samples.
            /// </summary>
            public float [] pcm;
        }

        /// <summary>
        /// A division of a sequence. Each sequence is divided into 64 division.
        /// </summary>
        public struct Div
        { 
            public int sampleIdx;
            public int period;
            public int effectCommand;
        }

        public class Channel
        {
            public Div[] divs;
        }

        public class Pattern
        {
            public Channel [] channels;
        }

        public int channelsCt = 4;

        public string title;
        public List<Sample> samples = new List<Sample>();

        public int songLength
        { 
            get
            { 
                if(this.sequences == null)
                    return 0;

                return sequences.Length;
            }
        }

        public int restartPos;

        public byte [] sequences;

        public List<Pattern> patternData = new List<Pattern>();

        public bool Load(string file)
        { 
            // this.Clear();

            using(System.IO.FileStream fs = new System.IO.FileStream(file, System.IO.FileMode.Open))
            { 
                System.IO.BinaryReader r = new System.IO.BinaryReader(fs);
                return this.Load(r);
            }
        }

        public bool Load(System.IO.BinaryReader r)
        { 

            int sampleCt = 15;
            long startPos = r.BaseStream.Position;

            r.BaseStream.Seek(startPos + 1080, System.IO.SeekOrigin.Begin);
            string mkTag = GetByteArrayString(r, 4);
            r.BaseStream.Seek(startPos, System.IO.SeekOrigin.Begin);

            switch(mkTag)
            { 
                case "M.K.":
                    sampleCt = 31;
                    break;

                case "M!K!":
                    break;

                case "FLT4":
                    break;

                case "FLT8":
                    this.channelsCt = 8;
                    break;

                case "4CHN":
                    break;

                case "6CHN":
                    channelsCt = 6;
                    break;

                case "8CHN":
                    channelsCt = 8;
                    break;
            }   

            this.title = GetByteArrayString(r, 20);

            for (int i = 0; i < sampleCt; ++i)
            {
                Sample s        = new Sample();
                s.name          = GetByteArrayString(r, 22);
                s.length        = ReadUShort(r) * 2;

                int finet = r.ReadByte();
                s.finetune  = NibbleToFinetune(finet);

                byte vol = r.ReadByte();
                s.defaultVolume = vol;

                s.loopBack = ReadUShort(r) * 2;
                s.loopLength = ReadUShort(r) * 2;

                if(s.loopLength <= 2)
                    s.loopLength = 0;

                this.samples.Add(s);
            }

            int patternCt = r.ReadByte();
            this.restartPos = r.ReadByte();

            this.sequences = r.ReadBytes(patternCt);
            if(patternCt != 128)
                r.ReadBytes(128 - patternCt); // Consume the rest of the unneeded 128

            // We already read this at the beginning to figure out sampleCt.
            // Now we're just moving past it to read our binary reader properly
            // aligned.
            r.ReadBytes(4); 

            int highestPatternIdx = -1;
            for(int i = 0; i < this.sequences.Length; ++i)
                highestPatternIdx = Mathf.Max(highestPatternIdx, this.sequences[i]);

            for (int i = 0; i < highestPatternIdx + 1; ++i)
            { 
                Pattern p = new Pattern();
                p.channels = new Channel[channelsCt];
                patternData.Add(p);

                for (int c = 0; c < channelsCt; ++c)
                { 
                    p.channels[c] = new Channel();
                    p.channels[c].divs = new Div[64];
                }

                for(int j = 0; j < 64 * channelsCt; ++j)
                {
                    int n = j/ channelsCt;
                    int c = j % channelsCt;

                    byte [] rbd = r.ReadBytes(4);

                    int sample = (rbd[0] & hiNibble) | ((rbd[2] & hiNibble) >> 4);
                    int period = ((rbd[0]&loNibble) << 8)|rbd[1];
                    int effect = ((rbd[2] & loNibble) << 8) | rbd[3];

                    Div note = new Div();
                    note.sampleIdx = sample - 1; // Rebase it so the ids are array indices and -1 is the non-instrument instead of 0
                    note.period = period;
                    note.effectCommand = effect;

                    p.channels[c].divs[n] = note;
                }
            }

            for(int i = 0; i < sampleCt; ++i)
            { 
                int sCt = Mathf.Max(0, samples[i].length);
                samples[i].pcm = new float[sCt];

                for(int s = 0; s < samples[i].pcm.Length; ++s)
                    samples[i].pcm[s] = (float)r.ReadSByte() / 128.0f;

                // The first two have some weird deal where they're the audio
                // data as well as this other thing
                if(samples[i].pcm.Length >= 2)
                { 
                    samples[i].pcm[0] = 0.0f;
                    samples[i].pcm[1] = 0.0f;
                }
            }

            return true;
        }

        public static double NibbleToFinetune(int finet)
        {
            int finetSign = finet & (1 << 3);
            if (finetSign != 0)
                finet = -(~finet & ((1 << 3) - 1)) - 1;
            else
                finet = (finet & ((1 << 3) - 1));

            return Mathf.Pow(2.0f, finet / (12 * 8));
        }

        static string ByteArrayToString(byte [] rb)
        {
            int end = System.Array.IndexOf<byte>(rb, 0);
            if(end != -1)
                return System.Text.Encoding.ASCII.GetString(rb, 0, end);

            return System.Text.Encoding.ASCII.GetString(rb);
        }

        static string GetByteArrayString(System.IO.BinaryReader r, int byteCt)
        {
            byte[] rb = r.ReadBytes(byteCt);
            return ByteArrayToString(rb);
        }

        public static short ReadShort(System.IO.BinaryReader r)
        { 
            return (short)((r.ReadByte() << 8)|r.ReadByte());
        }

        public static ushort ReadUShort(System.IO.BinaryReader r)
        {
            return (ushort)((r.ReadByte() << 8) | r.ReadByte());
        }

        public ModPlayCtx CreateContext(int samplesPerSec, bool looping, float bpm = 125.0f)
        { 
            return new ModPlayCtx(this, samplesPerSec, looping, bpm);
        }
    }
}