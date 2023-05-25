using System;
using UnityEngine;

[Serializable]
public class PlayerInput
{
    public bool[] fred = new bool[5];

    public void Update()
    {
        fred[0] = Input.GetKey(KeyCode.Alpha1);
        fred[1] = Input.GetKey(KeyCode.Alpha2);
        fred[2] = Input.GetKey(KeyCode.Alpha3);
        fred[3] = Input.GetKey(KeyCode.Alpha4);
        fred[4] = Input.GetKey(KeyCode.Alpha5);
    }
}
