using UnityEngine;

namespace WorldUnitCollisionSystem.Example
{
    public class Bullet : MonoBehaviour
    {
        public float speed = 20f;
        private Vector2 direction;

        private void Update()
        {
            // 使用2D向量来更新子弹位置
            transform.position += (Vector3)direction * speed * Time.deltaTime;
        }

        public void SetDirection(Vector2 newDirection)
        {
            direction = newDirection.normalized;
            // 设置子弹的旋转，使其朝向移动方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        void OnBecameInvisible()
        {
            // 使用对象池回收子弹
            BulletObjectPool.Instance.ReturnToPool(this.gameObject);
        }
    }
}