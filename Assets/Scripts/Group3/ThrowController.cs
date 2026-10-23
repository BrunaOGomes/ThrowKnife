using System;
using UnityEngine;

namespace Group3
{
    public class ThrowController : MonoBehaviour
    {
        [Header("Dependências do Grupo 3")]
        public HandTracking handTracking;

        [Header("Referência ao Grupo 2 (contrato)")]
        public MonoBehaviour gameManagerBehaviour;
        private Func<Vector3, bool> _tentarPegarFaca;

        public event Action OnFacaColidiu;
        public event Action OnFacaSaiuDaCena;
        public event Action<LaunchData> OnLancamento;

        [Header("Facas na Cena (Ordem de uso)")]
        [Tooltip("Arraste as 3 facas da mesa aqui, na ordem que devem ser pegas.")]
        public Rigidbody[] facasNaMesa;
        private int _indiceFacaAtual = 0;

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

        private HandVelocityCalculator _calc = new HandVelocityCalculator();
        private HandFrameData _prev;
        private bool _hasPrev;
        private bool _holding;
        private Rigidbody _knife;
        private Vector3 _initialVel;
        private bool _resolved;

        void Awake()
        {
            if (gameManagerBehaviour != null)
            {
                var m = gameManagerBehaviour.GetType().GetMethod("TentarPegarFaca");
                if (m != null)
                    _tentarPegarFaca = (Func<Vector3, bool>)Delegate.CreateDelegate(typeof(Func<Vector3, bool>), gameManagerBehaviour, m);
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

                if (fechou && !_holding) TryGrab(f.PalmWorldPos);

                if (abriu && _holding)
                {
                    if (_calc.Speed >= minThrowSpeed) Throw(f);
                    else Drop();
                }
            }
            _prev = f; _hasPrev = true;
        }

        void FixedUpdate()
        {
            if (_knife == null || _resolved) return;

            _knife.AddForce(gravity, ForceMode.Acceleration);

            Vector3 pos = _knife.position;
            bool cruzou = (_initialVel.z > 0f && pos.z >= balloonPlaneZ) || (_initialVel.z < 0f && pos.z <= balloonPlaneZ);

            if (cruzou && TryHitBalloon(pos))
            {
                _resolved = true;
                OnFacaColidiu?.Invoke();
                Destroy(_knife.gameObject); // A faca acertou o balão, então é destruída
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

            if (_indiceFacaAtual >= facasNaMesa.Length) return;

            _knife = facasNaMesa[_indiceFacaAtual];
            _indiceFacaAtual++;

            // 1. Ativa a faca na cena
            _knife.gameObject.SetActive(true);

            // 2. Desliga a física para ela não cair nem tremer
            _knife.isKinematic = true;
            _knife.useGravity = false;
            _knife.linearVelocity = Vector3.zero;
            _knife.angularVelocity = Vector3.zero;

            // 3. Cola a faca na mão (Zera a posição local para ficar no centro da mão)
            _knife.transform.SetParent(palmAnchor);
            _knife.transform.localPosition = Vector3.zero;
            _knife.transform.localRotation = Quaternion.identity;

            _holding = true;
            _resolved = false;
            Debug.Log($"[ThrowController] Faca {_indiceFacaAtual} presa na mão.");
        }

        private void Throw(HandFrameData f)
        {
            if (_knife == null) return;

            // 1. Descola a faca da mão
            _knife.transform.SetParent(null);

            // 2. Liga a física novamente
            _knife.isKinematic = false;
            _knife.useGravity = false;

            // 3. Calcula a velocidade
            Vector3 vel = _calc.Direction * (_calc.Speed * throwBoost);
            vel.y += upwardBoost;

            // Força o Z para frente para garantir que a faca vá para os balões
            vel.z = Mathf.Max(vel.z, 5f);

            _initialVel = vel;

            // 4. Aplica a velocidade para a faca voar
            _knife.linearVelocity = vel;
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
        }

        private bool TryHitBalloon(Vector3 knifePos)
        {
            if (balloons == null) return false;
            Vector2 k = new Vector2(knifePos.x, knifePos.y);
            float best = hitRadius;
            foreach (var b in balloons)
            {
                if (b == null || !b.gameObject.activeSelf) continue; // Só checa balões ativos
                float d = Vector2.Distance(k, new Vector2(b.position.x, b.position.y));
                if (d <= best) { best = d; return true; }
            }
            return false;
        }
    }
}