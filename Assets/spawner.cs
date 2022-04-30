using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private GameObject parent;
    [SerializeField] private int boidsCount;
    
    // Start is called before the first frame update
    void Start()
    {
        SpawnBoids(boidsCount);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SpawnBoids(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject boidInstance = Instantiate(boidPrefab, transform.position, Quaternion.Euler((float)Random.value * 360, 90, 0));
            boidInstance.name = "Boid" + i;
            boidInstance.transform.parent = parent.transform;
        }
    }

}
