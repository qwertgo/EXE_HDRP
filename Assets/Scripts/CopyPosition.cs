using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyPosition : MonoBehaviour
{
    [SerializeField] float yOffset = 0;
    public Transform transformToCopyPositionFrom;

    private void Update()
    {
        transform.position = transformToCopyPositionFrom.position + new Vector3(0, yOffset, 0);
    }
}
