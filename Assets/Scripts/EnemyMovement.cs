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

    //private bool destinationReached;
    private bool isSearchCoroutineRunning;
    private Transform movementTarget;
    private int collidedWithCounter;

    [SerializeField] private float searchTime = 9f;

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
            if (Vector3.Distance(movementTarget.position, enemyTransform.position) <= 0.5f) //ist es schon da?
            {
                //destinationReached = true;
                movementTarget = GetRandomTransform();
                SetMovementTarget(movementTarget);
            }
        }
        else if (currentState == enemyState.followPlayer)
        {
            SetMovementTarget(GameVariables.instance.player.transform);
        }
        else //Seach Player
        {
            if (!isSearchCoroutineRunning)
            {
                StartCoroutine(Search());
            }
        }
        //if ((int)currentState > 0) //ist in State 1 oder 2, d.h. alles außer idle
    }
    IEnumerator Search()
    {
        isSearchCoroutineRunning = true;
        float startTime = Time.realtimeSinceStartup;
        SearchPlayer();

        while (Time.realtimeSinceStartup - startTime < searchTime)
        {
            if (Vector3.Distance(movementTarget.position, enemyTransform.position) <= 1f)
            {
                //destinationReached = true;
                SearchPlayer();
                SetMovementTarget(movementTarget);       
            }
            yield return null;
        }
        currentState = enemyState.idle;
        movementTarget = GetRandomTransform();
        SetMovementTarget(movementTarget);
        isSearchCoroutineRunning = false;
        Debug.Log("Coroutine finished!");
    }

    private void SearchPlayer()
        {
            Vector3 randomPosition = enemyTransform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
            movementTarget.position = randomPosition;
            //navMeshAgent.SetDestination(randomPosition); Das wäre die Lösung für das Versetzungs- Problem, aber dann wird das ganze genau einmal ausgeführt, und der Enemy steht danach dumm rum
            navMeshAgent.SetDestination(movementTarget.position);
            //SetMovementTarget(movementTarget);
            Debug.Log("Enemy Pos: " + enemyTransform.position);
            Debug.Log("Movement Target: " + movementTarget.position);
        }
    

    private void OnTriggerEnter(Collider other)
    {
        
        if(other.tag.Equals("FireflyCollector"))
        {
            collidedWithCounter++;
            if(collidedWithCounter > 1)
            {
                Debug.Log("ded");
            }
            else
            {
                currentState = enemyState.followPlayer;
                Debug.Log("Current State: Follow Player");
            }
            Debug.Log(collidedWithCounter);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("FireflyCollector"))
        {
            collidedWithCounter--;
            if (collidedWithCounter == 0)
            {
                currentState = enemyState.searchPlayer;
                Debug.Log("Current State: Search Player");
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

//Jetziges Problem: Da einer der Cubes in der Liste das movement Target ist, und dieses immer versetzt wird, wird bei jedem SeachPlayer State
//einer der fixed Punkte auf der Map versetzt. Das wäre auch an sich gar nicht schlimm, aber es besteht die Möglichkeit, dass dieser Punkt
//außerhalb der NavMesh Fläche oder in einem nicht begehbaren Objekt gesetzt wird, was beim nächsten Idle State dazu führen könnte,
//dass der Enemy irgendwo nicht hinkommt, und dann "feststeckt", was ein Game Breaking Bug wäre.