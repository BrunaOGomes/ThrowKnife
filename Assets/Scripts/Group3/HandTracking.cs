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
        public float scale = 10f;

        [Header("Referência da palma")]
        public int palmIndex = 0;

        [Header("Debug")]
        public bool logEveryFrame = false;

        public HandFrameData CurrentFrame { get; private set; }

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

            for (int i = 0; i < 21; i++)
            {
                if (handPoints == null || i >= handPoints.Length || handPoints[i] == null)
                    continue;
                JArray p = (JArray)lms[i];
                float px = p[0].Value<float>();
                float py = p[1].Value<float>();
                float pz = p[2].Value<float>();

                handPoints[i].transform.localPosition = new Vector3(
                    -(px - 0.5f) * scale,
                     (py - 0.5f) * scale,
                      pz * scale);
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