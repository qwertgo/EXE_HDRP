using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireflySpawner : MonoBehaviour
{
    [SerializeField] FireflyWalk fireflyPrefab;
    [SerializeField] int ringCount;
    [SerializeField] float worldRadius;
    [SerializeField] float innerRadius;

    void Start()
    {
        for(int i = 0; i < ringCount; i++)
        {
            float centerDistance = ((worldRadius - innerRadius) / ringCount * i) + innerRadius;

            Vector3 startFireflyPosition = Vector3.right * centerDistance;
            float randomRotation = Random.Range(0f, 360f);
            int currentRingFireflyAmount = i + 1;
            float rotationPerFirefly = 360f / currentRingFireflyAmount;

            for (int o = 0; o < currentRingFireflyAmount; o++)
            {
                float fireflyRotation = randomRotation + rotationPerFirefly * o;
                Vector3 fireflyPosition = Quaternion.Euler(0, fireflyRotation, 0) * startFireflyPosition;
                FireflyWalk currentFirefly = Instantiate(fireflyPrefab, fireflyPosition, Quaternion.identity);

                if(i%2 == 0)
                {
                    currentFirefly.SetDirection(false);
                }
            }
        }         
    }
}
