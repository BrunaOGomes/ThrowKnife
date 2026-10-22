using System;
using UnityEngine;

namespace Group3
{
    public class ThrowController : MonoBehaviour
    {
        [Header("Dependências do Grupo 3")]
        public HandTracking handTracking;

        [Header("Referência ao Grupo 2 (contrato)")]
        [Tooltip("Objeto que tem o método público bool TentarPegarFaca(Vector3 maoMundo).")]
        public MonoBehaviour gameManagerBehaviour;

        private Func<Vector3, bool> _tentarPegarFaca;

        // Contratos Grupo 3 -> Grupo 2
        public event Action OnFacaColidiu;
        public event Action OnFacaSaiuDaCena;
        public event Action<LaunchData> OnLancamento;

        [Header("Faca (prefab)")]
        public Rigidbody knifePrefab;
        public Transform palmAnchor;

        [Header("Lançamento")]
        public float minThrowSpeed = 2.5f;
        public float throwBoost = 1.2f;
        public float upwardBoost = 0.3f;

        [Header("Física")]
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        public float torqueOnThrow = 5f;

        [Header("Colisão com balões")]
        public float balloonPlaneZ = 5f;
        public float hitRadius = 0.6f;
        public Transform[] balloons;
        public float outOfSceneZ = 20f;

        // ---- estado interno ----
        private HandVelocityCalculator _calc = new HandVelocityCalculator();
        private HandFrameData _prev;
        private bool _hasPrev;
        private bool _holding;
        private Rigidbody _knife;
        private Vector3 _initialVel;
        private bool _resolved;
        private Transform _hitBalloon;

        void Awake()
        {
            if (gameManagerBehaviour != null)
            {
                var m = gameManagerBehaviour.GetType().GetMethod("TentarPegarFaca");
                if (m != null)
                    _tentarPegarFaca = (Func<Vector3, bool>)Delegate.CreateDelegate(
                        typeof(Func<Vector3, bool>), gameManagerBehaviour, m);
                else
                    Debug.LogError("[ThrowController] gameManagerBehaviour não tem TentarPegarFaca(Vector3).", this);
            }
            else
            {
                Debug.LogError("[ThrowController] gameManagerBehaviour não atribuído.", this);
            }
        }

        void Update()
        {
            if (handTracking == null) return;

            HandFrameData f = handTracking.CurrentFrame;
            _calc.Update(f);

            if (_hasPrev && f.IsValid)
            {
                bool fechou = !_prev.MaoFechada && f.MaoFechada;
                bool abriu = _prev.MaoFechada && !f.MaoFechada;

                // Se fechou a mão e ainda não está segurando, tenta pegar.
                if (fechou && !_holding)
                {
                    TryGrab(f.PalmWorldPos);
                }

                // Se abriu a mão e ESTÁ segurando, lança ou dropa.
                if (abriu && _holding)
                {
                    if (_calc.Speed >= minThrowSpeed)
                    {
                        Throw(f);
                    }
                    else
                    {
                        Drop();
                    }
                }
            }

            _prev = f;
            _hasPrev = true;
        }

        void FixedUpdate()
        {
            if (_knife == null || _resolved) return;

            _knife.AddForce(gravity, ForceMode.Acceleration);

            Vector3 pos = _knife.position;
            bool cruzou = (_initialVel.z > 0f && pos.z >= balloonPlaneZ)
                       || (_initialVel.z < 0f && pos.z <= balloonPlaneZ);

            if (cruzou && TryHitBalloon(pos))
            {
                _resolved = true;
                OnFacaColidiu?.Invoke();
                Destroy(_knife.gameObject);
                _knife = null;
                return;
            }

            if (Mathf.Abs(pos.z) > outOfSceneZ || Mathf.Abs(pos.x) > outOfSceneZ || pos.y < -10f)
            {
                _resolved = true;
                OnFacaSaiuDaCena?.Invoke();
                Destroy(_knife.gameObject);
                _knife = null;
            }
        }

        private void TryGrab(Vector3 maoMundo)
        {
            if (_holding || _tentarPegarFaca == null) return;
            if (!_tentarPegarFaca(maoMundo)) return;

            _knife = Instantiate(knifePrefab, palmAnchor.position, palmAnchor.rotation);
            _knife.isKinematic = true;
            _knife.useGravity = false;
            _knife.transform.SetParent(palmAnchor, worldPositionStays: true);
            _holding = true;
            _resolved = false;
            Debug.Log("[ThrowController] Faca presa na mão.");
        }

        private void Throw(HandFrameData f)
        { 
            if (_knife == null) return;

            _knife.transform.SetParent(null, worldPositionStays: true);
            _knife.isKinematic = false;
            _knife.useGravity = false;

            Vector3 vel = _calc.Direction * (_calc.Speed * throwBoost);
            vel.y += upwardBoost;
            _initialVel = vel;

            _knife.linearVelocity = vel;   // Unity 6; use .velocity no < 6
            _knife.angularVelocity = UnityEngine.Random.onUnitSphere * torqueOnThrow;

            _holding = false;

            var launch = new LaunchData
            {
                Posicao = new float[] { f.PalmWorldPos.x, f.PalmWorldPos.y, f.PalmWorldPos.z },
                Yaw = f.Yaw,
                Pitch = f.Pitch,
                Velocidade = new float[] { vel.x, vel.y, vel.z },
                Timestamp = f.Timestamp,
            };
            OnLancamento?.Invoke(launch);
            Debug.Log($"[ThrowController] LANÇOU | {launch.ToJson()}");
        }

        private void Drop()
        {
            if (_knife == null) return;
            _knife.transform.SetParent(null, worldPositionStays: true);
            _knife.isKinematic = false;
            _knife.useGravity = true;
            _knife.linearVelocity = Vector3.zero;
            _knife.angularVelocity = Vector3.zero;
            _holding = false;

            _resolved = true;
            OnFacaSaiuDaCena?.Invoke();
            Debug.Log("[ThrowController] Soltou sem velocidade.");
        }

        private bool TryHitBalloon(Vector3 knifePos)
        {
            _hitBalloon = null;
            if (balloons == null) return false;

            Vector2 k = new Vector2(knifePos.x, knifePos.y);
            float best = hitRadius;
            foreach (var b in balloons)
            {
                if (b == null) continue;
                float d = Vector2.Distance(k, new Vector2(b.position.x, b.position.y));
                if (d <= best) { best = d; _hitBalloon = b; }
            }
            return _hitBalloon != null;
        }
    }
}