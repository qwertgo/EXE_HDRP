using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarSpawn : MonoBehaviour
{
    
    [SerializeField] private int pillarCount;
    [SerializeField] private GameObject pillarPrefab;

    [Header("Fireflies")] 
    [SerializeField, Min(1)] private int firelfyCount;
    [SerializeField] private GameObject fireflyPrefab;
    [SerializeField] private Transform fireflyParent;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < pillarCount; i++)
        {
            Vector2 pos = RandomPos();

            Instantiate(pillarPrefab, new Vector3(pos.x, pillarPrefab.transform.localScale.y, pos.y), Quaternion.identity, transform);
        }

        for (int i = 0; i < firelfyCount; i++)
        {
            Vector2 pos = RandomPos();
            Instantiate(fireflyPrefab, new Vector3(pos.x, 1, pos.y), Quaternion.identity, fireflyParent);
        }
    }

    Vector2 RandomPos()
    {
        float x = Random.Range(-195f, 195f);
        x = x >= 0 ? x + 5 : x - 5;
        // Debug.Log(x);
        float z = Random.Range(-195f, 195f);
        z = z >= 0 ? z + 5 : z - 5;

        return new Vector2(x, z);
    }
}
