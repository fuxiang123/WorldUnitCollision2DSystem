
using UnityEngine;

namespace WorldUnitCollisionSystem
{
    public class CameraUtil
    {
        private static Camera camera;

        // 当前目标是否超出摄像机范围
        public static bool IsOutOfCamera(Vector2 target)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }
            var targetScreenPos = camera.WorldToScreenPoint(target);
            return targetScreenPos.x < -1 || targetScreenPos.x > Screen.width + 1 || targetScreenPos.y < -1 || targetScreenPos.y > Screen.height + 1;
        }
    }
}