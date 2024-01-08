using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Vector3 = UnityEngine.Vector3;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyMovement enemyPrefab;
    [SerializeField] private NavMeshAgent enemyAgent;
    private NavMeshQueryFilter filter;

    private readonly UnityEvent enemyFoundPlayer = new ();
    private readonly UnityEvent enemyLostPlayer = new ();

    void Start()
    {
        int numberOfEnemies = 4;
        filter.areaMask = enemyAgent.areaMask;
        filter.agentTypeID = enemyAgent.agentTypeID;

        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 position = GetRandomPosition(250, i, numberOfEnemies, 0);

            EnemyMovement enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);

            List<Vector3> idleDestinationPoints = new List<Vector3>();
            for (int o = 0; o < 20; o++)
            {
                Vector3 pos = GetRandomPosition(100, i, numberOfEnemies, 20);
                idleDestinationPoints.Add(pos);
            }

            enemy.name = "Enemy " + i;
            enemy.idleDestinationPoints = idleDestinationPoints;
            enemy.SetEvents(enemyFoundPlayer, enemyLostPlayer);
        }
    }

    //This Method generates a random position by rotating a vector by a random
    //value and the scaling the vector by a second random value
    //this makes sure the positions stay in a circular area
    Vector3 GetRandomPosition(float startRadius, int i, int numberOfEnemies, float sideBuffer)
    {
        float indexAngle = 360f / numberOfEnemies * i;
        
        float maxRandomAngle = 360f / numberOfEnemies - sideBuffer;
        float positionAngle = Random.Range(0, maxRandomAngle) + sideBuffer / 2;
        positionAngle += indexAngle;
        
        float distanceFromCenter = Random.Range(startRadius, 330f);
        Vector3 position = Quaternion.AngleAxis(positionAngle, Vector3.up) * Vector3.forward * distanceFromCenter;
            
        if(NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, filter))
            position = hit.position;
        else
        {
            Debug.Log("Could not find NavMesh Position");
            return Vector3.zero;
        }

        return position;
    }
}
