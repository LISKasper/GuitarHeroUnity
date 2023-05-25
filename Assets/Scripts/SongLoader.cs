using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class SongLoader : MonoBehaviour
{
    public delegate void OnLoaded(Song song);

    public delegate void OnPrepared();

    private static SongLoader instance;
    private readonly object lockObject = new object();
    private string error;

    //threading
    private Song song;

    public static SongLoader Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("SongLoader").AddComponent<SongLoader>();
            return instance;
        }
    }

    public void Load(string chartFile,
        OnLoaded onLoaded)
    {
        StartCoroutine(LoadCoroutine(chartFile,
            onLoaded));
    }

    public void PrepareAudio(Song song,
        OnPrepared onPrepared)
    {
        StartCoroutine(PrepareCoroutine(song,
            onPrepared));
    }

    private IEnumerator LoadCoroutine(string chartFile,
        OnLoaded onLoaded)
    {
        yield return null;
        song = new Song();
        song.ready = false;
        song.fileInfo = new FileInfo(chartFile);
        if (!song.fileInfo.Exists)
            throw new Exception(".chart file not found: " + chartFile);
        Thread thread = new Thread(Parse);
        thread.IsBackground = true;
        yield return null;
        thread.Start();
        //Parse();
        while (true)
        {
            lock (lockObject)
            {
                if (song.ready)
                    break;
            }

            yield return null;
        }

        if (error != null)
            throw new Exception(error);
        onLoaded(song);
    }

    private IEnumerator PrepareCoroutine(Song song,
        OnPrepared onPrepared)
    {
        Debug.Log("Loading song");
        yield return null;
        FileInfo songFileInfo = new FileInfo(song.fileInfo.Directory.FullName + "/song.ogg");
        if (songFileInfo.Exists)
        {
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(songFileInfo.FullName,
                       AudioType.OGGVORBIS))
            {
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.LogError(uwr.error);
                    yield break;
                }

                yield return null;
                song.audio = DownloadHandlerAudioClip.GetContent(uwr);
            }
        }

        Debug.Log("Audio loaded");
        onPrepared();
    }

    private void Parse()
    {
        string gotError = null;
        try
        {
            string fullFileName;
            lock (lockObject)
            {
                fullFileName = song.fileInfo.FullName;
            }

            string[] chart = File.ReadAllLines(fullFileName);
            List<Song.Note> notes = new List<Song.Note>();
            List<Song.SyncTrack> syncTrack = new List<Song.SyncTrack>();
            uint resolution = 0;
            for (int i = 0; i < chart.Length; ++i)
            {
                if (chart[i].Contains("[Song]"))
                {
                    i = LoadChartSong(out resolution,
                        chart,
                        i);
                    continue;
                }

                if (chart[i].Contains("[SyncTrack]"))
                {
                    i = LoadChartSyncTrack(syncTrack,
                        chart,
                        i);
                    continue;
                }

                if (chart[i].Contains("[Chart]"))
                {
                    i = LoadChartNotes(chart,
                        i,
                        notes,
                        resolution);
                }
            }

            Song.Data data = new Song.Data();
            data.syncTrack = syncTrack;
            data.resolution = resolution;
            data.notes = notes;
            song.data = data;
        }
        catch (Exception e)
        {
            gotError = e.Message + " - " + e.StackTrace;
        }

        lock (lockObject)
        {
            error = gotError;
            song.ready = true;
        }
    }

    private int LoadChartSong(out uint resolution,
        string[] chart,
        int i)
    {
        resolution = 0;
        int timeout = 100000;
        while (i < timeout)
        {
            if (chart[i].Contains("{"))
            {
                //Debug.Log("Start reading song info");
                i++;
                break;
            }

            i++;
        }

        while (i < timeout)
        {
            if (chart[i].Contains("}"))
            {
                //Debug.Log("End reading song info");
                break;
            }

            if (chart[i].Contains("Resolution"))
            {
                resolution = uint.Parse(chart[i].Split(new[] { " = " },
                    StringSplitOptions.None)[1]);
            }

            i++;
        }

        return i;
    }

    private int LoadChartSyncTrack(List<Song.SyncTrack> syncTrack,
        string[] chart,
        int i)
    {
        int timeout = 100000;
        while (i < timeout)
        {
            if (chart[i].Contains("{"))
            {
                //Debug.Log("Start reading SyncTrack");
                i++;
                break;
            }

            i++;
        }

        while (i < timeout)
        {
            if (chart[i].Contains("}"))
                break;
            string line = chart[i];
            if (line.Contains(" = "))
            {
                string[] splitted = line.Split(new[] { " = " },
                    StringSplitOptions.None);
                string[] commandValue = splitted[1].Split(" "[0]);
                syncTrack.Add(new Song.SyncTrack(uint.Parse(splitted[0]),
                    commandValue[0],
                    uint.Parse(commandValue[1])));
            }

            i++;
        }

        return i;
    }

    private int LoadChartNotes(string[] chart,
        int i,
        List<Song.Note> list,
        uint resolution)
    {
        int timeout = 100000;
        while (i < timeout)
        {
            if (chart[i].Contains("{"))
            {
                //Debug.Log("Start reading Notes");
                i++;
                break;
            }

            i++;
        }

        while (i < timeout)
        {
            if (chart[i].Contains("}"))
            {
                //Debug.Log("End reading Notes");
                break;
            }

            string line = chart[i];
            if (line.Contains(" = "))
            {
                string[] splitted = line.Split(new[] { " = " },
                    StringSplitOptions.None);
                string[] noteSplitted = splitted[1].Split(" "[0]);
                uint timestamp = uint.Parse(splitted[0]);
                if (noteSplitted[0] == "N")
                {
                    uint fred = uint.Parse(noteSplitted[1]);

                    if (uint.Parse(noteSplitted[1]) < 5)
                    {
                        list.Add(new Song.Note(timestamp,
                            fred,
                            uint.Parse(noteSplitted[2])));
                    }
                }
            }

            i++;
        }

        return i;
    }
}
