using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class CameraColliderGenerator : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private float nearClippingPlane;
    [SerializeField] private float farClippingPlane;

    private Vector3 eulerAnglesOffset;
    
    [Header("References")]
    [SerializeField] private MeshCollider rightMeshCollider;
    [SerializeField] private MeshCollider leftMeshCollider;
    [SerializeField] private GameObject sphere;
    [SerializeField] private Material nearMat;
    [SerializeField] private Material farMat;
    private Camera cam;
    
    Vector3[] p = new Vector3[12];
    
    void Start()
    {
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;

        cam = Camera.main;
        
        CalculateCornerPoints();
        CreateRightMeshCollider();
        CreateLeftMeshCollider();

        transform.position = cam.transform.position;
        eulerAnglesOffset = cam.transform.eulerAngles;
    }

    private void Update()
    {
        transform.position = cam.transform.position;
        transform.eulerAngles = cam.transform.eulerAngles - eulerAnglesOffset;
    }

    

    void CalculateCornerPoints()
    {
        //calculate collider corner points
        Vector3[] frustumCorners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

        for(int i = 0; i < frustumCorners.Length; i++)
        {
            Vector3 worldSpaceCorner = cam.transform.TransformVector(frustumCorners[i]);
            Vector3 camPos = cam.transform.position;
            Vector3 camToCorner = worldSpaceCorner - camPos;
            camToCorner.Normalize();
            
            p[i] = camToCorner * nearClippingPlane;
            p[i + 6] = camToCorner * farClippingPlane;
        }

        //calculate in between points
        p[4] = p[3] + (p[0] - p[3]) * .5f;
        p[5] = p[2] + (p[1] - p[2]) * .5f;

        p[10] = p[9] + (p[6] - p[9]) * .5f;
        p[11] = p[8] + (p[7] - p[8]) * .5f;
        
        // CreateDebugSpheres();
    }

    void CreateDebugSpheres()
    {
        Vector3 camPos = cam.transform.position;
        for(int i =0; i < p.Length; i++)
        {
            GameObject instance = Instantiate(sphere, p[i] + camPos, Quaternion.identity);
            instance.name = "point" + i;
            instance.GetComponent<MeshRenderer>().material = i < 6 ? nearMat : farMat;
        }
    }

    void CreateLeftMeshCollider()
    {
        //leftCollider
        Mesh leftMesh = new Mesh
        {
            vertices = new []
            {
                //front 0-3
                p[0],p[1],p[5],p[4],
                //back 4-7
                p[6], p[7], p[11], p[10]
                
            },
            
            triangles = new []
            {
                //back
                0,1,2,
                2,3,0,
                //front
                6,5,4,
                6,4,7,
                //right
                3,2,6,
                6,7,3,
                //left
                5,1,0,
                5,0,4,
                //top
                5,6,2,
                2,1,5,
                //down
                7,4,0,
                7,0,3
            }
        };
        
        leftMesh.RecalculateNormals();
        leftMeshCollider.sharedMesh = leftMesh;
    }

    void CreateRightMeshCollider()
    {
        //rightCollider
        Mesh rightMesh = new Mesh
        {
            vertices = new []
            {
                //front 0-3
                p[4],p[5],p[2],p[3],
                //back 4-7
                p[10], p[11], p[8], p[9]
                
            },
            
            triangles = new []
            {
                //back
                0,1,2,
                2,3,0,
                //front
                6,5,4,
                6,4,7,
                //right
                3,2,6,
                6,7,3,
                //left
                5,1,0,
                5,0,4,
                //top
                5,6,2,
                2,1,5,
                //down
                7,4,0,
                7,0,3
            }
        };
        
        rightMesh.RecalculateNormals();
        rightMeshCollider.sharedMesh = rightMesh;
    }
}