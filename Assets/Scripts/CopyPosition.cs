using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyPosition : MonoBehaviour
{
    [SerializeField] private Transform transformToCopyPositionFrom;

    private void Update()
    {
        transform.position = transformToCopyPositionFrom.position;
    }
}
