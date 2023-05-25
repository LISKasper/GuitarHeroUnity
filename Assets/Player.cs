using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerInput playerInput;
    public int layerMask;
    public NoteCounter noteCounter;
    public RenderTexture output;
    public Song song;
    public List<Song.Note> notes;
    public Transform cam, board;
    public PoolIndex index;
    public Pool pool;
    public List<NoteInstance> activeNotes, willRemove;
    public List<BarInstance> activeBars, willRemoveBars;
    public Line nextLine;
    public Animation2D[] flame;
    public GameObject[] fredHighlight;
    public uint resolution;
    public float speed;
    public uint nextBar;
    private List<LongNote> longNotes;

    public RenderTexture Initialize(
        Song _song,
        Vector2 _output,
        Pool _pool,
        PoolIndex _poolIndex,
        uint _resolution,
        float _speed)
    {
        layerMask = 1 << 10;
        song = _song;
        notes = song.data.notes;

        pool = _pool;
        index = new PoolIndex();
        resolution = _resolution;
        nextBar = resolution;
        speed = _speed;
        index = _poolIndex;
        longNotes = new List<LongNote>();
        activeNotes = new List<NoteInstance>();
        activeBars = new List<BarInstance>();
        willRemove = new List<NoteInstance>();
        willRemoveBars = new List<BarInstance>();
        nextLine = new Line
        {
            note = new List<NoteInstance>(),
            fred = new bool[5]
        };

        noteCounter.Initialize();

        output = new RenderTexture(Mathf.CeilToInt(_output.x),
            Mathf.CeilToInt(_output.y),
            16,
            RenderTextureFormat.ARGB32);
        cam.GetComponent<Camera>().targetTexture = output;
        cam.GetComponent<Camera>().cullingMask = layerMask;
        SetLayerRecursive(transform,
            10);

        playerInput = new PlayerInput();

        return output;
    }

    public void GetInput()
    {
        playerInput.Update();
    }

    public void SetLayerRecursive(Transform t,
        int layerMask)
    {
        foreach (Transform child in t)
        {
            //Debug.Log(child.name);
            child.gameObject.layer = layerMask;
            SetLayerRecursive(child,
                layerMask);
        }
    }

    public NoteModel[] MakePool(int size,
        GameObject prefab)
    {
        NoteModel[] newPool = new NoteModel[size];
        GameObject poolObject = new GameObject("Pool " + prefab.name);
        poolObject.transform.SetParent(transform);
        for (int i = 0; i < newPool.Length; ++i)
        {
            GameObject g = Instantiate(prefab);
            g.SetActive(false);
            g.transform.SetParent(poolObject.transform);
            newPool[i] = g.GetComponent<NoteModel>();
            if (newPool[i].line != null)
                newPool[i].materialInstance = newPool[i].line.material;
        }

        return newPool;
    }

    public void SpawnObjects(double tick)
    {
        if (index.note >= notes.Count)
            return; //end of song
        Song.Note nextNote = notes[index.note];
        if (!(nextNote.timestamp < tick + MetersToTickDistance(4f))) //spawn tick + 10 seconds?
            return;

        try
        {
            bool longNote = nextNote.duration > 0;
            int poolNumber = (int)nextNote.fred + (longNote ? 5 : 0);
            NoteModel noteModel = pool.note[poolNumber][index.noteModel[poolNumber] % pool.noteSize];
            GameObject newNote = noteModel.gameObject;
            noteModel.myTransform.rotation = cam.rotation;
            newNote.SetActive(true);
            NoteInstance noteInstance = pool.noteInstance[index.noteInstance % pool.noteInstanceSize];
            index.noteInstance++;
            noteInstance.Update(noteModel,
                nextNote.timestamp,
                nextNote.fred,
                nextNote.duration);
            activeNotes.Add(noteInstance);

            index.note++;
            index.noteModel[poolNumber]++;
            SpawnObjects(tick);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + " - " + e.StackTrace);
        }
    }

    public void Dispose()
    {
        song = null;
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("pool"))
                Destroy(child.gameObject);
        }

        cam.gameObject.SetActive(false);
        pool = null;
        index = null;
        Destroy(gameObject);
    }

    public void UpdateObjects(double smoothTick,
        Sprite[] noteSprites,
        int frameIndex)
    {
        Vector3 boardPosition = board.localPosition;
        boardPosition.z = (float)(TickDistanceToMeters(smoothTick) % 2 * -1f + 4);
        if (!float.IsNaN(boardPosition.z))
            board.localPosition = boardPosition;
        foreach (NoteInstance noteInstance in activeNotes)
        {
            Transform noteTransform = noteInstance.noteModel.transform;
            Vector3 pos = noteTransform.localPosition;

            double tickDistance = noteInstance.timestamp - smoothTick;
            double distanceInMeters = TickDistanceToMeters(tickDistance);
            pos.z = (float)distanceInMeters;
            noteTransform.localPosition = pos;
            double endOfNoteDistance = tickDistance + noteInstance.duration;
            double endOfNoteInMeters = TickDistanceToMeters(endOfNoteDistance);
            if (noteInstance.duration > 0)
            {
                //update long note length
                float length = (float)(endOfNoteInMeters - distanceInMeters);
                noteInstance.noteModel.SetLength(length);
            }

            //show correct sprite
            SpriteRenderer spriteRenderer = noteInstance.noteModel.spriteRenderer;
            spriteRenderer.sprite = noteSprites[noteInstance.fred];

            if (endOfNoteInMeters < -1 &&
                longNotes.All(longNote => noteInstance != longNote.noteInstance)) //out of view 
                willRemove.Add(noteInstance);
        }
    }

    public void CreateBar(double tick)
    {
        if (!(nextBar < tick + MetersToTickDistance(4f))) //spawn tick + 10 seconds?
            return;

        BarInstance newBar = pool.bar[index.bar % pool.barSize];
        index.bar++;
        newBar.gameObject.SetActive(true);
        newBar.timestamp = nextBar;
        activeBars.Add(newBar);
        nextBar += resolution;
    }

    public void UpdateActiveBars(double smoothTick)
    {
        foreach (BarInstance barInstance in activeBars)
        {
            double tickDistance = barInstance.timestamp - smoothTick;
            double distanceInMeters = TickDistanceToMeters(tickDistance);
            Vector3 pos = barInstance.myTransform.localPosition;
            pos.z = (float)distanceInMeters;
            barInstance.myTransform.localPosition = pos;
            if (tickDistance < 0)
                willRemoveBars.Add(barInstance);
        }

        for (int i = willRemoveBars.Count - 1; i > -1; --i)
        {
            activeBars.Remove(willRemoveBars[i]);
            willRemoveBars[i].gameObject.SetActive(false);
            willRemoveBars.RemoveAt(i);
        }
    }

    public void RegisterAndRemove(double smoothTick)
    {
        bool missedThisFrame = false;
        //highlighting player input
        for (int i = 0; i < playerInput.fred.Length; ++i)
            fredHighlight[i].SetActive(playerInput.fred[i]);

        double window = resolution / 8.0f;

        //check if new line needs to be created
        if (!nextLine.available)
        {
            //check if notes are available
            //only create line when it is a bit closer 
            if (activeNotes.Count > 0 && activeNotes[0].timestamp < smoothTick + window * 2)
            {
                nextLine.timestamp = activeNotes[0].timestamp;
                //check if more notes are on the same timestamp
                for (int i = 0; i < 5; ++i)
                {
                    if (i >= activeNotes.Count)
                        break; //out of range

                    if (Math.Abs(activeNotes[i].timestamp - nextLine.timestamp) > 0.001)
                        break; //different line

                    if (activeNotes[i].duration > 0)
                    {
                        double endOfNote = smoothTick + activeNotes[i].duration;
                        longNotes.Add(new LongNote(activeNotes[i].timestamp,
                            activeNotes[i].fred,
                            endOfNote,
                            activeNotes[i]));
                    }

                    nextLine.note.Add(activeNotes[i]);
                }

                nextLine.lowestFred = 4;
                foreach (NoteInstance note in nextLine.note)
                {
                    uint fred = note.fred;
                    nextLine.lowestFred = Mathf.Min(nextLine.lowestFred,
                        (int)fred);
                    nextLine.fred[fred] = true;
                }

                nextLine.available = true;
            }
        }

        if (longNotes.Count > 0)
        {
            List<LongNote> toRemove = new List<LongNote>();
            foreach (LongNote note in longNotes)
            {
                if (note.endOfNoteInMeters < smoothTick)
                    toRemove.Add(note);

                if (playerInput.fred[note.fred])
                {
                    note.SetHeld(true);

                    flame[note.fred].gameObject.SetActive(true);
                    flame[note.fred].Reset();
                    flame[note.fred].seconds = 1f / 60f * 8f;
                }
                else
                    note.SetHeld(false);

                if (note.timestamp - smoothTick < -window && !note.held)
                    toRemove.Add(note);
            }

            foreach (LongNote note in toRemove)
            {
                Debug.Log("Removing note");
                willRemove.Add(note.noteInstance);
                longNotes.Remove(note);
            }

            toRemove.Clear();
        }

        //Check if next line is available now
        if (nextLine.available)
        {
            bool correctColors = true;
            for (int i = nextLine.lowestFred; i < playerInput.fred.Length; ++i)
                correctColors &= playerInput.fred[i] == nextLine.fred[i];
            if (correctColors)
                nextLine.state = Line.State.Success;

            if (nextLine.timestamp - smoothTick < -window)
                nextLine.state = Line.State.Failed;

            //Check if next line is succes or fail
            if (nextLine.state == Line.State.Failed)
            {
                foreach (NoteInstance note in nextLine.note)
                {
                    if (longNotes.Any(longNote => note == longNote.noteInstance))
                        continue;

                    willRemove.Add(note);
                    missedThisFrame = true;
                }

                nextLine.Clear();
                noteCounter.number = 0;
            }

            if (nextLine.state == Line.State.Success)
            {
                foreach (NoteInstance note in nextLine.note)
                {
                    uint fred = note.fred;
                    if (longNotes.All(longNote => note != longNote.noteInstance))
                        willRemove.Add(note);

                    flame[fred].gameObject.SetActive(true);
                    flame[fred].Reset();
                    flame[fred].seconds = 1f / 60f * 8f;
                }

                nextLine.Clear();
                noteCounter.number++;
            }
        }

        for (int i = willRemove.Count - 1; i >= 0; --i)
        {
            activeNotes.Remove(willRemove[i]);
            willRemove[i].noteModel.transform.gameObject.SetActive(false);
            willRemove.RemoveAt(i);
        }

        //update note counter
        //noteCounter.gameObject.SetActive(noteCounter.number > 30);
        noteCounter.UpdateCounter();

        //if missed a note, do function again to check if next note is hit instead. but break combo
        if (missedThisFrame)
            RegisterAndRemove(smoothTick);
    }

    public double TickDistanceToMeters(double tickDistance)
    {
        return tickDistance / resolution * speed;
    }

    private double MetersToTickDistance(double meters)
    {
        return meters / speed * resolution;
    }

    [Serializable]
    public class Pool
    {
        public int noteSize;
        public int barSize;
        public int noteInstanceSize;
        public NoteInstance[] noteInstance;
        public BarInstance[] bar;
        public NoteModel[][] note;
    }

    [Serializable]
    public class PoolIndex
    {
        public int note;
        public int[] noteModel;
        public int bar;
        public int noteInstance;
    }

    [Serializable]
    public class NoteInstance
    {
        public NoteModel noteModel;
        public uint timestamp;
        public uint fred;
        public uint duration;

        public void Update(NoteModel _noteModel,
            uint _timestamp,
            uint _fred,
            uint _duration
        )
        {
            noteModel = _noteModel;
            timestamp = _timestamp;
            fred = _fred;
            duration = _duration;
        }
    }

    [Serializable]
    public class Line
    {
        public enum State
        {
            Unknown,
            Success,
            Failed
        }

        public bool available;
        public int lowestFred;
        public double timestamp;
        public bool[] fred;
        public List<NoteInstance> note;
        public State state;

        public void Clear()
        {
            state = State.Unknown;
            available = false;
            timestamp = 0;
            lowestFred = 4;
            note.Clear();
            for (int i = 0; i < fred.Length; ++i)
                fred[i] = false;
        }
    }
}

public struct LongNote
{
    public float timestamp;
    public readonly uint fred;
    public readonly double endOfNoteInMeters;
    public readonly Player.NoteInstance noteInstance;
    public bool held;

    public void SetHeld(bool held)
    {
        this.held = held;
    }

    public LongNote(float timestamp,
        uint fred,
        double endOfNoteInMeters,
        Player.NoteInstance noteInstance)
    {
        this.timestamp = timestamp;
        this.fred = fred;
        this.endOfNoteInMeters = endOfNoteInMeters;
        this.noteInstance = noteInstance;
        held = false;
    }
}
