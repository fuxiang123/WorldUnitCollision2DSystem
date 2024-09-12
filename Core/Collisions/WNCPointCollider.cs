
using System;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class WNCPointCollider : AbstractCollider
    {
        private Vector2Int _currentIndex;

        private Vector2Int _impossibleIndex = new Vector2Int(int.MaxValue, int.MaxValue);

        void OnDisable()
        {
            if (_currentIndex != _impossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
        }

        void Update()
        {
            if (CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_currentIndex != _impossibleIndex)
                {
                    WorldUnitCollision2DSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
                    _currentIndex = _impossibleIndex;
                }
                return;
            }

            // 更新子弹所在的WorldUnit
            var index = WorldUnitCollision2DSystem.Instance.GetWorldUnitIndex(transform.position);
            if (_currentIndex == _impossibleIndex || index != _currentIndex)
            {
                WorldUnitCollision2DSystem.Instance.AddObject(transform.position, LayerName, gameObject);
                if (_currentIndex != _impossibleIndex) WorldUnitCollision2DSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
                _currentIndex = index;
            }
        }
    }
}
