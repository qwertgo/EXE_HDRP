using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FireflySpawner : MonoBehaviour
{
    [SerializeField] FireflyWalk fireflyPrefab;
    [SerializeField] int ringCount;
    [SerializeField] float outerRadius;
    [SerializeField] float innerRadius;

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, innerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, outerRadius);
    }

    void Start()
    {
        float halfRingWidth = (outerRadius - innerRadius) / ringCount / 2;

        //for each ring
        for(int i = 0; i < ringCount; i++)
        {
            float centerDistance = ((outerRadius - innerRadius) / ringCount * i) + innerRadius + halfRingWidth;

            Vector3 fireflyStartPosition = Vector3.right * centerDistance;
            float randomRotation = Random.Range(0f, 360f);
            int fireflyAmountInRing = (int)Mathf.Pow(2, i + 2);
            float rotationPerFirefly = 360f / fireflyAmountInRing;

            //for each firefly inside the ring
            for (int o = 0; o < fireflyAmountInRing; o++)
            {
                float fireflyRotation = randomRotation + rotationPerFirefly * o;
                Vector3 fireflyPosition = Quaternion.Euler(0, fireflyRotation, 0) * fireflyStartPosition;
                FireflyWalk currentFirefly = Instantiate(fireflyPrefab, fireflyPosition, Quaternion.identity);

                bool moveRight = i % 2 == 0;
                currentFirefly.SetFireflyValues(halfRingWidth / 2, moveRight);

                currentFirefly.transform.parent = transform;
            }
        }         
    }
}
