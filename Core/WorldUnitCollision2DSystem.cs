using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace WorldUnitCollision2DSystem
{
    // 碰撞边界
    public struct CollisionBounds
    {
        public CollisionBounds(float XMin, float XMax, float YMin, float YMax)
        {
            this.XMin = XMin;
            this.XMax = XMax;
            this.YMin = YMin;
            this.YMax = YMax;
        }
        public float XMin;
        public float XMax;
        public float YMin;
        public float YMax;
    }

    // 网格碰撞系统
    public class WorldUnitCollision2DSystem : MonoBehaviour
    {
        public static WorldUnitCollision2DSystem Instance;
        [FormerlySerializedAs("CollisionLayerConfigSO")] [LabelText("碰撞层配置文件")] public CollisionLayerConfigSO CollisionLayerConfigSo;
        [SerializeField, LabelText("网格大小")] private float unitWidth = 1;
        [SerializeField, LabelText("网格存活时间")] private float worldUnitRemoveTime = 5;

        private readonly Dictionary<Vector2Int, WorldUnit> WorldUnits = new();
        // 需要删除的网格
        private readonly List<WorldUnit> _worldUnitsToRemove = new();
        // 待触发碰撞的物体
        private readonly List<Action> _triggerActionList = new();
        // worldUnit对象池
        private readonly WorldUnitObjectPool _worldUnitObjectPool = new();
#if UNITY_EDITOR
        [FormerlySerializedAs("ShowDebugInfo")] public bool showDebugInfo = true;
#endif
        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            CheckCollision();
            TriggerAllCollision();
            RemoveWorldUnits();
        }

        // 触发所有的碰撞事件
        private void TriggerAllCollision()
        {
            _triggerActionList.ForEach(e =>
            {
                e?.Invoke();
            });
            _triggerActionList.Clear();
        }

        // 移除长时间不用的网格
        private void RemoveWorldUnits()
        {
            if (worldUnitRemoveTime <= 0) return;
            foreach (var item in _worldUnitsToRemove)
            {
                _worldUnitObjectPool.ReturnObject(item);
                WorldUnits.Remove(item.Index);
            }
            _worldUnitsToRemove.Clear();
        }

        // 遍历所有格子，检查碰撞
        private void CheckCollision()
        {
            // 遍历所有网格，计算碰撞
            foreach (var unit in WorldUnits)
            {
                var worldUnit = unit.Value;
                HandleWorldUnitRemove(worldUnit);

                // 遍历所有碰撞层配置
                foreach (var config in CollisionLayerConfigSo.CollisionLayerConfigs)
                {
                    var layerName = config.ActiveLayer;
                    var otherCollisionLayers = config.CollisionLayers;
                    // 获取存储在当前网格的主动碰撞层物体
                    if (!worldUnit.LayerObjects.TryGetValue(layerName, out var activeObjects)) continue;
                    // 遍历当前层所有物体
                    foreach (var activeObj in activeObjects)
                    {
                        // 通过碰撞层配置，计算所有的碰撞
                        foreach (var otherCollisionLayer in otherCollisionLayers)
                        {
                            // 获取可能与当前物体碰撞的物体
                            if (!worldUnit.LayerObjects.TryGetValue(otherCollisionLayer, out var otherObjects)) continue;
                            foreach (var otherObj in otherObjects)
                            {
                                HandleObjectCollision(activeObj, otherObj);
                            }
                        }
                    }
                }
            }
        }

        // 处理网格随时间销毁的逻辑
        void HandleWorldUnitRemove(WorldUnit worldUnit)
        {
            if (worldUnitRemoveTime <= 0) return;
            // 如果当前网格没有物体，则增加时间
            if (worldUnit.ObjectCount <= 0)
            {
                worldUnit.LastCollisionTime += Time.deltaTime;
                // 删除长时间没有物体的网格
                if (worldUnit.LastCollisionTime > worldUnitRemoveTime)
                {
                    _worldUnitsToRemove.Add(worldUnit);
                }
            }
            else if (worldUnit.LastCollisionTime > 0)
            {
                worldUnit.LastCollisionTime = 0;
            }
        }

        // 处理两个物体的碰撞
        void HandleObjectCollision(AbstractCollider activeCld, AbstractCollider otherCld)
        {
            if (!activeCld.isActiveAndEnabled || !otherCld.isActiveAndEnabled) return;
            
            bool isCollision = false;
            
            if (activeCld is WNCBoxCollider && otherCld is WNCBoxCollider)
            {
                isCollision = IsCollision((activeCld as WNCBoxCollider).GetBounds(),
                    (otherCld as WNCBoxCollider).GetBounds());
            } else if (activeCld is WNCBoxCollider && otherCld is WNCPointCollider)
            {
                isCollision = IsCollision((activeCld as WNCBoxCollider).GetBounds(), otherCld.transform.position);
            } else if (activeCld is WNCPointCollider && otherCld is WNCBoxCollider)
            {
                isCollision = IsCollision((otherCld as WNCBoxCollider).GetBounds(), activeCld.transform.position);
            }
            
            if (isCollision)
            {
                _triggerActionList.Add(() =>
                {
                    if (activeCld.isActiveAndEnabled && otherCld.isActiveAndEnabled)
                        activeCld.OnTrigger?.Invoke(otherCld.gameObject, otherCld.LayerName);    
                });
            }
        }

        bool IsCollision(CollisionBounds bounds, CollisionBounds otherBounds)
        {
            return bounds.XMax > otherBounds.XMin && bounds.XMin < otherBounds.XMax &&
                   bounds.YMax > otherBounds.YMin && bounds.YMin < otherBounds.YMax;
        }

        bool IsCollision(CollisionBounds bounds, Vector2 position)
        {
            return bounds.XMax > position.x && bounds.XMin < position.x &&
                   bounds.YMax > position.y && bounds.YMin < position.y;
        }


        // 获取当前位置的四周格子
        public HashSet<WorldUnit> GetWorldUnitGroup(WNCBoxCollider collision)
        {
            // 获取BoxCollider占据的所有格子
            var bounds = collision.GetBounds();
            return GetWorldUnitGroup(bounds);
        }

        // 获取当前位置的四周格子
        public HashSet<WorldUnit> GetWorldUnitGroup(CollisionBounds bound)
        {
            // 获取BoxCollider占据的所有格子
            var XMin = bound.XMin;
            var XMax = bound.XMax;
            var YMin = bound.YMin;
            var YMax = bound.YMax;
            HashSet<WorldUnit> tempUnits = new HashSet<WorldUnit>
            {
                // 先计算四个角，防止跨边界的情况
                GetWorldUnit(new Vector2(XMin, YMin)),
                GetWorldUnit(new Vector2(XMax, YMax)),
                GetWorldUnit(new Vector2(XMin, YMax)),
                GetWorldUnit(new Vector2(XMax, YMin))
            };

            // 计算物体的四个边
            for (float x = XMin + unitWidth; x < XMax; x += unitWidth)
            {
                tempUnits.Add(GetWorldUnit(new Vector2(x, YMin)));
                tempUnits.Add(GetWorldUnit(new Vector2(x, YMax)));
            }

            for (float y = YMin + unitWidth; y < YMax; y += unitWidth)
            {
                tempUnits.Add(GetWorldUnit(new Vector2(XMin, y)));
                tempUnits.Add(GetWorldUnit(new Vector2(XMax, y)));
            }
            return tempUnits;
        }


        // 添加新网格
        public void AddWorldUnit(Vector2Int index)
        {
            var worldUnit = _worldUnitObjectPool.GetObject(index, unitWidth, index.x * unitWidth, index.y * unitWidth);
            WorldUnits.Add(index, worldUnit);
        }

        public Vector2Int GetWorldUnitIndex(Vector2 position)
        {
            return new Vector2Int((int)Math.Floor(position.x / unitWidth), (int)Math.Floor(position.y / unitWidth));
        }

        // 添加一个点物体到网格
        public Vector2Int AddCollider(Vector2 position, string layerName, AbstractCollider cld)
        {
            if (!CollisionLayerConfigSo.PassiveCollisionLayers.Contains(layerName) && !CollisionLayerConfigSo.ActiveCollisionLayers.Contains(layerName))
            {
                Debug.LogError("没有配置的碰撞层：" + layerName);
                return new Vector2Int(0, 0);
            }
            var worldUnit = GetWorldUnit(position);
            worldUnit.AddCollider(layerName, cld);
            return worldUnit.Index;
        }

        public HashSet<WorldUnit> AddCollider(HashSet<WorldUnit> worldUnits, string layerName, AbstractCollider cld)
        {
            foreach (var worldUnit in worldUnits)
            {
                worldUnit.AddCollider(layerName, cld);
            }
            return worldUnits;
        }

        // 从网格移除点物体
        public void RemoveCollider(Vector2 position, string layerName, AbstractCollider cld)
        {
            var worldUnit = GetWorldUnit(position);
            worldUnit.RemoveCollider(layerName, cld);
        }

        // 添加碰撞盒物体到网格
        public HashSet<WorldUnit> AddCollider(CollisionBounds collisionBound, string layerName, AbstractCollider cld)
        {
            if (!CollisionLayerConfigSo.ActiveCollisionLayers.Contains(layerName) && !CollisionLayerConfigSo.PassiveCollisionLayers.Contains(layerName))
            {
                Debug.LogError("没有配置的碰撞层：" + layerName);
                return new HashSet<WorldUnit>();
            }

            var units = GetWorldUnitGroup(collisionBound);
            foreach (var u in units)
            {
                u.AddCollider(layerName, cld);
            }
            return units;
        }

        // 移除碰撞盒物体
        public void RemoveCollider(CollisionBounds collisionBound, string layerName, AbstractCollider cld)
        {
            foreach (var item in GetWorldUnitGroup(collisionBound))
            {
                item.RemoveCollider(layerName, cld);
            }
        }

        public void RemoveCollider(HashSet<WorldUnit> units, string layerName, AbstractCollider cld)
        {
            foreach (var item in units)
            {
                item.RemoveCollider(layerName, cld);
            }
        }


        // 根据空间获取网格
        private WorldUnit GetWorldUnit(Vector2 position)
        {
            var index = GetWorldUnitIndex(position);
            return GetWorldUnit(index);
        }

        private WorldUnit GetWorldUnit(Vector2Int index)
        {
            if (!WorldUnits.ContainsKey(index))
            {
                AddWorldUnit(index);
            }
            return WorldUnits[index];
        }

        public void Reset()
        {
            WorldUnits.Clear();
            _worldUnitsToRemove.Clear();
            _triggerActionList.Clear();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (this != null && !showDebugInfo) return;
            foreach (var unit in WorldUnits)
            {
                CollisionBounds bounds = unit.Value.CollisionBounds;
                var topLeft = new Vector2(bounds.XMin, bounds.YMax);
                var topRight = new Vector2(bounds.XMax, bounds.YMax);
                var bottomLeft = new Vector2(bounds.XMin, bounds.YMin);
                var bottomRight = new Vector2(bounds.XMax, bounds.YMin);

                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);
                Handles.Label(bottomLeft + new Vector2(0.2f, 0.2f), "(" + unit.Key.x + "," + unit.Key.y + "}");
                Handles.Label(bottomRight + new Vector2(-0.2f, 0.2f), unit.Value.ObjectCount.ToString());
            }
        }
    }
#endif
}