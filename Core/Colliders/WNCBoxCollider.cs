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
                WorldUnitCollision2DSystem.Instance.RemoveCollider(_worldUnits, LayerName, this);
                _worldUnits = null;
            }
        }

        void Update()
        {
            if (disableWhenOutOfCamera && CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_worldUnits != null)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveCollider(_worldUnits, LayerName, this);
                    _worldUnits = null;
                }
                return;
            }

            var bounds = GetBounds();
            var curWorldUnits = WorldUnitCollision2DSystem.Instance.GetWorldUnitGroup(bounds);
            if (_worldUnits == null || !curWorldUnits.SetEquals(_worldUnits))
            {
                if (_worldUnits != null) WorldUnitCollision2DSystem.Instance.RemoveCollider(_worldUnits, LayerName, this);
                WorldUnitCollision2DSystem.Instance.AddCollider(curWorldUnits, LayerName, this);
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