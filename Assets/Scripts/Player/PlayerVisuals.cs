using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private float yOffset;
    [SerializeField] Rigidbody rb;

    void Update()
    {
        transform.position = rb.transform.position + new Vector3(0, yOffset);
    }
}
