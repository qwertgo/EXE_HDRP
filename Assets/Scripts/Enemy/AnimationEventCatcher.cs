using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventCatcher : MonoBehaviour
{
    [SerializeField] private EnemyMovement enemyMovement;

    public void FinishedAttack()
    {
        enemyMovement.finishedAttack = true;
    }

    public void StartSlowMoBoost()
    {
        GameVariables.instance.player.StartSlowMoBoost(enemyMovement);
    }
}
