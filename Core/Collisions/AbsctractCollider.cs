
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class AbstractCollider : MonoBehaviour
    {
        public string LayerName;
        public Action<GameObject, string> OnTrigger;

        private void Start()
        {
            var CollisionLayerConfigSO = WorldUnitCollision2DSystem.Instance.CollisionLayerConfigSO;
            if (!CollisionLayerConfigSO.ActiveCollisionLayers.Contains(LayerName) && !CollisionLayerConfigSO.PassiveCollisionLayers.Contains(LayerName))
            {
                Debug.LogError($"碰撞层配置文件中没有找到LayerName为{LayerName}的碰撞层配置");
                return;
            }
        }
    }
}