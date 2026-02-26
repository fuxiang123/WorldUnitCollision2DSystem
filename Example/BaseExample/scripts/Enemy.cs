using UnityEngine;

namespace WorldUnitCollision2DSystem.Example
{
    public class Enemy : MonoBehaviour
    {
        public float moveSpeed = 3f;

        private Transform _player;
        private WNCBoxCollider _boxCollision;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _boxCollision = GetComponent<WNCBoxCollider>();
            _boxCollision.OnTriggerEnter += (bullet, layerName) =>
            {
                if (layerName == "PlayerBullet")
                {
                    EnemyPoolManager.Instance.ReturnToPool(gameObject);
                    bullet.GetComponent<Bullet>()?.Recycle($"Enemy.OnTriggerEnter from {name} (id={GetInstanceID()})");
                }
            };
        }

        private void Update()
        {
            // 敌人朝玩家移动
            transform.position = Vector2.MoveTowards(transform.position, _player.position, moveSpeed * Time.deltaTime);
        }
    }
}
