using UnityEngine;
using System.Collections.Generic;


namespace WorldUnitCollision2DSystem.Example
{
    public class BulletObjectPool : MonoBehaviour
    {
        public static BulletObjectPool Instance;
        public GameObject bulletPrefab;
        public int initSize = 20;

        private List<GameObject> bulletPool = new List<GameObject>();
        private int activeBulletCount;
        private readonly HashSet<GameObject> activeBullets = new HashSet<GameObject>();
        private readonly HashSet<GameObject> pooledBullets = new HashSet<GameObject>();

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < initSize; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab);
                bullet.SetActive(false);
                bulletPool.Add(bullet);
                pooledBullets.Add(bullet);
            }
        }

        public GameObject GetFromPool()
        {
            if (bulletPool.Count > 0)
            {
                var index = bulletPool.Count - 1;
                GameObject bullet = bulletPool[index];
                bulletPool.RemoveAt(index);
                pooledBullets.Remove(bullet);
                bullet.SetActive(true);
                if (activeBullets.Add(bullet)) activeBulletCount++;
                return bullet;
            }
            else
            {
                GameObject bullet = Instantiate(bulletPrefab);
                bullet.SetActive(true);
                if (activeBullets.Add(bullet)) activeBulletCount++;
                return bullet;
            }
        }

        public void ReturnToPool(GameObject bullet)
        {
            ReturnToPool(bullet, null);
        }

        public void ReturnToPool(GameObject bullet, string reason)
        {
            if (bullet == null) return;
            var reasonInfo = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            if (!activeBullets.Remove(bullet))
            {
                var bulletInfo = $"{bullet.name} (id={bullet.GetInstanceID()})";
                var stateInfo = bullet.activeSelf ? "active" : "inactive";
#if UNITY_EDITOR
                Debug.Log($"子弹重复回收/不在活动列表，已忽略。bullet={bulletInfo}, state={stateInfo}, reason={reasonInfo}, frame={Time.frameCount}", this);
#endif
                return;
            }
            activeBulletCount--;
            bullet.SetActive(false);
            if (pooledBullets.Add(bullet))
            {
                bulletPool.Add(bullet);
            }
            else
            {
                var bulletInfo = $"{bullet.name} (id={bullet.GetInstanceID()})";
#if UNITY_EDITOR
                Debug.Log($"子弹已在对象池中，忽略重复加入。bullet={bulletInfo}, reason={reasonInfo}, frame={Time.frameCount}", this);
#endif
            }
        }

        public int ActiveBulletCount => activeBulletCount;
    }
}
