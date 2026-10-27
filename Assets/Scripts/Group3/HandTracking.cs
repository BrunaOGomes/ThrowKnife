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

        [Header("Debug")]
        public bool logEveryFrame = false;

        public HandFrameData CurrentFrame { get; private set; }

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
            if (string.IsNullOrEmpty(data)) { CurrentFrame = default; return; }

            JObject frame;
            try { frame = JObject.Parse(data); }
            catch { return; }

            long ts = frame.Value<long?>("timestamp") ?? 0L;
            bool maoFech = frame.Value<bool?>("mao_fechada") ?? false;
            float yaw = frame.Value<float?>("yaw") ?? 0f;
            float pitch = frame.Value<float?>("pitch") ?? 0f;

            if (!(frame["landmarks"] is JArray lms) || lms.Count < 21) return;

            Vector3[] localPositions = new Vector3[21];
            for (int i = 0; i < 21; i++)
            {
                JArray p = (JArray)lms[i];
                float px = p[0].Value<float>();
                float py = p[1].Value<float>();
                float pz = p[2].Value<float>();

                localPositions[i] = new Vector3(
                     (px - 0.5f) * scale,
                    -(py - 0.5f) * scale,
                    (invertZ ? -pz : pz) * scale);
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

            CurrentFrame = new HandFrameData
            {
                IsValid = true,
                Timestamp = (long)(Time.time * 1000f),
                MaoFechada = maoFech,
                Yaw = yaw,
                Pitch = pitch,
                PalmWorldPos = palmT != null ? palmT.position : Vector3.zero,
                PalmWorldEuler = palmT != null ? palmT.eulerAngles : Vector3.zero,
            };

            if (logEveryFrame)
                Debug.Log($"[HandTracking] ts={ts} mao={maoFech} palm={CurrentFrame.PalmWorldPos}");
        }
    }
}