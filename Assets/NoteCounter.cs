﻿using UnityEngine;

public class NoteCounter : MonoBehaviour
{
    public NumberRenderer[] numberRenderer;
    public GameObject musicNote;
    public int number = 2345;
    public float showCounterAnimation;
    public Vector3 hidePosition, showPosition;
    private readonly int[] previousFrame = { 0, 0, 0, 0 };
    private readonly int[] show = { 0, 0, 0, 0 };

    public void Initialize()
    {
        number = 0;
        for (int i = 0; i < 4; ++i)
            numberRenderer[i].time = 1;
        gameObject.SetActive(false);
    }

    public void UpdateCounter()
    {
        show[3] = number % 10;
        show[2] = number / 10 % 10;
        show[1] = number / 100 % 10;
        show[0] = number / 1000 % 10;
        musicNote.SetActive(number < 1000);
        for (int i = 0; i < 4; ++i)
        {
            if (show[i] != previousFrame[i])
                numberRenderer[i].time = 0.5f;
            numberRenderer[i].mySpriteRenderer.sprite = numberRenderer[i].number[show[i]];
            previousFrame[i] = show[i];
            numberRenderer[i].UpdateNumber();
        }

        showCounterAnimation = Mathf.Min(showCounterAnimation + Time.deltaTime * 2f,
            1);
        transform.localPosition = Vector3.LerpUnclamped(hidePosition,
            showPosition,
            elastic(showCounterAnimation));
        gameObject.SetActive(true);
        //else
        //{
        //    showCounterAnimation = Mathf.Max(showCounterAnimation - Time.deltaTime * 3f,
        //        0);
        //    transform.localPosition = Vector3.LerpUnclamped(hidePosition,
        //        showPosition,
        //        BackEaseOut(showCounterAnimation));
        //    if (showCounterAnimation == 0)
        //        gameObject.SetActive(false);
        //    //showCounterAnimation = Mathf.Max(showCounterAnimation - Time.deltaTime, 0f);
        //}
    }

    public static float elastic(float p)
    {
        return Mathf.Sin(-13 * Mathf.PI * 0.5f * (p + 1)) * Mathf.Pow(2,
            -10 * p) + 1;
    }

    public static float BackEaseOut(float p)
    {
        float f = 1 - p;
        return 1 - (f * f * f - f * Mathf.Sin(f * Mathf.PI));
    }
}
