using UnityEngine;

namespace WorldUnitCollision2DSystem.Example
{
    public class Bullet : MonoBehaviour
    {
        public float speed = 20f;
        public float lifeTime = 3f;
        private Vector2 direction;
        private bool _recycled;
        private float _lifeTimer;

        private void OnEnable()
        {
            _recycled = false;
            _lifeTimer = lifeTime;
        }

        private void Update()
        {
            // 使用2D向量来更新子弹位置
            transform.position += (Vector3)direction * speed * Time.deltaTime;
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Recycle("Bullet.LifeTime");
            }
        }

        public void SetDirection(Vector2 newDirection)
        {
            direction = newDirection.normalized;
            // 设置子弹的旋转，使其朝向移动方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void Recycle(string reason)
        {
            if (_recycled) return;
            _recycled = true;
            BulletObjectPool.Instance.ReturnToPool(this.gameObject, reason);
        }
    }
}
