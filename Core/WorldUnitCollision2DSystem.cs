using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    // 碰撞边界
    public struct CollisionBounds
    {
        public CollisionBounds(float xMin, float xMax, float yMin, float yMax)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.yMin = yMin;
            this.yMax = yMax;
        }
        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;
    }

    // 网格碰撞系统
    public class WorldUnitCollision2DSystem : MonoBehaviour
    {
        [HideInInspector] public Dictionary<Vector2Int, WorldUnit> WorldUnits = new Dictionary<Vector2Int, WorldUnit>();
        public static WorldUnitCollision2DSystem Instance;
        [LabelText("碰撞层配置文件")] public CollisionLayerConfigSO CollisionLayerConfigSO;
        [SerializeField, LabelText("网格大小")] private float unitWidth = 1;
        [SerializeField, LabelText("网格存活时间")] private float worldUnitRemoveTime = 5;

        // 需要删除的网格
        private List<WorldUnit> WorldUnitsToRemove = new List<WorldUnit>();
        // 待触发碰撞的物体
        private List<Action> TriggerActionList = new List<Action>();
        // worldUnit对象池
        private WorldUnitObjectPool worldUnitObjectPool = new WorldUnitObjectPool();
#if UNITY_EDITOR
        public bool ShowDebugInfo = true;
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
            TriggerActionList.ForEach(e =>
            {
                e?.Invoke();
            });
            TriggerActionList.Clear();
        }

        // 移除长时间不用的网格
        private void RemoveWorldUnits()
        {
            if (worldUnitRemoveTime <= 0) return;
            foreach (var item in WorldUnitsToRemove)
            {
                worldUnitObjectPool.ReturnObject(item);
                WorldUnits.Remove(item.index);
            }
            WorldUnitsToRemove.Clear();
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
                foreach (var config in CollisionLayerConfigSO.CollisionLayerConfigs)
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
            if (worldUnit.objectCount <= 0)
            {
                worldUnit.lastCollisionTime += Time.deltaTime;
                // 删除长时间没有物体的网格
                if (worldUnit.lastCollisionTime > worldUnitRemoveTime)
                {
                    WorldUnitsToRemove.Add(worldUnit);
                }
            }
            else if (worldUnit.lastCollisionTime > 0)
            {
                worldUnit.lastCollisionTime = 0;
            }
        }

        // 处理两个物体的碰撞
        void HandleObjectCollision(GameObject activeObj, GameObject otherObj)
        {
            if (!activeObj.activeSelf || !otherObj.activeSelf) return;
            var activeCollision = activeObj.GetComponent<WNCBoxCollider>();
            // 主动层为碰撞盒，被动层为点碰撞器
            var otherPointCollider = otherObj.GetComponent<WNCPointCollider>();
            if (activeCollision != null && otherPointCollider != null)
            {
                if (IsCollision(activeCollision.GetBounds(), otherPointCollider.transform.position))
                {
                    TriggerActionList.Add(() =>
                    {
                        if (otherObj.activeSelf && activeObj.activeSelf)
                        {
                            activeCollision.OnTrigger?.Invoke(otherObj, otherPointCollider.LayerName);
                        }
                    });
                }
                return;
            }

            var otherBoxCollider = otherObj.GetComponent<WNCBoxCollider>();
            // 主动和被动层都为碰撞盒
            if (activeCollision != null && otherBoxCollider != null)
            {
                if (IsCollision(activeCollision.GetBounds(), otherBoxCollider.GetBounds()))
                {
                    TriggerActionList.Add(() =>
                    {
                        if (otherObj.activeSelf && activeObj.activeSelf)
                        {
                            activeCollision.OnTrigger?.Invoke(otherObj, otherBoxCollider.LayerName);
                        }
                    });
                }
                return;
            }

            // 主动层为点碰撞器，被动层为碰撞盒
            var activePointCollider = activeObj.GetComponent<WNCBoxCollider>();
            if (activePointCollider != null && otherBoxCollider != null)
            {
                if (IsCollision(otherBoxCollider.GetBounds(), activeCollision.transform.position))
                {
                    TriggerActionList.Add(() =>
                    {
                        if (otherObj.activeSelf && activeObj.activeSelf)
                        {
                            activePointCollider.OnTrigger?.Invoke(otherObj, otherBoxCollider.LayerName);
                        }
                    });
                }
                return;
            }
        }

        bool IsCollision(CollisionBounds bounds, CollisionBounds otherBounds)
        {
            return bounds.xMax > otherBounds.xMin && bounds.xMin < otherBounds.xMax &&
                   bounds.yMax > otherBounds.yMin && bounds.yMin < otherBounds.yMax;
        }

        bool IsCollision(CollisionBounds bounds, Vector2 position)
        {
            return bounds.xMax > position.x && bounds.xMin < position.x &&
                   bounds.yMax > position.y && bounds.yMin < position.y;
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
            var xMin = bound.xMin;
            var xMax = bound.xMax;
            var yMin = bound.yMin;
            var yMax = bound.yMax;
            HashSet<WorldUnit> tempUnits = new HashSet<WorldUnit>
            {
                // 先计算四个角，防止跨边界的情况
                GetWorldUnit(new Vector2(xMin, yMin)),
                GetWorldUnit(new Vector2(xMax, yMax)),
                GetWorldUnit(new Vector2(xMin, yMax)),
                GetWorldUnit(new Vector2(xMax, yMin))
            };

            // 计算物体的四个边
            for (float x = xMin + unitWidth; x < xMax; x += unitWidth)
            {
                tempUnits.Add(GetWorldUnit(new Vector2(x, yMin)));
                tempUnits.Add(GetWorldUnit(new Vector2(x, yMax)));
            }

            for (float y = yMin + unitWidth; y < yMax; y += unitWidth)
            {
                tempUnits.Add(GetWorldUnit(new Vector2(xMin, y)));
                tempUnits.Add(GetWorldUnit(new Vector2(xMax, y)));
            }
            return tempUnits;
        }


        // 添加新网格
        public void AddWorldUnit(Vector2Int index)
        {
            var worldUnit = worldUnitObjectPool.GetObject(index, unitWidth, index.x * unitWidth, index.y * unitWidth);
            WorldUnits.Add(index, worldUnit);
        }

        public Vector2Int GetWorldUnitIndex(Vector2 position)
        {
            return new Vector2Int((int)Math.Floor(position.x / unitWidth), (int)Math.Floor(position.y / unitWidth));
        }

        // 添加一个点物体到网格
        public Vector2Int AddObject(Vector2 position, string layerName, GameObject gameObject)
        {
            if (!CollisionLayerConfigSO.PassiveCollisionLayers.Contains(layerName) && !CollisionLayerConfigSO.ActiveCollisionLayers.Contains(layerName))
            {
                Debug.LogError("没有配置的碰撞层：" + layerName);
                return new Vector2Int(0, 0);
            }
            var worldUnit = GetWorldUnit(position);
            worldUnit.AddObject(layerName, gameObject);
            return worldUnit.index;
        }

        public HashSet<WorldUnit> AddObject(HashSet<WorldUnit> worldUnits, string layerName, GameObject gameObject)
        {
            foreach (var worldUnit in worldUnits)
            {
                worldUnit.AddObject(layerName, gameObject);
            }
            return worldUnits;
        }

        // 从网格移除点物体
        public void RemoveObject(Vector2 position, string layerName, GameObject gameObject)
        {
            var worldUnit = GetWorldUnit(position);
            worldUnit.RemoveObject(layerName, gameObject);
        }

        // 添加碰撞盒物体到网格
        public HashSet<WorldUnit> AddObject(CollisionBounds collisionBound, string layerName, GameObject gameObject)
        {
            if (!CollisionLayerConfigSO.ActiveCollisionLayers.Contains(layerName) && !CollisionLayerConfigSO.PassiveCollisionLayers.Contains(layerName))
            {
                Debug.LogError("没有配置的碰撞层：" + layerName);
                return new HashSet<WorldUnit>();
            }

            var units = GetWorldUnitGroup(collisionBound);
            foreach (var u in units)
            {
                u.AddObject(layerName, gameObject);
            }
            return units;
        }

        // 移除碰撞盒物体
        public void RemoveObject(CollisionBounds collisionBound, string layerName, GameObject gameObject)
        {
            foreach (var item in GetWorldUnitGroup(collisionBound))
            {
                item.RemoveObject(layerName, gameObject);
            }
        }

        public void RemoveObject(HashSet<WorldUnit> units, string layerName, GameObject gameObject)
        {
            foreach (var item in units)
            {
                item.RemoveObject(layerName, gameObject);
            }
        }


        // 根据空间获取网格
        public WorldUnit GetWorldUnit(Vector2 position)
        {
            var index = GetWorldUnitIndex(position);
            return GetWorldUnit(index);
        }

        public WorldUnit GetWorldUnit(Vector2Int index)
        {
            if (!WorldUnits.ContainsKey(index))
            {
                AddWorldUnit(index);
            }
            return WorldUnits[index];
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (this != null && !ShowDebugInfo) return;
            foreach (var unit in WorldUnits)
            {
                CollisionBounds bounds = unit.Value.CollisionBounds;
                var topLeft = new Vector2(bounds.xMin, bounds.yMax);
                var topRight = new Vector2(bounds.xMax, bounds.yMax);
                var bottomLeft = new Vector2(bounds.xMin, bounds.yMin);
                var bottomRight = new Vector2(bounds.xMax, bounds.yMin);

                Gizmos.DrawLine(topLeft, topRight);
                Gizmos.DrawLine(topRight, bottomRight);
                Gizmos.DrawLine(bottomRight, bottomLeft);
                Gizmos.DrawLine(bottomLeft, topLeft);
                Handles.Label(bottomLeft + new Vector2(0.2f, 0.2f), "(" + unit.Key.x + "," + unit.Key.y + "}");
                Handles.Label(bottomRight + new Vector2(-0.2f, 0.2f), unit.Value.objectCount.ToString());
            }
        }
    }
#endif
}