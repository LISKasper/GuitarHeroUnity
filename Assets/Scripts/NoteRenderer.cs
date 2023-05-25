using System;
using UnityEngine;

public class NoteRenderer : MonoBehaviour
{
    public SpriteData spriteData;

    [Serializable]
    public class SpriteData
    {
        public FredSpriteData[] fred;
    }

    [Serializable]
    public class FredSpriteData
    {
        public Sprite normal, hammerOn;
        public Sprite[] star, starHammerOn;
    }
}