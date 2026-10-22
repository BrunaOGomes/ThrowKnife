using UnityEngine;

namespace Group3
{
    /// <summary>Estado da mão em um frame (já parseado do JSON do Grupo 1).</summary>
    public struct HandFrameData
    {
        public bool IsValid;
        public long Timestamp;      // timestamp ORIGINAL do Grupo 1 (ms)
        public bool MaoFechada;
        public float Yaw;
        public float Pitch;
        public Vector3 PalmWorldPos;   // posição da palma (GameObject) em MUNDO
        public Vector3 PalmWorldEuler; // rotação da palma em MUNDO
    }
}
