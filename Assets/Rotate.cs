using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 rotation;

    private void Update()
    {
        transform.Rotate(rotation * Time.deltaTime);
    }
}