using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class WNCPointCollider : AbstractCollider
    {
        [LabelText("是否在屏幕外禁用")]public bool DisableWhenOutOfCamera = true;
        private Vector2Int _currentIndex;

        private readonly Vector2Int _impossibleIndex = new(int.MaxValue, int.MaxValue);

        void OnDisable()
        {
            if (_currentIndex != _impossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
        }

        void Update()
        {
            if (DisableWhenOutOfCamera && CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_currentIndex != _impossibleIndex)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
                    _currentIndex = _impossibleIndex;
                }
                return;
            }

            // 更新子弹所在的WorldUnit
            var index = WorldUnitCollision2DSystem.Instance.GetWorldUnitIndex(transform.position);
            if (_currentIndex == _impossibleIndex || index != _currentIndex)
            {
                WorldUnitCollision2DSystem.Instance.AddCollider(transform.position, LayerName, this);
                if (_currentIndex != _impossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
                _currentIndex = index;
            }
        }
    }
}
