using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vinyl.Mod;

public class TestMod : MonoBehaviour
{
    [System.Serializable]
    public struct Sample
    { 
        public string name;
        public string path;
    }

    public enum LoadMode
    { 
        None,
        Once,
        Loop,
        WWW
    }
    
    ModPlayCtx playCtx = null;
    ModFile loadedSong = null;

    AudioSource source = null;
    AudioClip clip = null;

    Vector2 scroll = Vector2.zero;

    public List<Sample> testSamples;

    int lastSongLoadedIdx = -1;
    LoadMode lastLoadMode = LoadMode.None;


    public enum ViewMode
    { 
        SongListings,
        PlaybackInfo,
        SongInfo
    }

    ViewMode viewMode = ViewMode.SongListings;

    void Start()
    {
        this.source = this.gameObject.AddComponent<AudioSource>();
        this.source.volume = 0.5f;
    }

    void Update()
    {
        
    }

    void LoadAndPlay(Sample s, bool forceloop)
    {
        this.loadedSong = new Vinyl.Mod.ModFile();
        this.loadedSong.Load(s.path);

        this.playCtx = this.loadedSong.CreateContext(44100, forceloop);
        AudioClip ac = AudioClip.Create("", 2048, 1, 44100, true, this.playCtx.PCMReaderCallback, null);

        this.source.clip = ac;
        this.source.loop = true;
        this.source.Play();
    }

    private void OnGUI()
    {
        this.scroll = GUILayout.BeginScrollView(this.scroll, GUILayout.Width(600.0f));

        GUILayout.BeginVertical("box");
            GUILayout.Label("Options");
            GUILayout.BeginHorizontal();
                if(GUILayout.Toggle(this.viewMode == ViewMode.SongListings, "Songs"))
                    this.viewMode = ViewMode.SongListings;

                if(this.lastLoadMode == LoadMode.Loop || this.lastLoadMode == LoadMode.Once)
                {
                    if (GUILayout.Toggle(this.viewMode == ViewMode.SongInfo, "Song Info"))
                        this.viewMode = ViewMode.SongInfo;
                    if (GUILayout.Toggle(this.viewMode == ViewMode.PlaybackInfo, "Playback Info"))
                        this.viewMode = ViewMode.PlaybackInfo;
                }
            GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        bool playingFromCtx = this.lastLoadMode == LoadMode.Loop || this.lastLoadMode == LoadMode.Once;
        if (this.playCtx != null && playingFromCtx)
        {
            GUILayout.BeginVertical("box");
                GUILayout.Label($"SONG: {this.testSamples[this.lastSongLoadedIdx].name}");
                GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Restart") == true)
                    {
                        this.playCtx.Restart();
                        this.playCtx.Play();
                    }

                    if (GUILayout.Button("Stop") == true)
                    {
                        this.playCtx.Stop();
                    }

                    if (GUILayout.Button("Play") == true)
                    {
                        this.playCtx.Play();
                    }
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        switch(this.viewMode)
        { 
            case ViewMode.SongListings:
                this.OnGUI_ListSongs();
                break;

            case ViewMode.SongInfo:
                this.OnGUI_ShowSongInfo();
                break;

            case ViewMode.PlaybackInfo:
                this.OnGUI_ShowPlaybackInfo();
                break;
        }

        GUILayout.EndScrollView();

    }

    void OnGUI_ListSongs()
    {
        for (int i = 0; i < this.testSamples.Count; ++i)
        {
            Sample s = this.testSamples[i];

            GUILayout.BeginHorizontal();

            GUILayout.Label(s.name, GUILayout.Width(300.0f));

            if(this.lastSongLoadedIdx == i && this.lastLoadMode == LoadMode.Once)
                GUI.color = Color.green;
            if (GUILayout.Button("Once") == true)
            {
                this.lastSongLoadedIdx = i;
                this.lastLoadMode = LoadMode.Once;
                this.LoadAndPlay(s, false);
            }
            GUI.color = Color.white;

            if (this.lastSongLoadedIdx == i && this.lastLoadMode == LoadMode.Loop)
                GUI.color = Color.green;
            if (GUILayout.Button("Loop") == true)
            {
                this.lastSongLoadedIdx = i;
                this.lastLoadMode = LoadMode.Loop;
                this.LoadAndPlay(s, true);
            }
            GUI.color = Color.white;

            if (this.lastSongLoadedIdx == i && this.lastLoadMode == LoadMode.WWW)
                GUI.color = Color.green;
            if (GUILayout.Button("WWW") == true)
            {
                this.lastSongLoadedIdx = i;
                this.lastLoadMode = LoadMode.WWW;
                this.WebTest(s.path);
            }
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }
    }

    void OnGUI_ShowPlaybackInfo()
    {
        if (this.playCtx != null)
        {
            GUILayout.Box("Playback Info");

            GUILayout.Label($"Samples {this.playCtx.samplesInSong}");

            float curBPM = (float)(this.playCtx.bps * 60.0);
            GUILayout.BeginHorizontal();
                GUILayout.Label("BPM");
                if(GUILayout.Button("-5", GUILayout.ExpandWidth(false)))
                    this.playCtx.SetBPM(curBPM - 5.0f);

                GUI.enabled = false;
                GUILayout.TextField(curBPM.ToString());
                GUI.enabled = true;

                if (GUILayout.Button("+5", GUILayout.ExpandWidth(false)))
                    this.playCtx.SetBPM(curBPM + 5.0f);

            GUILayout.EndHorizontal();

            int posIt = 0;
            GUILayout.Label($"Seq {this.playCtx.curSeq}");
            while(posIt < this.playCtx.song.sequences.Length)
            { 
                GUILayout.BeginHorizontal();
                for(int i = 0; i < 4; ++i)
                { 
                    int seqIt = posIt + i;
                    if(seqIt >= this.playCtx.song.sequences.Length)
                    { 
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    if(seqIt == this.playCtx.curSeq)
                        GUI.color = Color.green;

                    if(GUILayout.Button($"SEQ {seqIt}"))
                        this.playCtx.sequenceJump = seqIt;

                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();
                posIt += 4;
            }

            GUILayout.Label($"Div {this.playCtx.curDiv}");
            for(int r = 0; r < 4; ++r)
            {
                GUILayout.BeginHorizontal();
                int baseDiv = r * 12;
                for(int i = 0; i < 12; ++i)
                { 
                    int divIdx = baseDiv + i;
                    if(this.playCtx.curDiv == divIdx)
                        GUI.color = Color.green;

                    if(GUILayout.Button(divIdx.ToString()) == true)
                        this.playCtx.curDiv = divIdx;

                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Label($"Div Start {this.playCtx.curDivStart}");
            GUILayout.Label($"Div End {this.playCtx.curDivEnd}");

            foreach (ChannelPlayhead cph in this.playCtx.channelPlays)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Layer {cph.channel}");

                if (cph.streamingSample == true)
                {
                    GUILayout.Label($"\tStreaming true");
                }
                else
                {
                    GUILayout.Label($"\tStreaming false");
                    GUI.color = Color.gray;
                }

                GUILayout.Label($"\tVolume {cph.vol}");

                ModFile.Sample sample = cph.curSample;

                if (sample != null)
                    GUILayout.Label($"\tSample {sample.name}");
                else
                    GUILayout.Label($"\tSample NONE");

                GUILayout.Label($"\tPeriod {cph.period}");

                GUI.color = Color.white;
                GUILayout.EndVertical();
            }
        }
    }

    void OnGUI_ShowSongInfo()
    {
        if (this.loadedSong != null)
        {
            GUILayout.Box("Song Info");
            GUILayout.Label($"Channels {this.loadedSong.channelsCt}");
            GUILayout.Label($"Restart Pos {this.loadedSong.restartPos}");
            GUILayout.Label($"SongLength {this.loadedSong.songLength}");

            GUILayout.Box("Samples Info");
            for(int i = 0; i < this.loadedSong.samples.Count; ++i)
            {
                ModFile.Sample mfs = this.loadedSong.samples[i];

                GUILayout.BeginVertical("box");
                    GUILayout.Label($"{(i+1).ToString("00")}) Sample - {mfs.name}");
                    GUILayout.Label($"\tLength {mfs.length}");
                    GUILayout.Label($"\tLoop Pos {mfs.loopBack}");
                    GUILayout.Label($"\tLoop Len {mfs.loopLength}");
                    GUILayout.Label($"\tPCM Samples {mfs.pcm.Length}");
                    GUILayout.Label($"\tFine Tune {mfs.finetune}");

                    GUILayout.Label($"\tDefault Vol {mfs.defaultVolume}");
                    GUILayout.BeginHorizontal();
                        GUILayout.Space(60);
                        mfs.defaultVolume = (int)GUILayout.HorizontalSlider(mfs.defaultVolume, 0.0f, 64.0f);
                    GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

        }
    }

    void WebTest(string path)
    { 
        this.StartCoroutine(this.WebTestEnum(path));
    }

    IEnumerator WebTestEnum(string path)
    {
        path = 
            System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                path);

        WWW www = new WWW("file://" + path); 
        yield return www;

        if(string.IsNullOrEmpty(www.error) == false)
        {
            Debug.LogError("Download error : " + www.error);
            yield break;
        }
        AudioClip ac = www.GetAudioClip();
        Debug.Log("Audio clip of downloaded sample : " + ac.samples.ToString());
        if(this.source == null)
            this.source = gameObject.AddComponent<AudioSource>();

        this.source.clip = ac;
        this.source.Play();
    }
}
