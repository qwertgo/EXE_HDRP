using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    enum enemyState { idle, followPlayer, searchPlayer}

    [SerializeField] enemyState currentState;

    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform playerTransform;

    [SerializeField] private Transform enemyTransform;
    [SerializeField] private SphereCollider playerRadius;

    [SerializeField] private SphereCollider deathCollider;
    [SerializeField] private SphereCollider followCollider;

    private bool destinationReached;
    private Transform movementTarget;
    private int collidedWithCounter;

    [SerializeField]
    public List<Transform> transformList = new List<Transform>(); // Liste für Transforms
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        movementTarget = GetRandomTransform();
        SetMovementTarget(movementTarget);
    }
    private void Update()
    {
        if (currentState == enemyState.idle)
        {
            if (Vector3.Distance(movementTarget.position, enemyTransform.position) <= 0.5f)
            {
                destinationReached = true;
                movementTarget = GetRandomTransform();
                SetMovementTarget(movementTarget);
                //Debug.Log("Ich bin imn der if-Anweisung");
            }
        }
        else if (currentState == enemyState.followPlayer)
        {
            Debug.Log("State ist Follow Player");
            SetMovementTarget(GameVariables.instance.player.transform);
        }
        else //Seach Player
        {

        }

        //if ((int)currentState > 0) //ist in State 1 oder 2, d.h. alles außer idle
        //{

        //}


    }
    private void SearchPlayer()
    {
        //Vector3 movementTargetVector3;
        //movementTargetVector3 = transform.position + new Vector3(Random.Range(0f, 10f), 0f, Random.Range(0f, 10f));
        //movementTarget = movementTargetVector3 + transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collided with Player");
        if(other.tag.Equals("FireflyCollector"))
        {
            collidedWithCounter++;
            if(collidedWithCounter > 1)
            {
                //player will die
            }
            else
            {
                currentState = enemyState.followPlayer;
            }
            Debug.Log(collidedWithCounter);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            collidedWithCounter--;
            if (collidedWithCounter == 0)
            {
                currentState = enemyState.searchPlayer;
            }
        }
    }

    internal void SetMovementTarget(Transform movementTarget)
    {
        //movementTarget = GetRandomTransform();
        navMeshAgent.SetDestination(movementTarget.position);

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
            return null;
        }
    }
}
