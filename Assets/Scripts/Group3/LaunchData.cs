using System;
using Newtonsoft.Json;

namespace Group3
{
    [Serializable]
    public class LaunchData
    {
        [JsonProperty("tipo")] public string Tipo = "lancamento";
        [JsonProperty("posicao")] public float[] Posicao;
        [JsonProperty("yaw")] public float Yaw;
        [JsonProperty("pitch")] public float Pitch;
        [JsonProperty("velocidade")] public float[] Velocidade;
        [JsonProperty("timestamp")] public long Timestamp;

        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}