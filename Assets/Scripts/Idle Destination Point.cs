using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleDestinationPoint : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
}
