
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    [Serializable]
    public class CollisionLayerConfig
    {
        [LabelText("当前主动碰撞层")]
        [ValueDropdown("GetActiveLayers")]
        [ValidateInput("ValidateActiveLayerExist", "只能配置主动碰撞层")]
        public string ActiveLayer;

        [LabelText("与当前主动层碰撞的其他层")]
        [ValueDropdown("GetCollisionLayers")]
        [ValidateInput("ValidateCollisionLayersDuplicate", "不能有重复的碰撞层配置")]
        [ValidateInput("ValidateCollisionLayerExist", "当前碰撞层不存在")]
        [ValidateInput("ValidateNotActiveLayer", "不能配置当前主动层")]
        public List<string> CollisionLayers;

        private List<string> _activeCollisionLayers;
        private List<string> _passiveCollisionLayers;
        public void SetCollisionLayers(List<string> activeCollisionLayers, List<string> passiveCollisionLayers)
        {
            _activeCollisionLayers = activeCollisionLayers;
            _passiveCollisionLayers = passiveCollisionLayers;
        }

        private List<string> GetActiveLayers()
        {
            return _activeCollisionLayers;
        }

        // 获取所有碰撞层，但不包括当前CurrentActiveLayer
        private List<string> GetCollisionLayers()
        {
            return _activeCollisionLayers.Where(layer => layer != ActiveLayer).Concat(_passiveCollisionLayers).ToList();
        }

        // 验证碰撞层，不能有重复的碰撞层
        private bool ValidateCollisionLayersDuplicate(List<string> collisionLayers)
        {
            return collisionLayers.Distinct().Count() == collisionLayers.Count;
        }

        // 验证碰撞层， 配置的主动碰撞层是否存在
        private bool ValidateActiveLayerExist(string activeLayer)
        {
            return _activeCollisionLayers.Contains(activeLayer);
        }

        // 验证碰撞层， 配置的碰撞层是否存在
        private bool ValidateCollisionLayerExist(List<string> collisionLayers)
        {
            var allLayers = _activeCollisionLayers.Concat(_passiveCollisionLayers).ToList();
            foreach (var layer in collisionLayers)
            {
                if (!allLayers.Contains(layer))
                {
                    return false;
                }
            }
            return true;
        }

        // 验证碰撞层，不能配置当前主动层
        private bool ValidateNotActiveLayer(List<string> collisionLayers)
        {
            return !collisionLayers.Contains(ActiveLayer);
        }
    }

    [CreateAssetMenu(fileName = "CollisionLayerConfig", menuName = "WorldUnitCollision2DSystem/CollisionLayerConfig")]
    public class CollisionLayerConfigSO : ScriptableObject
    {

        [LabelText("主动碰撞层名称")]
        [OnValueChanged("OnCollisionLayersChanged")]
        [ValidateInput("ValidateLayerDuplicate", "不能有重复的碰撞层")]
        [ValidateInput("ValidateLayerEmpty", "不能有为空的碰撞层")]
        public List<string> ActiveCollisionLayers;

        [LabelText("被动碰撞层名称")]
        [OnValueChanged("OnCollisionLayersChanged")]
        [ValidateInput("ValidateLayerDuplicate", "不能有重复的碰撞层")]
        [ValidateInput("ValidateLayerEmpty", "不能有为空的碰撞层")]
        public List<string> PassiveCollisionLayers;

        [LabelText("碰撞层配置"), TableList, Space(10)]
        [OnValueChanged("OnCollisionLayersChanged")]
        [ValidateInput("ValidateLayerConfigDuplicate", "不能有重复的碰撞层配置")]
        [ValidateInput("ValidateLayerConfigEmpty", "不能有为空的碰撞层配置")]
        public List<CollisionLayerConfig> CollisionLayerConfigs;

        private void OnEnable()
        {
            foreach (var config in CollisionLayerConfigs)
            {
                config.SetCollisionLayers(ActiveCollisionLayers, PassiveCollisionLayers);
            }
        }

        // 获取碰撞层相关配置
        public CollisionLayerConfig GetCollisionLayerConfig(string layerName)
        {
            return CollisionLayerConfigs.Find(config => config.ActiveLayer == layerName);
        }

        private void OnCollisionLayersChanged()
        {
            foreach (var config in CollisionLayerConfigs)
            {
                config.SetCollisionLayers(ActiveCollisionLayers, PassiveCollisionLayers);
            }
        }


        // 验证碰撞层，不能有重复的层
        private bool ValidateLayerDuplicate(List<string> layers)
        {
            return layers.Distinct().Count() == layers.Count;
        }

        // 验证碰撞层，不能有为空的层
        private bool ValidateLayerEmpty(List<string> layers)
        {
            return layers.All(layer => !string.IsNullOrEmpty(layer));
        }

        // 验证碰撞层配置， 不能有重复的层
        private bool ValidateLayerConfigDuplicate(List<CollisionLayerConfig> collisionLayerConfigs)
        {
            var layers = new List<string>();
            foreach (var config in collisionLayerConfigs)
            {
                if (layers.Contains(config.ActiveLayer))
                {
                    return false;
                }
                layers.Add(config.ActiveLayer);
            }
            return true;
        }

        // 验证碰撞层配置，不能有为空的层
        private bool ValidateLayerConfigEmpty(List<CollisionLayerConfig> collisionLayerConfigs)
        {
            foreach (var config in collisionLayerConfigs)
            {
                if (string.IsNullOrEmpty(config.ActiveLayer))
                {
                    return false;
                }
            }
            return true;
        }
    }
}