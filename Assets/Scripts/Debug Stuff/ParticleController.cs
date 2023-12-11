using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{

    [SerializeField] private GameObject particles;



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            {
                particles.SetActive(false);
            }

        if (Input.GetKeyDown(KeyCode.O))
        {
            particles.SetActive(true);
        }

    }
}
