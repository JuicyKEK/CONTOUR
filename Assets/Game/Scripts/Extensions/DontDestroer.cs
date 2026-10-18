using System;
using UnityEngine;

public class DontDestroer : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
