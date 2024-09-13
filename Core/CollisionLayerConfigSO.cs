
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    [Serializable]
    public class CollisionLayerConfig
    {
        public string ActiveLayer;
        public List<string> CollisionLayers;
    }

    [CreateAssetMenu(fileName = "CollisionLayerConfig", menuName = "WorldUnitCollision2DSystem/CollisionLayerConfig")]
    public class CollisionLayerConfigSO : ScriptableObject
    {
        public List<string> ActiveCollisionLayers;
        public List<string> PassiveCollisionLayers;
        public List<CollisionLayerConfig> CollisionLayerConfigs;
    }
}