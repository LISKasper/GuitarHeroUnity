using UnityEngine;

public class StartCamera : MonoBehaviour
{
    public float startRot, finalRot;
    public AudioSource audioSource;
    private bool skippedFirstFrame;
    private float t;

    private void Update()
    {
        if (!skippedFirstFrame)
        {
            skippedFirstFrame = true;
            return;
        }

        transform.localEulerAngles = new Vector3(Mathf.LerpUnclamped(startRot,
                finalRot,
                ElasticEaseOut(t)),
            0,
            0);
        t = Mathf.Min(t + Time.deltaTime * 0.5f,
            1);
    }

    private void OnEnable()
    {
        transform.localEulerAngles = new Vector3(startRot,
            0,
            0);
        audioSource.Play();
        skippedFirstFrame = false;
        t = 0;
    }

    public static float ElasticEaseOut(float p)
    {
        return Mathf.Sin(-13f * Mathf.PI * 0.5f * (p + 1f)) * Mathf.Pow(2f,
            -10f * p) + 1f;
    }
}