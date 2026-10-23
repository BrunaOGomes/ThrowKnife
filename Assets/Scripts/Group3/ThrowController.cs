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

            // Mantém a faca na palma sem trocar o pai do objeto durante o Play Mode.
            if (_holding && _knife != null && palmAnchor != null)
                _knife.transform.SetPositionAndRotation(palmAnchor.position, palmAnchor.rotation);

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
            // ADICIONE ESTA LINHA: Se a faca foi destruída OU se ela se tornou Kinematic (cravou na parede), ignora o resto
            if (_knife == null || _resolved || _knife.isKinematic) return;

            _knife.AddForce(gravity, ForceMode.Acceleration);

            Vector3 pos = _knife.position;
            bool cruzou = (_initialVel.z > 0f && pos.z >= balloonPlaneZ) || (_initialVel.z < 0f && pos.z <= balloonPlaneZ);

            if (cruzou && TryHitBalloon(pos))
            {
                _resolved = true;
                OnFacaColidiu?.Invoke();
                //Destroy(_knife.gameObject); // A faca acertou o balão, então é destruída
                return;
            }

            if (Mathf.Abs(pos.z) > outOfSceneZ || Mathf.Abs(pos.x) > outOfSceneZ || pos.y < -10f)
            {
                _resolved = true;
                OnFacaSaiuDaCena?.Invoke();
                //Destroy(_knife.gameObject);
            }
        }

        public void NotificarFacaCravada(GameObject faca)
        {
            if (_knife == null || _knife.gameObject != faca || _resolved) return;

            _resolved = true;
            _holding = false;
            OnFacaSaiuDaCena?.Invoke();
        }

        private void TryGrab(Vector3 maoMundo)
        {
            if (_holding || _tentarPegarFaca == null) return;
            if (palmAnchor == null)
            {
                Debug.LogError("[ThrowController] Palm Anchor não atribuído no Inspector!");
                return;
            }
            if (facasNaMesa == null || _indiceFacaAtual >= facasNaMesa.Length) return;
            if (!_tentarPegarFaca(maoMundo)) return;

            _knife = facasNaMesa[_indiceFacaAtual];
            _indiceFacaAtual++;

            // 1. Ativa a faca na cena
            _knife.gameObject.SetActive(true);

            // 2. Desliga a física para ela não cair nem tremer
            _knife.isKinematic = true;
            _knife.detectCollisions = true;
            _knife.useGravity = false;
            _knife.angularVelocity = Vector3.zero;

            // 3. Posiciona a faca na palma sem alterar a hierarquia da cena.
            _knife.transform.SetPositionAndRotation(palmAnchor.position, palmAnchor.rotation);

            _holding = true;
            _resolved = false;
            Debug.Log($"[ThrowController] Faca {_indiceFacaAtual} presa na mão.");
        }

        private void Throw(HandFrameData f)
        {
            if (_knife == null) return;

            // Garante que a física está ativa e funcionando
            _knife.isKinematic = false;
            _knife.useGravity = false;
            _knife.detectCollisions = true;

            // IMPORTANTE: Força o Unity a atualizar o Rigidbody AGORA, antes de aplicar a velocidade
            _knife.linearVelocity = Vector3.zero;
            _knife.angularVelocity = Vector3.zero;

            Vector3 vel = _calc.Direction * (_calc.Speed * throwBoost);
            vel.y += upwardBoost;
            vel.z = Mathf.Max(vel.z, 5f);

            _initialVel = vel;

            // Aplica a velocidade
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
