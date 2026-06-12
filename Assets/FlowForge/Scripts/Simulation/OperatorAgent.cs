using UnityEngine;

namespace FlowForge.Simulation
{
    /// <summary>
    /// Simple operator avatar that walks between workstations to make flow and wasted motion visible.
    /// </summary>
    public class OperatorAgent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private Renderer bodyRenderer;

        private Vector3 target;
        private float stress = 0.25f;

        public float DistanceWalked { get; private set; }
        public float Stress => stress;

        public void Initialize(Color color)
        {
            target = transform.position;
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.material = new Material(Shader.Find("Standard"));
                bodyRenderer.material.color = color;
            }
        }

        public void MoveTo(Vector3 worldPosition, float extraStress)
        {
            target = worldPosition;
            stress = Mathf.Clamp01(stress + extraStress);
        }

        public void ReduceStress(float percent)
        {
            stress = Mathf.Clamp01(stress * (1f - Mathf.Clamp01(percent)));
        }

        private void Update()
        {
            var previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            DistanceWalked += Vector3.Distance(previous, transform.position);

            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = Color.Lerp(Color.cyan, new Color(1f, 0.45f, 0.25f), stress);
            }
        }
    }
}
