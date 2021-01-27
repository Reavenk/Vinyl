using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWav : MonoBehaviour
{
    [System.Serializable]
    public struct TestSample
    { 
        public string label;
        public string path;
    }

    PxPre.Vinyl.Wav.ChunkFmt format = new PxPre.Vinyl.Wav.ChunkFmt();

    List<PxPre.Vinyl.Wav.ChunkTable> foundChunks = new List<PxPre.Vinyl.Wav.ChunkTable>();

    public AudioSource audioSource;

    public List<TestSample> samples = new List<TestSample>();

    public PxPre.Vinyl.Meta.ID3 ? meta = null;

    public Vector2 scroll;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        this.scroll = GUILayout.BeginScrollView(this.scroll);

        for (int i = 0; i < this.samples.Count; ++i)
        { 
            TestSample ts = this.samples[i];
            if(GUILayout.Button(ts.label) == true)
            {
                this.meta = null;

                byte [] rb = System.IO.File.ReadAllBytes(ts.path);
                System.IO.MemoryStream memStream = new System.IO.MemoryStream(rb);
                System.IO.BinaryReader r = new System.IO.BinaryReader(memStream);
                this.foundChunks = PxPre.Vinyl.Wav.WAVUtils.ParseStaticChunks(r);

                Debug.Log(this.foundChunks.Count.ToString() + " chunks!");

                foreach(PxPre.Vinyl.Wav.ChunkTable c in this.foundChunks)
                { 
                    if(c.chunkID == (int)PxPre.Vinyl.Wav.ChunkID.fmt)
                    { 
                        
                        memStream.Seek(c.filePos, System.IO.SeekOrigin.Begin);
                        this.format.Read(r);
                    }
                    else if(c.chunkID == (int)PxPre.Vinyl.Wav.ChunkID.id3)
                    {
                        memStream.Seek(c.filePos, System.IO.SeekOrigin.Begin);
                        PxPre.Vinyl.Meta.ID3_2 id32 = new PxPre.Vinyl.Meta.ID3_2();
                        this.meta = id32.ReadToID31(r);

                        
                        //PxPre.WavLib.ID3 id3 = new PxPre.WavLib.ID3();
                        //id3.Read(r);
                        //this.meta = id3;
                    }
                }

                PxPre.Vinyl.Wav.ChunkFmt ? fmt;
                List<PxPre.Vinyl.Wav.AudioChunk> audios = 
                    PxPre.Vinyl.Wav.WAVUtils.ParseStaticPCM(r, this.foundChunks, out fmt);

                float [] pcm = PxPre.Vinyl.Wav.WAVUtils.GetChannel(audios, 0);

                AudioClip ac = AudioClip.Create("", pcm.Length, this.format.numChannels, (int)this.format.sampleRate, false);
                ac.SetData(pcm, 0);
                this.audioSource.clip = ac;
                this.audioSource.Play();
            }
        }

        GUILayout.Label($"Compression : {format.compressionCode}");
        GUILayout.Label($"Channels : {format.numChannels}");
        GUILayout.Label($"Sample Rate : {format.sampleRate}");
        GUILayout.Label($"Byte Rate : {format.avgBytesPerSecond}");
        GUILayout.Label($"Block Align : {format.blockAlign}");
        GUILayout.Label($"Significant Bits/S : {format.sigBitsPerSample}");
        GUILayout.Label($"Extra Bytes : {format.extraFormatBytes}");

        if(this.foundChunks.Count > 0)
        { 
            foreach(PxPre.Vinyl.Wav.ChunkTable ct in this.foundChunks)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("CHUNK DATA");

                    byte [] rb = System.BitConverter.GetBytes(ct.chunkID);
                    //System.Array.Reverse(rb);
                    string id = System.Text.ASCIIEncoding.ASCII.GetString(rb);
                    GUILayout.Label($"ID : {id}");
                    GUILayout.Label($"POS : {ct.filePos}");
                    GUILayout.Label($"SIZE : {ct.size}");
                GUILayout.EndHorizontal();
            }
        }

        if(this.meta != null)
        {
            GUILayout.Label($"Tag : {this.meta.Value.tag}");
            GUILayout.Label($"Title : {this.meta.Value.title}");
            GUILayout.Label($"Artist : {this.meta.Value.artist}");
            GUILayout.Label($"Album : {this.meta.Value.album}");
            GUILayout.Label($"Year : {this.meta.Value.year}");
            GUILayout.Label($"Comment : {this.meta.Value.comment}");
            GUILayout.Label($"Track : {this.meta.Value.track}");
            GUILayout.Label($"Genre : {this.meta.Value.genre}");
        }

        if(this.audioSource.clip != null)
        { 
            if(GUILayout.Button("Export byte") == true)
            { 
                float [] data = new float[this.audioSource.clip.samples];
                this.audioSource.clip.GetData(data, 0);

                byte [] rb = new byte[data.Length];
                for(int i = 0; i < data.Length; ++i)
                    rb[i] = (byte)Mathf.Clamp((int)(data[i] * 128.0 + 128.0f), 0, 255);

                System.IO.FileStream fs = new System.IO.FileStream("TestExport8i.wav", System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                PxPre.Vinyl.Wav.WAVUtils.CreateSimpleWavBinary(bw, rb, 44100, 1);
            }

            if (GUILayout.Button("Explort short") == true)
            {
                float[] data = new float[this.audioSource.clip.samples];
                this.audioSource.clip.GetData(data, 0);

                short [] rs = new short[data.Length];
                for(int i = 0; i < data.Length; ++i)
                    rs[i] = (short)Mathf.Clamp((int)(data[i] * short.MaxValue), short.MinValue, short.MaxValue);

                System.IO.FileStream fs = new System.IO.FileStream("TestExport16i.wav", System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                PxPre.Vinyl.Wav.WAVUtils.CreateSimpleWavBinary(bw, rs, 44100, 1);
            }

            if (GUILayout.Button("Export int") == true)
            {
                float[] data = new float[this.audioSource.clip.samples];
                this.audioSource.clip.GetData(data, 0);

                int [] ri = new int[data.Length];
                for (int i = 0; i < data.Length; ++i)
                    ri[i] = Mathf.Clamp((int)(data[i] * int.MaxValue), int.MinValue, int.MaxValue);

                System.IO.FileStream fs = new System.IO.FileStream("TestExport32i.wav", System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                PxPre.Vinyl.Wav.WAVUtils.CreateSimpleWavBinary(bw, ri, 44100, 1);
            }

            if (GUILayout.Button("Export float") == true)
            {
                float[] data = new float[this.audioSource.clip.samples];
                this.audioSource.clip.GetData(data, 0);

                // Yea yea, no practical point to this, but we're just testing the
                // float saving.
                float [] rf = data;

                System.IO.FileStream fs = new System.IO.FileStream("TestExport32f.wav", System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                PxPre.Vinyl.Wav.WAVUtils.CreateSimpleWavBinary(bw, rf, 44100, 1);
            }

            if (GUILayout.Button("Export double") == true)
            {
                float[] data = new float[this.audioSource.clip.samples];
                this.audioSource.clip.GetData(data, 0);

                double [] rd = new double[data.Length];
                for(int i = 0; i < data.Length; ++i)
                    rd[i] = data[i];

                System.IO.FileStream fs = new System.IO.FileStream("TestExport64f.wav", System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                PxPre.Vinyl.Wav.WAVUtils.CreateSimpleWavBinary(bw, rd, 44100, 1);
            }
        }

        GUILayout.EndScrollView();
    }
}
