using System;
using System.Collections.Generic;
using UnityEngine;

public class Session : MonoBehaviour
{
    public Song song;
    public Player playerPrefab;
    public Player player;
    public SessionRenderer sessionRenderer;
    public Sprite[] notes;
    public bool playing;
    public float speed; //meter per second
    public GameObject[] prefabs;
    public GameObject[] barPrefabs;
    public int frameIndex;
    public int syncIndex;
    public float boardLength = 10; //meters
    public float time, previousTime;
    public double visualOffset;
    public double tick;
    public double smoothTick;
    public double starPowerDuration;
    public double bpm, smoothBpm;
    public float RenderingFadeDistance = 3;
    public float RenderingFadeAmount = 1;

    private double beatsPerSecond, secondsPassed, beatsPassed, ticksPassed;

    //public NoteInstance[] noteInstancePool;
    private AudioSource guitarSource, rhythmSource, songSource;
    public Smoothing smoothing;

    private void Update()
    {
        if (songSource != null && songSource.isPlaying)
        {
            //first get input for this frame
            player.GetInput();
            frameIndex++;
            time = songSource.time * 1000f;
            float millisecondsPassed = time - previousTime;
            rhythmSource.time = songSource.time;
            guitarSource.time = songSource.time;

            Sync(millisecondsPassed);
            smoothBpm = smoothing.SmoothBPM(bpm);
            smoothTick = smoothing.SmoothTick(tick,
                song.data.info.resolution);
            bool playGuitarMusic = false;
            player.SpawnObjects(tick,
                beatsPerSecond);
            player.UpdateObjects(smoothTick,
                notes,
                frameIndex);
            player.CreateBar(tick);
            player.UpdateActiveBars(smoothTick);
            player.RegisterAndRemove(smoothTick);
            playGuitarMusic |= player.lastNoteHit;

            guitarSource.volume = playGuitarMusic ? 1 : 0;

            previousTime = time;
        }
        else
        {
            if (playing)
            {
                guitarSource.Play();
                rhythmSource.Play();
                songSource.Play();
            }
        }
    }

    public void Initialize(Song _song
    )
    {
        Debug.Log("initializing ");
        song = _song;
        guitarSource = gameObject.AddComponent<AudioSource>();
        rhythmSource = gameObject.AddComponent<AudioSource>();
        songSource = gameObject.AddComponent<AudioSource>();
        guitarSource.playOnAwake = rhythmSource.playOnAwake = songSource.playOnAwake = false;
        guitarSource.clip = song.audio.guitar;
        rhythmSource.clip = song.audio.rhythm;
        songSource.clip = song.audio.song;
        Shader.SetGlobalFloat("_GH_Distance",
            RenderingFadeDistance);
        Shader.SetGlobalFloat("_GH_Fade",
            RenderingFadeAmount);
        smoothing = new Smoothing(visualOffset);
        List<RenderTexture> outputs = new List<RenderTexture>();

        player = Instantiate(playerPrefab.gameObject).GetComponent<Player>();
        player.transform.SetParent(transform);
        player.gameObject.SetActive(true);

        Player.Pool pool = new Player.Pool();
        pool.barSize = 64;
        pool.noteInstanceSize = 1024;
        pool.noteSize = 256;
        Player.PoolIndex poolIndex = new Player.PoolIndex();

        pool.noteInstance = new Player.NoteInstance[pool.noteInstanceSize];
        for (int j = 0; j < pool.noteInstanceSize; ++j)
            pool.noteInstance[j] = new Player.NoteInstance();

        pool.note = new NoteModel[prefabs.Length][];
        for (int j = 0; j < prefabs.Length; ++j)
        {
            pool.note[j] = player.MakePool(pool.noteSize,
                prefabs[j]);
        }

        pool.bar = new BarInstance[pool.barSize];
        poolIndex.bar = poolIndex.note = poolIndex.noteInstance = 0;
        poolIndex.noteModel = new int[prefabs.Length];

        GameObject barPoolParent = new GameObject("BarPool");
        barPoolParent.transform.SetParent(player.transform);
        for (int j = 0; j < pool.barSize; ++j)
        {
            //Debug.Log(j + " - "+ pool.bar.Length);
            pool.bar[j] = Instantiate(barPrefabs[j % 2]).GetComponent<BarInstance>();
            pool.bar[j].transform.SetParent(barPoolParent.transform);
            pool.bar[j].gameObject.SetActive(false);
        }

        player.activeNotes = new List<Player.NoteInstance>();
        player.willRemove = new List<Player.NoteInstance>();
        player.activeBars = new List<BarInstance>();
        player.willRemoveBars = new List<BarInstance>();

        RenderTexture output = player.Initialize(song,
            new Vector2(1024,
                1024),
            pool,
            poolIndex,
            song.data.info.resolution,
            speed);
        outputs.Add(output);

        sessionRenderer.Initialize(outputs.ToArray());
        GC.Collect();
        //GcControl.GC_disable();
    }

    public void EndSession()
    {
        song = null;
        smoothing = null;
        playing = false;
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("pool"))
                Destroy(child.gameObject);
        }

        frameIndex = 0;
        //noteInstancePool = null;
        Destroy(guitarSource.clip);
        Destroy(rhythmSource.clip);
        Destroy(songSource.clip);
        Destroy(guitarSource);
        Destroy(rhythmSource);
        Destroy(songSource);
        guitarSource = rhythmSource = songSource = null;
        time = previousTime = 0;
        tick = 0;
        smoothTick = 0;
        starPowerDuration = 0;
        bpm = smoothBpm = 0;
        syncIndex = 0;
        player.Dispose();
        GC.Collect();
    }

    public void StartPlaying()
    {
        player.cam.gameObject.SetActive(true);
        playing = true;
    }

    private void Sync(float millisecondsPassed)
    {
        beatsPerSecond = bpm / 60d;
        secondsPassed = millisecondsPassed / 1000d;
        beatsPassed = beatsPerSecond * secondsPassed;
        ticksPassed = beatsPassed * song.data.info.resolution;
        if (!double.IsNaN(ticksPassed) && bpm > 0)
            tick += ticksPassed;
        if (syncIndex < song.data.syncTrack.Count) //check if on final sync
        {
            Song.SyncTrack nextSync = song.data.syncTrack[syncIndex];
            if (nextSync.timestamp <= tick)
            {
                switch (nextSync.command)
                {
                    case "B":
                        bpm = nextSync.value * 0.001d;
                        break;
                    case "TS":
                        //???????
                        break;
                }

                syncIndex++;
            }
        }
    }

    public float TickDistanceToMeters(float tickDistance)
    {
        if (song == null)
            return 0;
        return tickDistance / song.data.info.resolution * speed;
    }

    public float MetersToTickDistance(float meters)
    {
        if (song == null)
            return 0;
        return meters / speed * song.data.info.resolution;
    }
}
