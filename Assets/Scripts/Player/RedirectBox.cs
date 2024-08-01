using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedirectBox : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float cutOffPoint = .3f;
    [SerializeField] private bool giveBoost;
    [SerializeField] private bool useOtherObjectsForward;
    [SerializeField] private Transform otherObject;

    private PlayerController player => GameVariables.instance.player;
    private Vector3 forward;

    private void Start()
    {
        forward = useOtherObjectsForward ? otherObject.forward : transform.forward;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer.IsInsideMask(playerLayer))
        {
            player.StartRedirection(forward, cutOffPoint, giveBoost);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer.IsInsideMask(playerLayer))
        {
            player.StopRedirection();
        }
    }
}
