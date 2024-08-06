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

    private bool lockObjectScore;
    private bool lockEnemyScore;

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
        AddToScore(scoreType);

        closeObjectsCount--;
        scoreAdditionsToGive--;
    }

    private void AddToScore(ScoreType scoreType)
    {
        if (scoreType == ScoreType.CloseToObject && lockObjectScore || lockEnemyScore)
            return;

        highScoreCounter.AddToScore(scoreType);
        StartCoroutine(ScoreCooldown(scoreType));
    }

    private IEnumerator ScoreCooldown(ScoreType scoreType)
    {
        if (scoreType == ScoreType.CloseToObject)
            lockObjectScore = true;
        else
            lockEnemyScore = true;

        yield return new WaitForSeconds(1f);

        if (scoreType == ScoreType.CloseToObject)
            lockObjectScore = false;
        else
            lockEnemyScore = false;
    }

    private bool IsObstacleOrEnemy(int otherLayer) =>  otherLayer.IsInsideMask(obstacleEnemyLayerMask);

    public void PlayerCollidedWithCollider(int otherLayer)
    {
        if(!IsObstacleOrEnemy(otherLayer) || closeObjectsCount < 1)
            return;

        scoreAdditionsToGive--;      
    }
}
