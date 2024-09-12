using System.Collections.Generic;
using UnityEngine;

namespace WorldUnitCollisionSystem
{
    // 对象池管理类
    public class WorldUnitObjectPool
    {
        private List<WorldUnit> pool = new List<WorldUnit>();

        public WorldUnit GetObject(Vector2Int index, float size, float xMin, float yMin)
        {
            WorldUnit unit;
            if (pool.Count > 0)
            {
                unit = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
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
            pool.Add(unit);
        }
    }

    // 单个网格
    public class WorldUnit
    {
        public Vector2Int index;
        public float size; // 格子大小
        public CollisionBounds CollisionBounds;
        public int objectCount { get; private set; } = 0;
        // 长时间没有出现碰撞的话，进行销毁
        public float lastCollisionTime = 0;

        public WorldUnit(Vector2Int index, float size, float xMin, float yMin)
        {
            this.index = index;
            this.size = size;
            CollisionBounds.xMin = xMin;
            CollisionBounds.xMax = xMin + size;
            CollisionBounds.yMin = yMin;
            CollisionBounds.yMax = yMin + size;
        }

        public void Reset(Vector2Int index, float size, float xMin, float yMin)
        {
            this.index = index;
            this.size = size;
            CollisionBounds.xMin = xMin;
            CollisionBounds.xMax = xMin + size;
            CollisionBounds.yMin = yMin;
            CollisionBounds.yMax = yMin + size;
            objectCount = 0; // 重置物体计数
        }

        // 顶过layerName存储物体
        public Dictionary<string, HashSet<GameObject>> LayerObjects = new Dictionary<string, HashSet<GameObject>>();

        public void AddObject(string layerName, GameObject gameObject)
        {
            objectCount++;
            if (!LayerObjects.ContainsKey(layerName))
            {
                LayerObjects.Add(layerName, new HashSet<GameObject>());
            }
            LayerObjects[layerName].Add(gameObject);
        }

        public void RemoveObject(string layerName, GameObject gameObject)
        {
            objectCount--;
            if (!LayerObjects.ContainsKey(layerName)) return;
            LayerObjects[layerName].Remove(gameObject);
        }

        public void Clear(string layerName)
        {
            objectCount -= LayerObjects[layerName].Count;
            LayerObjects[layerName].Clear();
        }

        public void ClearAll()
        {
            objectCount = 0;
            foreach (var item in LayerObjects)
            {
                item.Value.Clear();
            }
        }
    }
}