using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class Song
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    public bool ready;
    public Data data;

    public Audio audio;

    //Data
    public FileInfo fileInfo;

    [Serializable]
    public class Data
    {
        public Notes notes;
        public Info info;
        public List<SyncTrack> syncTrack;
        public List<SongEvent> events;
    }

    [Serializable]
    public class Info
    {
        public uint resolution;
    }

    [Serializable]
    public class Notes
    {
        public List<Note> easy, medium, hard, expert;
    }

    [Serializable]
    public class Note
    {
        public uint timestamp, duration, fred;
        public bool star, hammerOn;

        public Note(uint _timestamp,
            uint _fred,
            uint _duration,
            bool _star,
            bool _hammerOn)
        {
            timestamp = _timestamp;
            duration = _duration;
            fred = _fred;
            star = _star;
            hammerOn = _hammerOn;
        }
    }

    [Serializable]
    public class SongEvent
    {
        public uint timestamp;
        public string name;

        public SongEvent(uint _timestamp,
            string _name)
        {
            timestamp = _timestamp;
            name = _name;
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
        public AudioClip song, guitar, rhythm, preview;
    }
}