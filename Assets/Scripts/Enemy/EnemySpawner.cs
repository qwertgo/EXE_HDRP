using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private EnemyMovement enemyPrefab;
    [SerializeField] private NavMeshAgent enemyAgent;
    [SerializeField] private float innerSpawnRadius = 250f;
    [SerializeField] private float outerSpawnRadius = 330f;
    [SerializeField] private int numberOfEnemies = 4;
    private NavMeshQueryFilter filter;

    public static readonly UnityEvent foundPlayer = new ();
    public static readonly UnityEvent lostPlayer = new ();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, innerSpawnRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, outerSpawnRadius);
    }

    private void Start()
    {
        
        filter.areaMask = enemyAgent.areaMask;
        filter.agentTypeID = enemyAgent.agentTypeID;

        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 position = GetRandomPosition(innerSpawnRadius, i, 0);

            EnemyMovement enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);

            List<Vector3> idleDestinationPoints = new List<Vector3>();
            for (int o = 0; o < 20; o++)
            {
                Vector3 pos = GetRandomPosition(80, i, 15);
                idleDestinationPoints.Add(pos);
            }

            enemy.name = "Enemy " + i;
            enemy.idleDestinationPoints = idleDestinationPoints;
        }
    }

    //This Method generates a random position by rotating a vector by a random
    //value and the scaling the vector by a second random value
    //this makes sure the positions stay in a circular area
    private Vector3 GetRandomPosition(float startRadius, int i, float sideBuffer)
    {
        float indexAngle = 360f / numberOfEnemies * i;
        
        float maxRandomAngle = 360f / numberOfEnemies - sideBuffer;
        float positionAngle = Random.Range(0, maxRandomAngle) + sideBuffer / 2;
        positionAngle += indexAngle;
        
        float distanceFromCenter = Random.Range(startRadius, outerSpawnRadius);
        Vector3 position = Quaternion.AngleAxis(positionAngle, Vector3.up) * Vector3.forward * distanceFromCenter;
            
        if(NavMesh.SamplePosition(position, out NavMeshHit hit, 30f, filter))
            position = hit.position;
        else
        {
            Debug.Log("Could not find NavMesh Position");
            return Vector3.zero;
        }

        return position;
    }
}
