﻿using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public float spawnTime = 3f;

    [SerializeField]
    EnemyFactory factory;
    IFactory Factory
    {
        get
        {
            return factory as IFactory;
        }
    }

    void Start()
    {
        //Mengeksekusi fungs Spawn setiap beberapa detik sesuai dengan nilai spawnTime
        InvokeRepeating("Spawn", spawnTime, spawnTime);
    }


    void Spawn()
    {
        if (playerHealth.currentHealth <= 0f)
        {
            return;
        }

        //random
        int spawnEnemy = Random.Range(0, 3);

        //enemy
        Factory.FactoryMethod(spawnEnemy);
    }
}