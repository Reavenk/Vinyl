using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vinyl.Mod
{
    public class SampleStream
    {
        public ModPlayCtx.PlayState playState { get; protected set; } = ModPlayCtx.PlayState.Playing;
        public readonly int period;
        public readonly int sampleRate;
        public float gain;

        double pos = 0.0f;
        double posIncr = 1.0f;

        ModFile.Sample sample;

        public SampleStream(ModFile.Sample sample, int sampleRate, int period, float gain)
        { 
            this.sample = sample;
            this.sampleRate = sampleRate;
            this.period = period;
            this.gain = gain;

            //if(this.sample.length == 
        }

        public void PCMSetPositionCallback(int position)
        {
        }

        public void PCMReaderCallback(float[] data)
        { 
        }
    }
}