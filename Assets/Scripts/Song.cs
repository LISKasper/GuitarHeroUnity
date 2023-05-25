using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class Song
{
    public bool ready;
    public Data data;

    public Audio audio;

    //Data
    public FileInfo fileInfo;

    [Serializable]
    public class Data
    {
        public List<Note> notes;
        public Info info;
        public List<SyncTrack> syncTrack;
    }

    [Serializable]
    public class Info
    {
        public uint resolution;
    }

    [Serializable]
    public class Note
    {
        public uint timestamp, duration, fred;

        public Note(uint _timestamp,
            uint _fred,
            uint _duration)
        {
            timestamp = _timestamp;
            duration = _duration;
            fred = _fred;
        }
    }

    [Serializable]
    public class SyncTrack
    {
        public uint timestamp, value;
        public string command;

        public SyncTrack(uint _timestamp,
            string _command,
            uint _value)
        {
            timestamp = _timestamp;
            command = _command;
            value = _value;
        }
    }

    [Serializable]
    public class Audio
    {
        public AudioClip song, guitar, rhythm;
    }
}
