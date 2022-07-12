using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private GameObject parent;
    [SerializeField] private int boidsCount;
    
    void Awake()
    {
        SpawnBoids(boidsCount);
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
