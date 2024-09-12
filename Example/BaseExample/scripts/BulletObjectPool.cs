using UnityEngine;
using System.Collections.Generic;


namespace WorldUnitCollisionSystem.Example
{
    public class BulletObjectPool : MonoBehaviour
    {
        public static BulletObjectPool Instance;
        public GameObject bulletPrefab;
        public int initSize = 20;

        private List<GameObject> bulletPool = new List<GameObject>();
        private int activeBulletCount;

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < initSize; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab);
                bullet.SetActive(false);
                bulletPool.Add(bullet);
            }
        }

        public GameObject GetFromPool()
        {
            activeBulletCount++;
            if (bulletPool.Count > 0)
            {
                var index = bulletPool.Count - 1;
                GameObject bullet = bulletPool[index];
                bulletPool.RemoveAt(index);
                bullet.SetActive(true);
                return bullet;
            }
            else
            {
                GameObject bullet = Instantiate(bulletPrefab);
                return bullet;
            }
        }

        public void ReturnToPool(GameObject bullet)
        {
            activeBulletCount--;
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }

        // 在屏幕显示当前子弹数量
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24; // 增大字体大小
            style.normal.textColor = Color.white; // 设置文字颜色为白色
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 50, 200, 20), "当前子弹数量: " + activeBulletCount, style);
        }
    }
}