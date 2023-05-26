using UnityEngine;

public class Animation2D : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite[] frames;
    public bool loop;
    public bool flip;
    public int fps = 60;
    public int index;
    public float seconds;

    public void Reset()
    {
        OnEnable();
    }

    private void Update()
    {
        seconds += Time.deltaTime;
        index = Mathf.FloorToInt(seconds * fps);
        if (index >= frames.Length)
        {
            if (!loop)
                gameObject.SetActive(false);
        }

        index %= frames.Length;
        spriteRenderer.sprite = frames[index];
    }

    private void OnEnable()
    {
        seconds = 0;
        index = 0;
        if (!flip)
            return;

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }
}
