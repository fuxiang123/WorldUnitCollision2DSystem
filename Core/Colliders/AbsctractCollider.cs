
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public class AbstractCollider : MonoBehaviour
    {
        [Required] public string LayerName;
        public Action<GameObject, string> OnTrigger;

        private void Start()
        {
            var sys = WorldUnitCollision2DSystem.Instance;
            if (sys == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException($"{nameof(WorldUnitCollision2DSystem)}.{nameof(WorldUnitCollision2DSystem.Instance)} 为 null。请确保场景中存在并启用一个 WorldUnitCollision2DSystem。");
#else
                Debug.LogError($"{nameof(WorldUnitCollision2DSystem)}.{nameof(WorldUnitCollision2DSystem.Instance)} 为 null，将禁用 {name} 上的 {GetType().Name}。", this);
                enabled = false;
                return;
#endif
            }
            var config = sys.CollisionLayerConfigSo;
            if (config == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException($"{nameof(WorldUnitCollision2DSystem.CollisionLayerConfigSo)} 未在 {sys.name} 上设置。");
#else
                Debug.LogError($"{nameof(WorldUnitCollision2DSystem.CollisionLayerConfigSo)} 未在 {sys.name} 上设置，将禁用 {name} 上的 {GetType().Name}。", this);
                enabled = false;
                return;
#endif
            }
            if (!config.ContainsLayer(LayerName))
            {
                Debug.LogError($"碰撞层配置文件中没有找到LayerName为{LayerName}的碰撞层配置");
                return;
            }
        }
    }
}
