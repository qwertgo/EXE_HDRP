using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform playerTransform;
    //[SerializeField] private Transform cubeTransform;
    [SerializeField] private Transform enemyTransform;
    private bool destinationReached;
    private Transform movementTarget;

    [SerializeField]
    public List<Transform> transformList = new List<Transform>(); // Liste für Transforms
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        SetMovementTarget();
    }
    private void Update()
    {
        
        if (Vector3.Distance(movementTarget.position , enemyTransform.position) <= 0.5f)
        {
            destinationReached = true;
            SetMovementTarget();
        }
    }

    internal void SetMovementTarget()
    {
        movementTarget = GetRandomTransform();
        navMeshAgent.SetDestination(movementTarget.position);
        Debug.Log("Set New Movement Target");
    }
    public Transform GetRandomTransform()
    {
        if (transformList.Count > 0)
        {
            int randomIndex = Random.Range(0, transformList.Count); // Zufälliger Index
            return transformList[randomIndex]; // Gib den zufälligen Transform zurück
        }
        else
        {
            Debug.LogError("Die Transform-Liste ist leer!");
            return null;
        }
    }
}
