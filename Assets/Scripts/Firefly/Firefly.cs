using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firefly : MonoBehaviour
{

    [SerializeField] private float collectSpeed;
    private Rigidbody _rb;
    private Collider _collider;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("FireflyCollector"))
        {
            _rb.isKinematic = true;
            _collider.enabled = false;
            StartCoroutine(MoveToPlayer(other.transform));
        }
    }

    IEnumerator MoveToPlayer(Transform player)
    {
        Vector3 startPos = transform.position;
        float t = 0;
        
        while (t < 1)
        {
            t += Time.deltaTime * collectSpeed;
            transform.position = Vector3.Lerp(startPos, player.position, t * t);
            yield return null;
        }

        player.parent.GetComponent<PlayerController>().CollectFirefly();
        Destroy(gameObject);
    }
}
