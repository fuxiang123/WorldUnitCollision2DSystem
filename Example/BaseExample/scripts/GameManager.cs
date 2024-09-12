using UnityEngine;

namespace WorldUnitCollision2DSystem.Example
{
    public class GameManager : MonoBehaviour
    {
        public float spawnInterval = 2f;
        public int enemiesToSpawn = 3; // 每次生成的敌人数量
        public bool isSpawning = true; // 控制生成状态

        private void Start()
        {
            InvokeRepeating("SpawnEnemy", 0f, spawnInterval);
        }

        private void Update()
        {
            CalFPS();
        }

        void SpawnEnemy()
        {
            if (!isSpawning) return; // 如果停止生成，则直接返回

            for (int i = 0; i < enemiesToSpawn; i++) // 循环生成多个敌人
            {
                Vector2 spawnPosition = GetSpawnPosition(); // 获取屏幕边缘生成位置
                // 从对象池获取敌人实例
                GameObject enemy = EnemyPoolManager.Instance.GetFromPool();
                enemy.transform.position = spawnPosition;
                enemy.transform.rotation = Quaternion.identity;
            }
        }

        private Vector2 GetSpawnPosition()
        {
            float x = Random.Range(0, Screen.width);
            float y = Random.Range(0, Screen.height);
            Vector2 screenPosition = new Vector2(x, y);
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

            return worldPosition;
        }

        private int count = 0;
        private float timer = 0f;
        private float showTime = 1f;
        private int fps = 0;

        // 在屏幕右下显示当前FPS
        private void CalFPS()
        {
            count++;
            timer += Time.deltaTime;
            if (timer >= showTime)
            {
                fps = Mathf.RoundToInt(count / timer);
                count = 0;
                timer = 0f;
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24; // 增大字体大小
            style.normal.textColor = Color.white; // 设置文字颜色为白色
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 150, 200, 20), "当前FPS: " + fps, style);
        }
    }
}