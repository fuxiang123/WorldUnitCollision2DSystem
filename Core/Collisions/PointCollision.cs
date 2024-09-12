
using System;
using UnityEngine;

namespace WorldUnitCollisionSystem
{
    public class PointCollision : MonoBehaviour
    {
        public string LayerName;
        public Action<GameObject, string> OnTrigger;
        private Vector2Int _currentIndex;

        private Vector2Int _impossibleIndex = new Vector2Int(int.MaxValue, int.MaxValue);

        void OnDisable()
        {
            if (_currentIndex != _impossibleIndex) WorldUnitCollisionSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
        }

        void Update()
        {
            if (CameraUtil.IsOutOfCamera(transform.position))
            {
                if (_currentIndex != _impossibleIndex)
                {
                    WorldUnitCollisionSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
                    _currentIndex = _impossibleIndex;
                }
                return;
            }

            // 更新子弹所在的WorldUnit
            var index = WorldUnitCollisionSystem.Instance.GetWorldUnitIndex(transform.position);
            if (_currentIndex == _impossibleIndex || index != _currentIndex)
            {
                WorldUnitCollisionSystem.Instance.AddObject(transform.position, LayerName, gameObject);
                if (_currentIndex != _impossibleIndex) WorldUnitCollisionSystem.Instance.RemoveObject(_currentIndex, LayerName, gameObject);
                _currentIndex = index;
            }
        }
    }
}
