using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    // 对象池管理类
    public class WorldUnitObjectPool
    {
        private readonly List<WorldUnit> _pool = new();

        public WorldUnit GetObject(Vector2Int index, float size, float xMin, float yMin)
        {
            WorldUnit unit;
            if (_pool.Count > 0)
            {
                unit = _pool[^1];
                _pool.RemoveAt(_pool.Count - 1);
                unit.Reset(index, size, xMin, yMin);
            }
            else
            {
                unit = new WorldUnit(index, size, xMin, yMin);
            }
            return unit;
        }

        public void ReturnObject(WorldUnit unit)
        {
            _pool.Add(unit);
        }
    }

    // 单个网格
    public class WorldUnit
    {
        public Vector2Int Index;
        public float Size; // 格子大小
        public CollisionBounds CollisionBounds;
        public int ObjectCount { get; private set; } = 0;
        // 长时间没有出现碰撞的话，进行销毁
        public float LastCollisionTime = 0;

        public WorldUnit(Vector2Int index, float size, float xMin, float yMin)
        {
            this.Index = index;
            this.Size = size;
            CollisionBounds.XMin = xMin;
            CollisionBounds.XMax = xMin + size;
            CollisionBounds.YMin = yMin;
            CollisionBounds.YMax = yMin + size;
        }

        public void Reset(Vector2Int index, float size, float xMin, float yMin)
        {
            this.Index = index;
            this.Size = size;
            CollisionBounds.XMin = xMin;
            CollisionBounds.XMax = xMin + size;
            CollisionBounds.YMin = yMin;
            CollisionBounds.YMax = yMin + size;
            ObjectCount = 0;
            LastCollisionTime = 0;
            // 清除残留的碰撞体数据
            foreach (var pair in LayerObjects)
            {
                pair.Value.Clear();
            }
        }

        // 按层名（layerName）存储碰撞体
        public Dictionary<string, HashSet<AbstractCollider>> LayerObjects = new();

        public void AddCollider(string layerName, AbstractCollider cld)
        {
            if (!LayerObjects.TryGetValue(layerName, out var set))
            {
                set = new HashSet<AbstractCollider>();
                LayerObjects.Add(layerName, set);
            }
            if (set.Add(cld))
            {
                ObjectCount++;
            }
        }

        public void RemoveCollider(string layerName, AbstractCollider cld)
        {
            if (!LayerObjects.TryGetValue(layerName, out var set)) return;
            if (set.Remove(cld))
            {
                ObjectCount--;
            }
        }

        public void Clear(string layerName)
        {
            ObjectCount -= LayerObjects[layerName].Count;
            LayerObjects[layerName].Clear();
        }

        public void ClearAll()
        {
            ObjectCount = 0;
            foreach (var item in LayerObjects)
            {
                item.Value.Clear();
            }
        }
    }
}
