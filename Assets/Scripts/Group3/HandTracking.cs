using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Group3
{
    public class HandTracking : MonoBehaviour
    {
        [Header("Fonte (Grupo 1 -> socket)")]
        public UDPReceive udpReceive;

        [Header("Renderização da mão (21 GameObjects)")]
        public GameObject[] handPoints;
        public float scale = 2f;

        [Header("Referência da palma")]
        public int palmIndex = 0;

        [Header("Rotação do HandRoot")]
        public Transform handRoot;
        public bool rotateHandRoot = true;
        public bool preserveSkeleton = true;
        public Vector3 handRootEulerOffset = Vector3.zero;
        public bool invertYaw = false;
        public bool invertPitch = false;
        public bool invertZ = true;

        [Header("Mapeamento da palma -> mundo (só mão)")]
        [Tooltip("Centro da área navegável. Dica: coloque IGUAL ao estoqueCentro do GameManager.")]
        public Vector3 worldCenter = new Vector3(0f, 1.5f, 0f);
        [Tooltip("Quanto a mão anda no mundo quando vai de 0..1 na câmera.")]
        public Vector3 worldSize = new Vector3(6f, 4f, 0f);
        [Tooltip("Suavização da posição do HandRoot (maior = mais rápido, mede velocidade maior no arremesso).")]
        public float positionSmoothSpeed = 15f;
        [Tooltip("Se true, usa o Z do MediaPipe para profundidade. Se false, Z fica fixo em worldCenter.z (recomendado p/ pega).")]
        public bool mapDepthToZ = false;
        public float depthScale = 2f;
        [Tooltip("Aproximar a mão da câmera empurra ela pra frente no mundo (usa o tamanho da mão como profundidade). É o que gera velocidade Z no arremesso.")]
        public bool useHandSizeForDepth = true;
        [Tooltip("Tamanho da mão (0-9) quando longe / perto (coordenadas normalizadas).")]
        public float sizeFar = 0.12f;
        public float sizeNear = 0.30f;
        [Tooltip("Quantos metros pra frente a mão anda quando vai de longe pra perto.")]
        public float depthRange = 4.5f;

        [Header("Sinal (só mão)")]
        [Tooltip("Sem pacote novo por esse tempo, considera mão perdida (IsValid=false). Python só envia quando vê a mão.")]
        public float lostTimeout = 0.5f;
        [Tooltip("Frames fechados seguidos p/ confirmar pega (debounce, evita flicker).")]
        public int closeFramesRequired = 3;
        [Tooltip("Frames abertos seguidos p/ confirmar soltura. 1 = soltura no instante do pico (melhor p/ arremessar).")]
        public int openFramesRequired = 1;

        [Header("Debug")]
        public bool logEveryFrame = false;

        public HandFrameData CurrentFrame { get; private set; }

        private string _lastData;
        private float _lastReceiveTime = -100f;
        private bool _hasPosition;
        private bool _debouncedClosed;
        private int _closeCount;
        private int _openCount;

        void Awake()
        {
            if (handRoot == null && handPoints != null)
            {
                foreach (GameObject point in handPoints)
                {
                    if (point == null) continue;
                    Transform points = point.transform.parent;
                    if (points == null) continue;
                    handRoot = points.parent != null ? points.parent : points;
                    break;
                }
            }
        }

        void Update()
        {
            string data = udpReceive != null ? udpReceive.data : null;
            if (string.IsNullOrEmpty(data))
            {
                if (Time.time - _lastReceiveTime > lostTimeout)
                    CurrentFrame = default;
                return;
            }

            // Python só envia UDP quando detecta a mão.
            // Se o dado é igual ao anterior, não há pacote novo: mantém o frame,
            // mas invalida após timeout para não congelar em "fechada".
            if (data == _lastData)
            {
                if (Time.time - _lastReceiveTime > lostTimeout)
                    CurrentFrame = default;
                return;
            }

            JObject frame;
            try { frame = JObject.Parse(data); }
            catch { return; }

            if (!(frame["landmarks"] is JArray lms) || lms.Count < 21) return;

            // Pacote novo e válido.
            _lastData = data;
            _lastReceiveTime = Time.time;

            long ts = frame.Value<long?>("timestamp") ?? 0L;
            bool maoFechRaw = frame.Value<bool?>("mao_fechada") ?? false;
            float yaw = frame.Value<float?>("yaw") ?? 0f;
            float pitch = frame.Value<float?>("pitch") ?? 0f;

            // Debounce da mão (histerese simples, sem mexer no ThrowController).
            if (maoFechRaw)
            {
                _openCount = 0;
                _closeCount++;
                if (_closeCount >= Mathf.Max(1, closeFramesRequired))
                    _debouncedClosed = true;
            }
            else
            {
                _closeCount = 0;
                _openCount++;
                if (_openCount >= Mathf.Max(1, openFramesRequired))
                    _debouncedClosed = false;
            }
            bool maoFech = _debouncedClosed;

            int anchorIdx = Mathf.Clamp(palmIndex, 0, 20);
            float ax = ((JArray)lms[anchorIdx])[0].Value<float>();
            float ay = ((JArray)lms[anchorIdx])[1].Value<float>();
            float az = ((JArray)lms[anchorIdx])[2].Value<float>();

            // 1. Posição absoluta da mão no mundo a partir da palma na imagem.
            // Antes: HandRoot.position ficava fixo e a palma nunca saía de perto dele,
            // então nunca entrava no estoque do GameManager.
            // Z: o Z do MediaPipe é relativo ao pulso (~0), não serve como distância.
            // Usa o TAMANHO da mão (0-9) como profundidade: aproximar da câmera = ir pra frente.
            float depthOffset = 0f;
            float handSize = 0f;
            if (useHandSizeForDepth)
            {
                float bRootX = ((JArray)lms[9])[0].Value<float>() - ax;
                float bRootY = ((JArray)lms[9])[1].Value<float>() - ay;
                handSize = Mathf.Sqrt(bRootX * bRootX + bRootY * bRootY);
                float denom = Mathf.Max(1e-4f, sizeNear - sizeFar);
                float closeness = Mathf.Clamp01((handSize - sizeFar) / denom);
                depthOffset = closeness * depthRange;
            }
            else if (mapDepthToZ)
            {
                depthOffset = (invertZ ? -az : az) * depthScale;
            }
            Vector3 targetPos = worldCenter + new Vector3(
                (ax - 0.5f) * worldSize.x,
                -(ay - 0.5f) * worldSize.y,
                depthOffset);

            if (handRoot != null)
            {
                if (!_hasPosition)
                {
                    handRoot.position = targetPos;
                    _hasPosition = true;
                }
                else
                {
                    float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionSmoothSpeed) * Time.deltaTime);
                    handRoot.position = Vector3.Lerp(handRoot.position, targetPos, t);
                }
            }

            // 2. Esqueleto RELATIVO à palma (antes era absoluto 0..1, o que
            // prendia o formato + posição juntos e impedia a navegação).
            Vector3[] localPositions = new Vector3[21];
            for (int i = 0; i < 21; i++)
            {
                JArray p = (JArray)lms[i];
                float px = p[0].Value<float>();
                float py = p[1].Value<float>();
                float pz = p[2].Value<float>();

                localPositions[i] = new Vector3(
                    (px - ax) * scale,
                    -((py - ay)) * scale,
                    (invertZ ? -(pz - az) : (pz - az)) * scale);
            }

            Quaternion rootRotation = handRoot != null ? handRoot.rotation : Quaternion.identity;
            bool hasOffset = handRootEulerOffset != Vector3.zero;

            if (handRoot != null && (rotateHandRoot || hasOffset))
            {
                Quaternion targetRotation = Quaternion.Euler(handRootEulerOffset);

                if (rotateHandRoot)
                {
                    float yawRad = (invertYaw ? -yaw : yaw) * Mathf.Deg2Rad;
                    float pitchRad = (invertPitch ? -pitch : pitch) * Mathf.Deg2Rad;

                    Vector3 direction = new Vector3(
                        Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
                        Mathf.Sin(pitchRad),
                        (invertZ ? 1f : -1f) * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad));

                    Vector3 up = Mathf.Abs(direction.y) > 0.99f ? Vector3.forward : Vector3.up;
                    targetRotation = Quaternion.LookRotation(direction, up) * targetRotation;
                }

                rootRotation = targetRotation;
                handRoot.rotation = targetRotation;
            }

            Quaternion inverseRoot = Quaternion.Inverse(rootRotation);

            for (int i = 0; i < 21; i++)
            {
                if (handPoints == null || i >= handPoints.Length || handPoints[i] == null)
                    continue;

                handPoints[i].transform.localPosition = preserveSkeleton
                    ? inverseRoot * localPositions[i]
                    : localPositions[i];
            }

            Transform palmT = handPoints != null
                && palmIndex >= 0 && palmIndex < handPoints.Length
                ? handPoints[palmIndex]?.transform : null;

            // Se não há HandRoot (cena sem referência), a palma visual não anda —
            // mas a posição lógica precisa andar para o GameManager autorizar a pega.
            Vector3 palmWorld = palmT != null ? palmT.position : Vector3.zero;
            if (handRoot == null)
                palmWorld = targetPos;

            CurrentFrame = new HandFrameData
            {
                IsValid = true,
                Timestamp = (long)(Time.time * 1000f),
                MaoFechada = maoFech,
                Yaw = yaw,
                Pitch = pitch,
                PalmWorldPos = palmWorld,
                PalmWorldEuler = palmT != null ? palmT.eulerAngles : Vector3.zero,
            };

            if (logEveryFrame)
                Debug.Log($"[HandTracking] ts={ts} maoRaw={maoFechRaw} mao={maoFech} palm={CurrentFrame.PalmWorldPos} target={targetPos} size={handSize:F3}");
        }
    }
}