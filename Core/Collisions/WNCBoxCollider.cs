
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class WNCBoxCollider : AbstractCollider
    {
        public float width = 1;
        public float height = 1;
        public Vector2 offset;
        private HashSet<WorldUnit> worldUnits;
        void OnDisable()
        {
            if (worldUnits != null)
            {
                WorldUnitCollision2DSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                worldUnits = null;
            }
        }

        void Update()
        {
            if (CameraUtil.IsOutOfCamera(transform.position))
            {
                if (worldUnits != null)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                    worldUnits = null;
                }
                return;
            }

            var bounds = GetBounds();
            var curWorldUnits = WorldUnitCollision2DSystem.Instance.GetWorldUnitGroup(bounds);
            if (worldUnits == null || !curWorldUnits.SetEquals(worldUnits))
            {
                if (worldUnits != null) WorldUnitCollision2DSystem.Instance.RemoveObject(worldUnits, LayerName, gameObject);
                WorldUnitCollision2DSystem.Instance.AddObject(curWorldUnits, LayerName, gameObject);
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
            if (WorldUnitCollision2DSystem.Instance != null && !WorldUnitCollision2DSystem.Instance.ShowDebugInfo) return;
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