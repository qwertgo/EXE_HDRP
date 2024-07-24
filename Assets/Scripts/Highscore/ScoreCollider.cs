using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using static HighScoreCounter;

public class ScoreCollider : MonoBehaviour
{

    [SerializeField] LayerMask obstacleEnemyLayerMask;

    private SphereCollider ownCollider;
    private SphereCollider playerCollider;
    private HighScoreCounter highScoreCounter => GameVariables.instance.highScoreCounter;

    private int closeObjectsCount;
    private int scoreAdditionsToGive;

    private void Start() {
        ownCollider = GetComponent<SphereCollider>();
        playerCollider = transform.parent.GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other) {
        int otherLayer = other.gameObject.layer;

        if(IsObstacleOrEnemy(otherLayer))
        {
            closeObjectsCount++;
            scoreAdditionsToGive++;
            Debug.Log("Started Close Collision Detection");
        }

        
        
        // Vector3 closestPoint =  other.ClosestPoint(transform.position);

        // float playerCollRadius = playerCollider.radius;
        // float distance = Vector3.Distance(closestPoint, transform.position);
        // float scoreScaleFactor = 1 - (distance - playerCollRadius) / (ownCollider.radius - playerCollRadius);

        // ScoreType scoreType = otherLayer == 9 ? ScoreType.CloseToObject : ScoreType.CloseToEnemy;
        // highScoreCounter.AddToHighscore(scoreType, scoreScaleFactor);
    }

    private void OnTriggerExit(Collider other) {
        int otherLayer = other.gameObject.layer;

        if( ! IsObstacleOrEnemy(otherLayer))
            return;
        


        if(scoreAdditionsToGive != closeObjectsCount){
            closeObjectsCount--;
            Debug.Log("No Points");
            return;
        }

        ScoreType scoreType = otherLayer == 9 ? ScoreType.CloseToObject : ScoreType.CloseToEnemy;
        highScoreCounter.AddToHighscore(scoreType);
        Debug.Log("Yes Points");

        closeObjectsCount--;
        scoreAdditionsToGive--;
    }

    private bool IsObstacleOrEnemy(int otherLayer) =>  otherLayer.IsInsideMask(obstacleEnemyLayerMask);

    public void PlayerCollidedWithCollider(int otherLayer)
    {
        if( ! IsObstacleOrEnemy(otherLayer) || closeObjectsCount < 1)
            return;

        scoreAdditionsToGive--;      
    }
}
