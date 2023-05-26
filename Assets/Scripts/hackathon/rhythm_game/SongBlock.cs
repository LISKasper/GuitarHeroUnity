using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SongBlock : MonoBehaviour
{
    public SongSelect songSelect;
    public Text text;
    public FileInfo fileInfo;

    public void Play()
    {
        songSelect.LoadSong(fileInfo);
    }
}