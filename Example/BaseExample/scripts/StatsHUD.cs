using UnityEngine;

namespace WorldUnitCollision2DSystem.Example
{
    public class StatsHUD : MonoBehaviour
    {
        public int fontSize = 24;
        public Color textColor = Color.white;
        public Vector2 padding = new Vector2(20f, 20f);
        public float lineHeight = 24f;

        private GUIStyle _style;
        private GameManager _gameManager;
        private bool _triedFindGameManager;

        private void Awake()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle
                {
                    fontSize = fontSize,
                    normal = { textColor = textColor }
                };
            }

            float x = Screen.width - 220f - padding.x;
            float y = Screen.height - padding.y - lineHeight;

            DrawLine(x, ref y, $"当前子弹数量: {GetBulletCount()}");
            DrawLine(x, ref y, $"当前敌人数量: {GetEnemyCount()}");
            DrawLine(x, ref y, $"当前FPS: {GetFps()}");
        }

        private void DrawLine(float x, ref float y, string text)
        {
            GUI.Label(new Rect(x, y, 220f, lineHeight), text, _style);
            y -= lineHeight;
        }

        private int GetBulletCount()
        {
            return BulletObjectPool.Instance ? BulletObjectPool.Instance.ActiveBulletCount : 0;
        }

        private int GetEnemyCount()
        {
            return EnemyPoolManager.Instance ? EnemyPoolManager.Instance.ActiveEnemyCount : 0;
        }

        private int GetFps()
        {
            if (_gameManager == null && !_triedFindGameManager)
            {
                _gameManager = FindObjectOfType<GameManager>();
                _triedFindGameManager = true;
            }
            return _gameManager ? _gameManager.FPS : 0;
        }
    }
}
