using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    void Update()
    {
        Vector3 pos = transform.position;

        transform.position = new Vector3(pos.x, 0, pos.z);
    }
}
