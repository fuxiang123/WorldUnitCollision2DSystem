using UnityEngine;

namespace WorldUnitCollisionSystem.Example
{
    public class Player : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float attackSpeed = 0.1f; // 攻击速度
        public int bulletCount = 5; // 同时发射的子弹数量
        public float spreadAngle = 15f; // 子弹散射角度
        private Vector2 movement;
        private float lastAttackTime;

        private void Update()
        {
            HandleMovement();

            if (Input.GetMouseButton(0) && Time.time >= lastAttackTime + attackSpeed) // 按住左键射击
            {
                Shoot();
                lastAttackTime = Time.time; // 更新最后攻击时间
            }
        }

        void HandleMovement()
        {
            // 获取输入
            float moveX = 0f;
            float moveY = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                moveY = 1f;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                moveY = -1f;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                moveX = -1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                moveX = 1f;
            }

            movement = new Vector2(moveX, moveY).normalized;

            // 移动玩家
            transform.position += new Vector3(movement.x, movement.y, 0) * moveSpeed * Time.deltaTime;
        }

        void Shoot()
        {
            // 获取鼠标位置
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // 计算方向
            Vector2 baseDirection = (mousePosition - transform.position).normalized;

            for (int i = 0; i < bulletCount; i++)
            {
                // 计算散射角度
                float angle = spreadAngle * (i - (bulletCount - 1) / 2f) / (bulletCount - 1);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;

                // 从对象池获取子弹实例
                GameObject bullet = BulletObjectPool.Instance.GetFromPool();
                bullet.transform.position = transform.position;
                bullet.transform.rotation = Quaternion.identity;
                // 设置子弹方向
                bullet.GetComponent<Bullet>().SetDirection(direction);
            }
        }
    }
}