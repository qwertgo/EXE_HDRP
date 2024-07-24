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
            // Debug.Log("Started Close Collision Detection");
        }
    }

    private void OnTriggerExit(Collider other) {
        int otherLayer = other.gameObject.layer;

        if( ! IsObstacleOrEnemy(otherLayer))
            return;
        


        if(scoreAdditionsToGive != closeObjectsCount){
            closeObjectsCount--;
            return;
        }

        ScoreType scoreType = otherLayer == 9 ? ScoreType.CloseToObject : ScoreType.CloseToEnemy;
        highScoreCounter.AddToScore(scoreType);

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
