using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireflySpawner : MonoBehaviour
{
    [SerializeField] FireflyWalk fireflyPrefab;
    [SerializeField] int ringCount;
    [SerializeField] float worldRadius;
    [SerializeField] float innerRadius;

    List <FireflyWalk> allFireflies = new ();

    void Start()
    {
        for(int i = 0; i < ringCount; i++)
        {
            float centerDistance = ((worldRadius - innerRadius) / ringCount * i) + innerRadius;

            Vector3 fireFlyPosition = Vector3.right * centerDistance;
            float randomRotation = Random.Range(0f, 360f);
            fireFlyPosition = Quaternion.Euler(0, randomRotation, 0) * fireFlyPosition;
            FireflyWalk currentFirefly = Instantiate(fireflyPrefab, fireFlyPosition, Quaternion.identity);

            if(i%2 == 0)
            {
                currentFirefly.SetDirection(false);
            }

            allFireflies.Add(currentFirefly);
        }         
    }
}
