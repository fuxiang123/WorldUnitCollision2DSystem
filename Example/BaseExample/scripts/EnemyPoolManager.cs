using UnityEngine;
using System.Collections.Generic;

namespace WorldUnitCollision2DSystem.Example
{
    public class EnemyPoolManager : MonoBehaviour
    {
        public static EnemyPoolManager Instance;
        public GameObject enemyPrefab;
        public int initSize = 10;

        private List<GameObject> enemyPool = new List<GameObject>();
        private int activeEnemyCount;

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < initSize; i++)
            {
                GameObject enemy = Instantiate(enemyPrefab);
                enemy.SetActive(false);
                enemyPool.Add(enemy);
            }
        }

        public GameObject GetFromPool()
        {
            activeEnemyCount++;
            if (enemyPool.Count > 0)
            {
                var index = enemyPool.Count - 1;
                GameObject enemy = enemyPool[index];
                enemyPool.RemoveAt(index);
                enemy.SetActive(true);
                return enemy;
            }
            else
            {
                GameObject enemy = Instantiate(enemyPrefab);
                return enemy;
            }
        }

        public void ReturnToPool(GameObject enemy)
        {
            enemy.SetActive(false);
            enemyPool.Add(enemy);
            activeEnemyCount--;
        }

        public int ActiveEnemyCount => activeEnemyCount;
    }
}
