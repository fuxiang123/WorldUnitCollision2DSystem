using System;
using UnityEngine;

namespace WorldUnitCollision2DSystem
{
    public static class CameraUtil
    {
        private static Camera _camera;
        private static bool _loggedNoMainCamera;

        // 当前目标是否超出摄像机范围
        public static bool IsOutOfCamera(Vector2 target)
        {
            if (!_camera)
            {
                _camera = Camera.main;
            }
            if (!_camera)
            {
#if UNITY_EDITOR
                throw new InvalidOperationException("Camera.main 为 null。请将某个相机的 Tag 设置为 MainCamera。");
#else
                if (!_loggedNoMainCamera)
                {
                    Debug.LogError("Camera.main 为 null。请将某个相机的 Tag 设置为 MainCamera。为避免误禁用碰撞体，将目标视为在镜头内。");
                    _loggedNoMainCamera = true;
                }
                return false;
#endif
            }
            var viewportPos = _camera!.WorldToViewportPoint(target);
            // 视口坐标：(0,0) 左下角, (1,1) 右上角，留一点容差
            return viewportPos.x < -0.01f || viewportPos.x > 1.01f || viewportPos.y < -0.01f || viewportPos.y > 1.01f;
        }
    }
}
