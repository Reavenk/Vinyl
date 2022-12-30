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

namespace PxPre.Vinyl.Wav
{
    public class WAVStreamer
    { 
        public enum State
        { 
            NotStarted,
            Initializing,
            Error,
            Streaming,
            Finished
        }

        struct StreamingRef
        {
            public ChunkID id;
            public long filePos;
            public long size;

            public long sampleStart;
            public long sampleEnd;

            public StreamingRef(ChunkID id, long filePos, long size)
            {
                this.id = id;
                this.filePos = filePos;
                this.size = size;

                this.sampleStart = 0;
                this.sampleEnd = 0;
            }
        }

        public State StreamState {get; private set; } = State.NotStarted;

        public double pos {get; protected set; } = 0.0;
        double posIncr = 1.0;
        int curIdx = 0;

        int [] channelOffset = new int []{0};
        public readonly int channels = 1;

        public readonly int sampleRate;

        System.IO.FileStream stream = null;
        System.IO.BinaryReader reader = null;

        public int wavChannels {get => this.format.numChannels; }
        public uint wavSampleRate {get => this.format.sampleRate; }

        ChunkFmt format = new ChunkFmt();

        List<StreamingRef> streamingRefs = new List<StreamingRef>();
        Endianness endi = Endianness.Little;

        public WAVStreamer(string filename, int sampleRate, int channels)
        { 
            this.sampleRate = sampleRate;
            this.channels = channels;
            try
            {
                this.stream = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            }
            catch(System.Exception /*ex*/)
            { 
                this.StreamState = State.Error;
                return;
            }
            this.Initialize();
        }

        public WAVStreamer(System.IO.FileStream stream, int sampleRate, int channels)
        { 
            if(stream == null || stream.CanRead == false)
                this.StreamState = State.Error;

            this.sampleRate = sampleRate;
            this.channels = channels;
            this.stream = stream;

            this.Initialize();
        }

        void Initialize()
        { 
            this.reader = new System.IO.BinaryReader(this.stream);

            this.stream.Seek(0, System.IO.SeekOrigin.Begin);
            List<ChunkTable> chunks = WAVUtils.ParseStaticChunks(this.reader);

            long it = 0;
            SeekPCMRegionsFromChunks(chunks);
            for(int i = 0; i < this.streamingRefs.Count; ++i)
            {
                StreamingRef sr = this.streamingRefs[i];
                sr.sampleStart = it;
                it += sr.size;
                sr.sampleEnd = it;

                this.streamingRefs[i] = sr;
            }

            this.channelOffset = new int[this.channels];
            for(int i = 0; i < this.channels; ++i)
                this.channelOffset[i] = i % this.channels;

            this.posIncr = (double)this.wavSampleRate / (double)this.sampleRate;

            this.StreamState = State.Streaming;
        }

        void SeekPCMRegionsFromChunks(List<ChunkTable> ct)
        {
            foreach (ChunkTable c in ct)
            {
                switch((ChunkID)c.chunkID)
                {
                    case ChunkID.RIFX:
                        endi = Endianness.Big;
                        break;

                    case ChunkID.fmt:
                        this.stream.Seek(c.filePos, System.IO.SeekOrigin.Begin);
                        this.format.Read(this.reader);
                        break;

                    case ChunkID.data:
                        this.streamingRefs.Add(new StreamingRef((ChunkID)c.chunkID, c.filePos, c.size));
                        break;

                    case ChunkID.wavl:
                        this.stream.Seek(c.filePos, System.IO.SeekOrigin.Begin);
                        List<ChunkTable> alternatingDataChunks = new List<ChunkTable>();
                        WAVUtils.ParseBoundedStaticChunksInto(this.reader, alternatingDataChunks, c.filePos + c.size);
                        this.SeekPCMRegionsFromChunks(alternatingDataChunks); // Recursion
                        break;

                    case ChunkID.slnt:
                        uint silentSamples = this.reader.ReadUInt32();
                        this.streamingRefs.Add( new StreamingRef((ChunkID)c.chunkID, c.filePos, silentSamples));
                        break;
                }
            }
        }

        /// <summary>
        /// Streaming callback for AudioClip streaming. Currently UNIMPLEMENTED.
        /// </summary>
        /// <param name="position">The PCM position to change the song position to.</param>
        public void PCMSetPositionCallback(int position)
        {
            if(this.StreamState == State.Error)
                return;

            this.pos = position;
            for(int i = 0; i < this.streamingRefs.Count; ++i)
            { 
                if(
                    position >= this.streamingRefs[i].sampleStart&&
                    position < this.streamingRefs[i].sampleEnd)
                { 
                    this.curIdx = i;
                    this.StreamState = State.Streaming;
                    return;
                }
            }

            this.StreamState = State.Finished;
        }

        /// <summary>
        /// Streaming callback for AudioClip streaming.
        /// </summary>
        /// <param name="data">The buffer to fill in streaming PCM data.</param>
        public void PCMReaderCallback(float[] data)
        {
            if(this.StreamState != State.Streaming)
                return;

            int samples = data.Length;
            int write = 0;

            while(write < samples)
            {
                int len = data.Length / this.channels;

                long endLenSeg = this.streamingRefs[this.curIdx].sampleEnd / this.wavChannels;

                double leftInBuff = (this.pos - endLenSeg);

                //byte [] rb = this.reader.ReadBytes
            
                //(this.pos + len) * this.channels;
                for(int i = 0; i < len; ++i)
                { 
                    int sampR = (int)this.pos * this.wavChannels;
                    float lambda = (float)(this.pos % 1.0);
                    
                    for(int c = 0; c < this.channels; ++c)
                    { 
                        //data[write + c] = 
                    }

                    write += this.channels;
                    this.pos += this.posIncr;
                }
            }

        }

    }
}