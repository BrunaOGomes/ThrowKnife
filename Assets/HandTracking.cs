using UnityEngine;
using Newtonsoft.Json.Linq;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] handPoints;   // 21 GameObjects no Inspector, na ordem do MediaPipe

    [Header("Escala / posicionamento")]
    [Tooltip("Quanto maior, mais 'espalhada' a mão fica no mundo.")]
    public float scale = 10f;
    [Tooltip("Desloca o centro da mão (0.5,0.5) para a origem do objeto pai.")]
    public bool centerAtWrist = false;

    [Header("Debug")]
    public bool logEveryFrame = false;

    // Último frame válido (para LineCode / outros scripts consultarem)
    public bool MaoFechada { get; private set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public long Timestamp { get; private set; }

    void Update()
    {
        string data = udpReceive != null ? udpReceive.data : null;

        // 1) Sem dados ainda -> não faz nada (evita NullReference/FormatException)
        if (string.IsNullOrEmpty(data))
            return;

        // 2) Parse do JSON
        JObject frame;
        try
        {
            frame = JObject.Parse(data);
        }
        catch (System.Exception e)
        {
            // JSON incompleto (UDP pode chegar truncado se o datagrama exceder o buffer)
            Debug.LogWarning($"[HandTracking] JSON inválido: {e.Message}");
            return;
        }

        // 3) Metadados
        MaoFechada = frame.Value<bool?>("mao_fechada") ?? false;
        Yaw = frame.Value<float?>("yaw") ?? 0f;
        Pitch = frame.Value<float?>("pitch") ?? 0f;
        Timestamp = frame.Value<long?>("timestamp") ?? 0L;

        // 4) Landmarks
        if (!(frame["landmarks"] is JArray landmarks) || landmarks.Count < 21)
            return;

        // Opcional: centralizar no pulso (landmark 0) em vez de usar (0.5, 0.5)
        Vector3 wristOffset = Vector3.zero;
        if (centerAtWrist)
        {
            var w = landmarks[0];
            wristOffset = new Vector3(
                w[0].Value<float>() - 0.5f,
                w[1].Value<float>() - 0.5f,
                w[2].Value<float>()
            );
        }

        for (int i = 0; i < 21; i++)
        {
            if (handPoints == null || i >= handPoints.Length || handPoints[i] == null)
                continue;

            JArray p = (JArray)landmarks[i];

            // Landmarks do MediaPipe: x,y em [0..1] normalizados na imagem; z relativo.
            // Invertemos X (espelho) e centralizamos em 0.5.
            float px = p[0].Value<float>() - 0.5f - wristOffset.x;
            float py = p[1].Value<float>() - 0.5f - wristOffset.y;
            float pz = p[2].Value<float>() - wristOffset.z;

            Vector3 local = new Vector3(-px * scale, py * scale, pz * scale);
            handPoints[i].transform.localPosition = local;
        }

        if (logEveryFrame)
            Debug.Log($"[HandTracking] ts={Timestamp} mao_fechada={MaoFechada} " +
                      $"yaw={Yaw:F2} pitch={Pitch:F2}");
    }
}