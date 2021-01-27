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
    
    ModPlayCtx playCtx = null;
    ModFile loadedSong = null;

    AudioSource source = null;
    AudioClip clip = null;

    Vector2 scroll = Vector2.zero;

    public List<Sample> testSamples;

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
        this.scroll = GUILayout.BeginScrollView(this.scroll);

        foreach (Sample s in this.testSamples)
        { 
            GUILayout.BeginHorizontal();

            GUILayout.Label(s.name, GUILayout.Width(300.0f));

            if(GUILayout.Button("Once") == true)
                this.LoadAndPlay(s, false);

            if (GUILayout.Button("Loop") == true)
                this.LoadAndPlay(s, true);

            if(GUILayout.Button("WWW") == true)
                this.WebTest(s.path);

            GUILayout.EndHorizontal();
        }

        if(this.playCtx != null)
        {
            GUILayout.Box("Playback Info");

            GUILayout.Label( $"Samples {this.playCtx.samplesInSong}");
            GUILayout.Label($"Pos {this.playCtx.curSeq}");
            GUILayout.Label($"Div {this.playCtx.curDiv}");
            GUILayout.Label($"Div Start {this.playCtx.curDivStart}");
            GUILayout.Label($"Div End {this.playCtx.curDivEnd}");

            foreach (ChannelPlayhead cph in this.playCtx.channelPlays)
            { 
                GUILayout.Label( $"Layer {cph.channel}");
            }
        }

        if(this.loadedSong != null)
        { 
            GUILayout.Box("Song Info");
            GUILayout.Label($"Channels {this.loadedSong.channelsCt}");
            GUILayout.Label($"Restart Pos {this.loadedSong.restartPos}");
            GUILayout.Label($"SongLength {this.loadedSong.songLength}");

            GUILayout.Box("Samples Info");
            foreach( ModFile.Sample mfs in this.loadedSong.samples)
            {
                GUILayout.Label($"Sample {mfs.name}");
                GUILayout.Label($"\tLength {mfs.length}");
                GUILayout.Label($"\tLoop Pos {mfs.loopBack}");
                GUILayout.Label($"\tLoop Len {mfs.loopLength}");
                GUILayout.Label($"\tPCM Samples {mfs.pcm.Length}");
                GUILayout.Label($"\tFine Tune {mfs.finetune}");
                GUILayout.Label($"\tDefault Vol {mfs.defaultVolume}");
            }

        }

        GUILayout.EndScrollView();

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
