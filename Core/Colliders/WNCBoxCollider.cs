using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace WorldUnitCollision2DSystem
{
    public class WNCBoxCollider : AbstractCollider
    {
        public float width = 1;
        public float height = 1;
        public Vector2 offset;
        [FormerlySerializedAs("DisableWhenOutOfCamera")] [LabelText("是否在屏幕外禁用")]public bool disableWhenOutOfCamera = true;
        private HashSet<WorldUnit> _worldUnits;
        void OnDisable()
        {
            if (_worldUnits != null)
            {
                WorldUnitCollision2DSystem.Instance.RemoveObject(_worldUnits, LayerName, gameObject);
                _worldUnits = null;
            }
        }

        void Update()
        {
            if (disableWhenOutOfCamera && CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_worldUnits != null)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveObject(_worldUnits, LayerName, gameObject);
                    _worldUnits = null;
                }
                return;
            }

            var bounds = GetBounds();
            var curWorldUnits = WorldUnitCollision2DSystem.Instance.GetWorldUnitGroup(bounds);
            if (_worldUnits == null || !curWorldUnits.SetEquals(_worldUnits))
            {
                if (_worldUnits != null) WorldUnitCollision2DSystem.Instance.RemoveObject(_worldUnits, LayerName, gameObject);
                WorldUnitCollision2DSystem.Instance.AddObject(curWorldUnits, LayerName, gameObject);
                _worldUnits = curWorldUnits;
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
            if (WorldUnitCollision2DSystem.Instance != null && !WorldUnitCollision2DSystem.Instance.showDebugInfo) return;
            var bounds = GetBounds();
            Gizmos.color = Color.green;
            Vector2 topLeft = new Vector2(bounds.XMin, bounds.YMax);
            Vector2 topRight = new Vector2(bounds.XMax, bounds.YMax);
            Vector2 bottomLeft = new Vector2(bounds.XMin, bounds.YMin);
            Vector2 bottomRight = new Vector2(bounds.XMax, bounds.YMin);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
#endif
    }
}