using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class WNCPointCollider : AbstractCollider
    {
        [LabelText("是否在屏幕外禁用")]public bool DisableWhenOutOfCamera = true;
        
        private static readonly Vector2Int ImpossibleIndex = new(int.MaxValue, int.MaxValue);
        private Vector2Int _currentIndex = ImpossibleIndex;

        void OnDisable()
        {
            if (_currentIndex != ImpossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
            _currentIndex = ImpossibleIndex;
        }

        void Update()
        {
            if (DisableWhenOutOfCamera && CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_currentIndex != ImpossibleIndex)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
                    _currentIndex = ImpossibleIndex;
                }
                return;
            }

            // 更新子弹所在的WorldUnit
            var index = WorldUnitCollision2DSystem.Instance.GetWorldUnitIndex(transform.position);
            if (_currentIndex == ImpossibleIndex || index != _currentIndex)
            {
                WorldUnitCollision2DSystem.Instance.AddCollider(transform.position, LayerName, this);
                if (_currentIndex != ImpossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveCollider(_currentIndex, LayerName, this);
                _currentIndex = index;
            }
        }
    }
}
