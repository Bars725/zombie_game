using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay.Weapons
{
    /// <summary>
    /// Shows the first-person model of the current weapon under a socket on the camera,
    /// with simple procedural feedback: recoil kick on every shot, lowered + tilted while reloading.
    /// Weapons without a ViewModel use the placeholder object.
    /// </summary>
    public sealed class WeaponViewModel : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController _weapons;
        [Tooltip("Parent for view models; its local pose is the 'rest' pose")]
        [SerializeField] private Transform _socket;
        [SerializeField] private GameObject _placeholder;

        [Header("Recoil")]
        [SerializeField] private float _kickBack = 0.05f;
        [SerializeField] private float _kickPitch = 6f;
        [SerializeField] private float _kickRecovery = 18f;

        [Header("Reload pose")]
        [SerializeField] private float _reloadDrop = 0.12f;
        [SerializeField] private float _reloadTilt = 30f;
        [SerializeField] private float _poseBlendSpeed = 10f;

        private readonly Dictionary<WeaponDefinition, GameObject> _instances = new Dictionary<WeaponDefinition, GameObject>();
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _kick;
        private float _reloadBlend;

        private void Awake()
        {
            if (_socket != null)
            {
                _restPosition = _socket.localPosition;
                _restRotation = _socket.localRotation;
            }
        }

        private void OnEnable()
        {
            if (_weapons == null) return;
            _weapons.WeaponChanged += HandleWeaponChanged;
            _weapons.Fired += HandleFired;
        }

        private void OnDisable()
        {
            if (_weapons == null) return;
            _weapons.WeaponChanged -= HandleWeaponChanged;
            _weapons.Fired -= HandleFired;
        }

        private void Start()
        {
            // The controller equipped its first weapon in Awake, before we subscribed.
            if (_weapons != null) Show(_weapons.Current?.Definition);
        }

        private void LateUpdate()
        {
            if (_socket == null) return;
            float dt = Time.deltaTime;

            _kick = Mathf.MoveTowards(_kick, 0f, _kickRecovery * dt * Mathf.Max(0.2f, _kick));
            bool reloading = _weapons != null && _weapons.Current != null && _weapons.Current.IsReloading;
            _reloadBlend = Mathf.MoveTowards(_reloadBlend, reloading ? 1f : 0f, _poseBlendSpeed * dt);
            float reload = Mathf.SmoothStep(0f, 1f, _reloadBlend);

            _socket.localPosition = _restPosition
                + new Vector3(0f, -_reloadDrop * reload, -_kickBack * _kick);
            _socket.localRotation = _restRotation
                * Quaternion.Euler(-_kickPitch * _kick + _reloadTilt * reload, 0f, -_reloadTilt * 0.5f * reload);
        }

        private void HandleFired(WeaponInstance weapon) => _kick = 1f;

        private void HandleWeaponChanged(WeaponInstance weapon) => Show(weapon?.Definition);

        private void Show(WeaponDefinition definition)
        {
            GameObject target = null;
            if (definition != null && definition.ViewModel != null)
            {
                if (!_instances.TryGetValue(definition, out target) || target == null)
                {
                    target = Instantiate(definition.ViewModel, _socket, false);
                    target.name = definition.ViewModel.name;
                    foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
                    {
                        r.shadowCastingMode = ShadowCastingMode.Off; // a floating gun shadow looks wrong
                    }
                    foreach (Collider c in target.GetComponentsInChildren<Collider>(true)) Destroy(c);
                    _instances[definition] = target;
                }
            }

            foreach (GameObject go in _instances.Values)
            {
                if (go != null) go.SetActive(go == target);
            }
            if (_placeholder != null) _placeholder.SetActive(target == null);
        }
    }
}
