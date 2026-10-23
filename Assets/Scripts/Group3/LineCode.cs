using UnityEngine;

namespace Group3
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineCode : MonoBehaviour
    {
        LineRenderer lineRenderer;
        public Transform origin;
        public Transform destination;

        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }

        void Update()
        {
            if (origin == null || destination == null) return;

            lineRenderer.SetPosition(0, origin.position);
            lineRenderer.SetPosition(1, destination.position);
        }
    }
}