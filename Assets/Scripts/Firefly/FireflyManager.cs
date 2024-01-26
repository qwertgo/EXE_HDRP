using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FireflyManager : MonoBehaviour
{
    public static event Action<Vector2> updatePosition;
    
    [SerializeField] FireflyDynamic dynamicFireflyPrefab;
    [SerializeField] private FireflyStatic staticFireflyPrefab;
    [SerializeField] private float outerRadius;
    [SerializeField] private float innerRadius;
    [SerializeField] private float fireflySpeed = 2f;
    [SerializeField] private bool useOldMethod;
    [SerializeField] private bool debugMode;
    
    [Header("New Method")]
    [SerializeField] private float fireFlyCount = 126;
    
    [Header("Old Method")]
    [SerializeField] private int ringCount;

    private IEnumerator fireFlyCollectSound;
    private NavMeshQueryFilter  navMeshFilter;
    private static int firefliesCollectedInLastSecond;
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
        if(useOldMethod)
            OldSpawnMethod();
        else
            NewSpawnMethod();

    }

    private void NewSpawnMethod()
    {
        NavMeshAgent fireflyAgent = dynamicFireflyPrefab.GetComponent<NavMeshAgent>();
        navMeshFilter.areaMask = fireflyAgent.areaMask;
        navMeshFilter.agentTypeID = fireflyAgent.agentTypeID;

        int spawnedFireflies = 0;

        for (int i = 0; i < fireFlyCount; i++)
        {
            Vector3 position = GetRandomPosition();
            
            if (debugMode && i % 2 > 0)
                Instantiate(staticFireflyPrefab, position, Quaternion.identity, transform);
            else
            {
                bool moveRight = i % 2 == 0;

                FireflyDynamic currentFirefly = Instantiate(dynamicFireflyPrefab, position, Quaternion.identity, transform);
                currentFirefly.SetFireflyValues(10, moveRight, spawnedFireflies);
            }
            
            
            spawnedFireflies++;
        }
    }

    private Vector3 GetRandomPosition()
    {
        float randomRotation = Random.Range(0, 360);
        float randomScale = Random.Range(innerRadius, outerRadius);

        Vector3 position = Quaternion.Euler(0, randomRotation, 0) * Vector3.right * randomScale;
        NavMesh.SamplePosition(position, out NavMeshHit hit, 20, navMeshFilter);

        return hit.hit ? hit.position : Vector3.zero;
    }
    
    private void OldSpawnMethod(){
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
                FireflyDynamic currentFirefly = Instantiate(dynamicFireflyPrefab, fireflyPosition, Quaternion.identity, transform);

                bool moveRight = i % 2 == 0;
                currentFirefly.SetFireflyValues(halfRingWidth * .3f, moveRight, spawnedFireflies);

                spawnedFireflies++;
            }
        }
    }
    void Update()
    {
        float xPos = Mathf.Sin(Time.time * 2 * fireflySpeed) ;
        float yPos = Mathf.Sin(Time.time * 4 * fireflySpeed) * .5f;
            
        updatePosition?.Invoke(new Vector2(xPos, yPos));
    }

    public static IEnumerator PlayStaticFireflySound(AudioSource audioSource, AudioClip audioClip)
    {
        if (firefliesCollectedInLastSecond == 0)
            pitchOffset = Random.Range(-.2f, .2f);

        float pitch = 1 + pitchOffset + firefliesCollectedInLastSecond / 10f;
        firefliesCollectedInLastSecond++;
        int tmpCollectedFireflies = firefliesCollectedInLastSecond;
        
        audioSource.PlayAudioPitched(audioClip, pitch);
        
        yield return new WaitForSeconds(2f);

        if (tmpCollectedFireflies == firefliesCollectedInLastSecond)
            firefliesCollectedInLastSecond = 0;
    }
}
