
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollisionSystem
{
    public class BoxCollision : MonoBehaviour
    {
        public float width = 1;
        public float height = 1;
        public Vector2 offset;
        // 碰撞层配置
        public string LayerName;
        // 碰撞的物体和碰撞层名称
        public Action<GameObject, string> OnTrigger;
        // 已经碰撞过的子弹
        private HashSet<WorldUnit> worldUnits;
        void OnDisable()
        {
            if (worldUnits != null)
            {
                WorldUnitCollisionSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                worldUnits = null;
            }
        }

        void Update()
        {
            if (CameraUtil.IsOutOfCamera(transform.position))
            {
                if (worldUnits != null)
                {
                    WorldUnitCollisionSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                    worldUnits = null;
                }
                return;
            }

            var bounds = GetBounds();
            var curWorldUnits = WorldUnitCollisionSystem.Instance.GetWorldUnitGroup(bounds);
            if (worldUnits == null || !curWorldUnits.SetEquals(worldUnits))
            {
                if (worldUnits != null) WorldUnitCollisionSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                WorldUnitCollisionSystem.Instance.AddObject(curWorldUnits, LayerName, gameObject);
                worldUnits = curWorldUnits;
            }
        }

        public CollisionBounds GetBounds()
        {
            var xMin = transform.position.x - width / 2 + offset.x;
            var xMax = transform.position.x + width / 2 + offset.x;
            var yMin = transform.position.y - height / 2 + offset.y;
            var yMax = transform.position.y + height / 2 + offset.y;
            return new CollisionBounds(xMin, xMax, yMin, yMax);
        }

#if UNITY_EDITOR
        // 绘制碰撞区域
        void OnDrawGizmosSelected()
        {
            if (WorldUnitCollisionSystem.Instance != null && !WorldUnitCollisionSystem.Instance.ShowDebugInfo) return;
            var bounds = GetBounds();
            Gizmos.color = Color.green;
            Vector2 topLeft = new Vector2(bounds.xMin, bounds.yMax);
            Vector2 topRight = new Vector2(bounds.xMax, bounds.yMax);
            Vector2 bottomLeft = new Vector2(bounds.xMin, bounds.yMin);
            Vector2 bottomRight = new Vector2(bounds.xMax, bounds.yMin);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
#endif
    }
}