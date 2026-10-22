using System.Collections.Generic;
using UnityEngine;

namespace Group3
{
    public class HandVelocityCalculator
    {
        public float Smoothing = 0.7f;
        public float MaxDt = 0.2f;
        public int MinSamples = 2;
        public float ZWeight = 0.5f;
        public float MinSpeedThreshold = 0.05f;

        private struct Sample { public Vector3 P; public float T; }
        private readonly List<Sample> _s = new List<Sample>(8);
        private Vector3 _emaVel = Vector3.zero;

        public Vector3 Velocity { get; private set; }
        public float Speed => Velocity.magnitude;
        public Vector3 Direction => Velocity.sqrMagnitude > 1e-8f
            ? Velocity.normalized : Vector3.zero;

        public void Update(HandFrameData f)
        {
            if (!f.IsValid) { Reset(); return; }

            float t = f.Timestamp * 0.001f;

            // Se a posição for exatamente igual à última, ignore este frame (é um "eco" da rede)
            if (_s.Count > 0 && _s[_s.Count - 1].P == f.PalmWorldPos)
                return;

            _s.Add(new Sample { P = f.PalmWorldPos, T = t });
            if (_s.Count > 8) _s.RemoveAt(0);
            if (_s.Count < MinSamples) return;

            var a = _s[_s.Count - 2];
            var b = _s[_s.Count - 1];
            float dt = b.T - a.T;

            // ⬇️ DIAGNÓSTICO
            //Debug.Log($"[VEL] dt={dt:F3} | posA={a.P} | posB={b.P}");

            if (dt <= 1e-4f || dt > MaxDt) { Velocity = Vector3.zero; return; }

            Vector3 raw = (b.P - a.P) / dt;
            raw.z *= ZWeight;

            // ⬇️ DIAGNÓSTICO
            //Debug.Log($"[VEL] raw=({raw.x:F2}, {raw.y:F2}, {raw.z:F2})");

            float alpha = 1f - Mathf.Clamp01(Smoothing);
            _emaVel = Vector3.Lerp(_emaVel, raw, alpha);
            Velocity = _emaVel.magnitude < MinSpeedThreshold ? Vector3.zero : _emaVel;
        }

        public void Reset() { _s.Clear(); _emaVel = Vector3.zero; Velocity = Vector3.zero; }
    }
}