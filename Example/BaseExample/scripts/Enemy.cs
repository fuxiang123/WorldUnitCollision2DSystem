using UnityEngine;

namespace WorldUnitCollision2DSystem.Example
{
    public class Enemy : MonoBehaviour
    {
        public float moveSpeed = 3f;

        private Transform player;
        private WNCBoxCollider boxCollision;

        private void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
            boxCollision = GetComponent<WNCBoxCollider>();
            boxCollision.OnTrigger += (bullet, layerName) =>
            {
                if (layerName == "PlayerBullet")
                {
                    EnemyPoolManager.Instance.ReturnToPool(gameObject);
                    BulletObjectPool.Instance.ReturnToPool(bullet);
                }
            };
        }

        private void Update()
        {
            // 敌人朝玩家移动
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
    }
}