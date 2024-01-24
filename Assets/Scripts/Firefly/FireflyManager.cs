using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FireflyManager : MonoBehaviour
{
    public static event Action<Vector2> updatePosition;
    
    [SerializeField] FireflyWalk fireflyPrefab;
    [SerializeField] int ringCount;
    [SerializeField] float outerRadius;
    [SerializeField] float innerRadius;

    private IEnumerator fireFlyCollectSound;
    private static int lastCollectedFireflies;
    private static float pitchOffset;

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
        int spawnedFireflies = 0;

        //for each ring
        for(int i = 0; i < ringCount; i++)
        {
            float centerDistance = ((outerRadius - innerRadius) / ringCount * i) + innerRadius + halfRingWidth;

            Vector3 fireflyStartPosition = Vector3.right * centerDistance;
            float randomRotation = Random.Range(0f, 360f);
            int fireflyAmountInRing = (int)Mathf.Pow(2, i + 1);
            float rotationPerFirefly = 360f / fireflyAmountInRing;

            //for each firefly inside the ring
            for (int o = 0; o < fireflyAmountInRing; o++)
            {
                float fireflyRotation = randomRotation + rotationPerFirefly * o;
                Vector3 fireflyPosition = Quaternion.Euler(0, fireflyRotation, 0) * fireflyStartPosition;
                FireflyWalk currentFirefly = Instantiate(fireflyPrefab, fireflyPosition, Quaternion.identity);

                bool moveRight = i % 2 == 0;
                currentFirefly.SetFireflyValues(halfRingWidth * .3f, moveRight, spawnedFireflies);
                currentFirefly.transform.parent = transform;

                spawnedFireflies++;
            }
        }

        // fireFlyCollectSound = CollectSoundPingPong();
    }
    void Update()
    {
        float xPos = Mathf.Sin(Time.time * 2) ;
        float yPos = Mathf.Sin(Time.time * 4) * .5f;
            
        updatePosition?.Invoke(new Vector2(xPos, yPos));
    }

    public static IEnumerator PlayStaticFireflySound(AudioSource audioSource, AudioClip audioClip)
    {
        if (lastCollectedFireflies == 0)
            pitchOffset = Random.Range(-.2f, .2f);

        float pitch = 1 + pitchOffset + lastCollectedFireflies / 10f;
        lastCollectedFireflies++;
        int tmpCollectedFireflies = lastCollectedFireflies;
        
        audioSource.PlayAudioPitched(audioClip, pitch);
        
        yield return new WaitForSeconds(2f);

        if (tmpCollectedFireflies == lastCollectedFireflies)
            lastCollectedFireflies = 0;
    }
}
