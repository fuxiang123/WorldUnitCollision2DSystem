using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
        // 待回收网格的索引：用 HashSet 去重，防止 Update 中途异常导致重复加入。
        private readonly HashSet<Vector2Int> _worldUnitsToRemove = new();
        // 防止日志刷屏：同一个网格“回收被跳过”的警告只记录一次。
        private readonly HashSet<Vector2Int> _worldUnitRemovalSkippedLogged = new();
        // 待触发碰撞的物体（使用结构体避免闭包GC）
        private readonly struct CollisionPair
        {
            public readonly AbstractCollider Active;
            public readonly AbstractCollider Other;
            public CollisionPair(AbstractCollider active, AbstractCollider other)
            {
                Active = active;
                Other = other;
            }
        }
        // Enter/Stay/Exit 的触发列表
        private readonly List<CollisionPair> _enterList = new();
        private readonly List<CollisionPair> _stayList = new();
        private readonly List<CollisionPair> _exitList = new();
        // 前后帧碰撞对集合（key = (idA,idB)，value = 对应的碰撞对）
        private Dictionary<(int, int), CollisionPair> _prevPairs = new();
        private Dictionary<(int, int), CollisionPair> _currentPairs = new();
        // WorldUnit 对象池
        private readonly WorldUnitObjectPool _worldUnitObjectPool = new();
#if UNITY_EDITOR
        [FormerlySerializedAs("ShowDebugInfo")] public bool showDebugInfo = true;
#endif
        void Awake()
        {
            Instance = this;
            if (CollisionLayerConfigSo == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置。");
#else
                Debug.LogError($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置，禁用 {nameof(WorldUnitCollision2DSystem)}。", this);
                enabled = false;
                return;
#endif
            }
        }

        void LateUpdate()
        {
            // 使用 LateUpdate：先让所有碰撞器在 Update 中完成注册/换格子，再在本帧末尾统一做碰撞检测，避免漏检/晚一帧。
            // 同时：即使用户回调抛异常，也要保证内部状态在本帧结束时被清理干净。
            _currentPairs.Clear();
            try
            {
                CheckCollision();
                BuildCollisionEvents();
                TriggerAllCollision();
            }
            finally
            {
                RemoveWorldUnits();
                _enterList.Clear();
                _stayList.Clear();
                _exitList.Clear();
                // 交换前后帧集合，复用内存，避免分配
                var tempPairs = _prevPairs;
                _prevPairs = _currentPairs;
                _currentPairs = tempPairs;
            }
        }

        // 触发所有的碰撞事件
        private void TriggerAllCollision()
        {
            TriggerList(_enterList, TriggerType.Enter);
            TriggerList(_stayList, TriggerType.Stay);
            TriggerList(_exitList, TriggerType.Exit);
        }

        private enum TriggerType
        {
            Enter,
            Stay,
            Exit
        }

        private void TriggerList(List<CollisionPair> list, TriggerType triggerType)
        {
            foreach (var pair in list)
            {
                if (pair.Active == null || pair.Other == null) continue;
                if (!pair.Active.isActiveAndEnabled || !pair.Other.isActiveAndEnabled) continue;

                try
                {
                    switch (triggerType)
                    {
                        case TriggerType.Enter:
                            pair.Active.OnTriggerEnter?.Invoke(pair.Other.gameObject, pair.Other.LayerName);
                            break;
                        case TriggerType.Stay:
                            pair.Active.OnTriggerStay?.Invoke(pair.Other.gameObject, pair.Other.LayerName);
                            break;
                        case TriggerType.Exit:
                            pair.Active.OnTriggerExit?.Invoke(pair.Other.gameObject, pair.Other.LayerName);
                            break;
                    }
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogException(ex, pair.Active);
                    throw;
#else
                    Debug.LogException(ex, pair.Active);
#endif
                }
            }
        }

        // 移除长时间不用的网格
        private void RemoveWorldUnits()
        {
            if (worldUnitRemoveTime <= 0) return;
            foreach (var index in _worldUnitsToRemove)
            {
                if (!WorldUnits.TryGetValue(index, out var item)) continue;

                // 安全防护：如果某个网格被加入“待回收”后又重新被注册了碰撞体（例如脚本执行顺序或回调副作用），
                // 则不要回收/入池；并记录一次警告以便排查。
                if (item.ObjectCount > 0)
                {
                    if (_worldUnitRemovalSkippedLogged.Add(index))
                        Debug.LogWarning($"WorldUnit {index} 已加入待回收列表，但当前包含 {item.ObjectCount} 个碰撞体；跳过回收。", this);
                    item.LastCollisionTime = 0;
                    continue;
                }
                _worldUnitRemovalSkippedLogged.Remove(index);

                _worldUnitObjectPool.ReturnObject(item);
                WorldUnits.Remove(index);
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
                    _worldUnitsToRemove.Add(worldUnit.Index);
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
            
            if (activeCld is WNCBoxCollider activeBox && otherCld is WNCBoxCollider otherBox)
            {
                isCollision = IsCollision(activeBox.GetBounds(), otherBox.GetBounds());
            }
            else if (activeCld is WNCBoxCollider activeBox2 && otherCld is WNCPointCollider)
            {
                isCollision = IsCollision(activeBox2.GetBounds(), otherCld.transform.position);
            }
            else if (activeCld is WNCPointCollider && otherCld is WNCBoxCollider otherBox2)
            {
                isCollision = IsCollision(otherBox2.GetBounds(), activeCld.transform.position);
            }
            // 点碰撞器 vs 点碰撞器：不做碰撞检测，直接 return
            
            if (isCollision)
            {
                // 碰撞去重：防止同一对物体因跨越多个网格而被多次检测
                int idA = activeCld.GetInstanceID();
                int idB = otherCld.GetInstanceID();
                var pairKey = idA < idB ? (idA, idB) : (idB, idA);
                if (_currentPairs.ContainsKey(pairKey)) return;
                _currentPairs.Add(pairKey, new CollisionPair(activeCld, otherCld));
            }
        }

        // 根据前后帧碰撞对集合生成 Enter/Stay/Exit 事件列表
        private void BuildCollisionEvents()
        {
            foreach (var pair in _currentPairs)
            {
                if (_prevPairs.ContainsKey(pair.Key))
                    _stayList.Add(pair.Value);
                else
                    _enterList.Add(pair.Value);
            }

            foreach (var pair in _prevPairs)
            {
                if (!_currentPairs.ContainsKey(pair.Key))
                    _exitList.Add(pair.Value);
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
            // 直接用索引计算，避免浮点遍历的累积误差
            var minIndex = GetWorldUnitIndex(new Vector2(bound.XMin, bound.YMin));
            var maxIndex = GetWorldUnitIndex(new Vector2(bound.XMax, bound.YMax));
            
            HashSet<WorldUnit> tempUnits = new HashSet<WorldUnit>();
            for (int x = minIndex.x; x <= maxIndex.x; x++)
            {
                for (int y = minIndex.y; y <= maxIndex.y; y++)
                {
                    tempUnits.Add(GetWorldUnit(new Vector2Int(x, y)));
                }
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
            if (CollisionLayerConfigSo == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置。");
#else
                Debug.LogError($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置，忽略碰撞体注册。", this);
                return default;
#endif
            }
            if (!CollisionLayerConfigSo.ContainsLayer(layerName))
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

        // 从网格移除点物体（通过位置）
        public void RemoveCollider(Vector2 position, string layerName, AbstractCollider cld)
        {
            var worldUnit = GetWorldUnit(position);
            worldUnit.RemoveCollider(layerName, cld);
        }

        // 从网格移除点物体（通过索引，避免浮点精度问题）
        public void RemoveCollider(Vector2Int index, string layerName, AbstractCollider cld)
        {
            if (WorldUnits.TryGetValue(index, out var worldUnit))
            {
                worldUnit.RemoveCollider(layerName, cld);
            }
        }

        // 添加碰撞盒物体到网格
        public HashSet<WorldUnit> AddCollider(CollisionBounds collisionBound, string layerName, AbstractCollider cld)
        {
            if (CollisionLayerConfigSo == null)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置。");
#else
                Debug.LogError($"{nameof(CollisionLayerConfigSo)} 未在 {name} 上设置，忽略碰撞体注册。", this);
                return new HashSet<WorldUnit>();
#endif
            }
            if (!CollisionLayerConfigSo.ContainsLayer(layerName))
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
            if (!WorldUnits.TryGetValue(index, out var worldUnit))
            {
                worldUnit = _worldUnitObjectPool.GetObject(index, unitWidth, index.x * unitWidth, index.y * unitWidth);
                WorldUnits.Add(index, worldUnit);
            }
            return worldUnit;
        }

        public void Reset()
        {
            WorldUnits.Clear();
            _worldUnitsToRemove.Clear();
            _worldUnitRemovalSkippedLogged.Clear();
            _enterList.Clear();
            _stayList.Clear();
            _exitList.Clear();
            _prevPairs.Clear();
            _currentPairs.Clear();
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
                Handles.Label(bottomLeft + new Vector2(0.2f, 0.2f), "(" + unit.Key.x + "," + unit.Key.y + ")");
                Handles.Label(bottomRight + new Vector2(-0.2f, 0.2f), unit.Value.ObjectCount.ToString());
            }
        }
#endif
    }
}
