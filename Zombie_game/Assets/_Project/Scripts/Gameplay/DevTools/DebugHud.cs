using Game.Gameplay.Combat;
using Game.Gameplay.Player;
using Game.Gameplay.Weapons;
using Game.Gameplay.Waves;
using UnityEngine;

namespace Game.Gameplay.DevTools
{
    /// <summary>
    /// Temporary developer HUD (IMGUI) for the test range: health, stamina, movement state, ammo,
    /// crosshair with hit marker. Replaced by the real uGUI HUD later.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerWeaponController _weapons;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private bool _showHelp = true;

        private float _hitMarkerTimer;
        private bool _lastHitKilled;
        private bool _lastHitHeadshot;
        private string _lastHitText = "-";
        private GUIStyle _style;
        private GUIStyle _centerStyle;
        private GUIStyle _waveStyle;
        private DevEnemySpawner _spawner;
        private DevPlayerRespawn _respawn;
        private WaveDirector _waveDirector;

        private void OnEnable()
        {
            if (_weapons != null) _weapons.Hit += HandleHit;
        }

        private void OnDisable()
        {
            if (_weapons != null) _weapons.Hit -= HandleHit;
        }

        private void Start()
        {
            _spawner = FindFirstObjectByType<DevEnemySpawner>();
            if (_spawner != null && !_spawner.enabled) _spawner = null;
            _waveDirector = FindFirstObjectByType<WaveDirector>();
            _respawn = GetComponent<DevPlayerRespawn>();
        }

        private void Update()
        {
            if (_hitMarkerTimer > 0f) _hitMarkerTimer -= Time.deltaTime;
        }

        private void HandleHit(ShotHit hit)
        {
            if (hit.Target == null || hit.Damage <= 0f) return; // ignore walls and already-dead targets
            _hitMarkerTimer = 0.15f;
            _lastHitKilled = hit.Killed;
            _lastHitHeadshot = hit.Headshot;
            _lastHitText = $"{hit.Collider.name}: {hit.Damage:0.#}{(hit.Headshot ? " HEAD" : "")}{(hit.Killed ? " KILL" : "")}";
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
                _centerStyle = new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
                _waveStyle = new GUIStyle(_style) { fontSize = 19, fontStyle = FontStyle.Bold };
            }

            // Crosshair / hit marker
            var center = new Rect(Screen.width / 2f - 20f, Screen.height / 2f - 20f, 40f, 40f);
            string cross = _hitMarkerTimer > 0f
                ? (_lastHitKilled ? "<color=red>X</color>" : _lastHitHeadshot ? "<color=yellow>x</color>" : "x")
                : "+";
            GUI.Label(center, cross, _centerStyle);

            GUILayout.BeginArea(new Rect(12f, 12f, 460f, 460f), GUI.skin.box);
            if (_waveDirector != null)
            {
                int shownWave = Mathf.Max(1, _waveDirector.CurrentWave);
                GUILayout.Label($"WAVES CLEARED  {_waveDirector.ClearedWaveCount}/{_waveDirector.MaxWaves}", _waveStyle);
                GUILayout.Label($"WAVE  {shownWave}/{_waveDirector.MaxWaves}", _style);
                GUILayout.Label($"ENEMIES  {_waveDirector.AliveCount}/{_waveDirector.CurrentWaveEnemyCount}", _style);
                if (_waveDirector.IsBetweenWaves)
                {
                    GUILayout.Label($"NEXT WAVE IN  {FormatTime(_waveDirector.TimeUntilNextWave)}", _waveStyle);
                }
            }
            if (_health != null) GUILayout.Label($"HP  {_health.Current:0}/{_health.Max:0}", _style);
            if (_motor != null && _motor.Stamina != null)
            {
                GUILayout.Label($"Stamina  {_motor.Stamina.Current:0}{(_motor.Stamina.Exhausted ? "  <color=orange>EXHAUSTED</color>" : "")}", _style);
                GUILayout.Label($"Speed {_motor.HorizontalSpeed:0.0}  {(_motor.IsGrounded ? "ground" : "air")}"
                    + $"{(_motor.IsSprinting ? "  SPRINT" : "")}{(_motor.IsCrouching ? "  CROUCH" : "")}", _style);
            }
            if (_weapons != null && _weapons.Current != null)
            {
                WeaponInstance w = _weapons.Current;
                string reload = w.IsReloading ? $"  <color=cyan>RELOAD {w.ReloadProgress * 100f:0}%</color>" : "";
                GUILayout.Label($"[{_weapons.CurrentIndex + 1}] {w.Definition.DisplayName}  {w.Magazine}/{w.Reserve}{reload}", _style);
            }
            GUILayout.Label($"Last hit: {_lastHitText}", _style);
            if (_input != null) GUILayout.Label($"Keys: {_input.DebugHeldKeys}", _style);
            if (_spawner != null)
            {
                GUILayout.Label($"Enemies: {_spawner.AliveCount}  spawning {(_spawner.Spawning ? "ON" : "OFF")}  (F1 toggle, F2 clear)", _style);
            }
            if (_respawn != null && _respawn.IsWaitingToRespawn)
            {
                GUILayout.Label("<color=red>YOU DIED - respawning...</color>", _style);
            }

            if (_input != null && !_input.GameplayActive)
            {
                GUILayout.Label("<color=yellow>Click the Game view to play</color>", _style);
            }
            if (_showHelp)
            {
                GUILayout.Label("WASD move, Shift sprint, Ctrl/C crouch, Space jump\nLMB fire, R reload, 1-3 / wheel weapons, Esc free cursor", _style);
            }
            GUILayout.EndArea();
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
