using System;
using FlowForge.Data;
using UnityEngine;

namespace FlowForge.Simulation
{
    /// <summary>
    /// Runtime machine/poste that processes one order at a time and emits visual state.
    /// </summary>
    public class Workstation : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Transform progressBar;

        public MachineConfig Config { get; private set; }
        public ProductionOrder CurrentOrder { get; private set; }
        public bool IsBusy => CurrentOrder != null;
        public bool IsBroken { get; private set; }
        public float BusyTime { get; private set; }
        public float BrokenTime { get; private set; }
        public float EffectiveProcessTime => Mathf.Max(0.25f, Config.processTimeSeconds * processTimeMultiplier);
        public float EffectiveTravelDistance => Config.operatorTravelDistance * travelDistanceMultiplier;

        private float elapsed;
        private float repairElapsed;
        private float processTimeMultiplier = 1f;
        private float defectMultiplier = 1f;
        private float travelDistanceMultiplier = 1f;

        public event Action<Workstation, ProductionOrder, bool> OrderProcessed;
        public event Action<Workstation> BrokeDown;
        public event Action<Workstation> Repaired;

        public void Initialize(MachineConfig config, Color fallbackColor)
        {
            Config = config;
            EnsureVisuals(fallbackColor);
            name = config != null ? config.displayName : "Workstation";
        }

        public bool TryStart(ProductionOrder order)
        {
            if (Config == null || IsBusy || IsBroken)
            {
                return false;
            }

            CurrentOrder = order;
            elapsed = 0f;
            UpdateProgress(0f);
            return true;
        }

        public void ApplyLeanMultiplier(float processReduction, float defectReduction, float travelReduction)
        {
            processTimeMultiplier *= 1f - Mathf.Clamp01(processReduction);
            defectMultiplier *= 1f - Mathf.Clamp01(defectReduction);
            travelDistanceMultiplier *= 1f - Mathf.Clamp01(travelReduction);
            transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        }

        private void Update()
        {
            if (Config == null)
            {
                return;
            }

            if (IsBroken)
            {
                repairElapsed += Time.deltaTime;
                BrokenTime += Time.deltaTime;
                Pulse(Color.red);
                if (repairElapsed >= Config.repairTimeSeconds)
                {
                    IsBroken = false;
                    repairElapsed = 0f;
                    Repaired?.Invoke(this);
                }
                return;
            }

            if (!IsBusy)
            {
                UpdateProgress(0f);
                return;
            }

            elapsed += Time.deltaTime;
            BusyTime += Time.deltaTime;
            UpdateProgress(elapsed / EffectiveProcessTime);

            if (elapsed < EffectiveProcessTime)
            {
                return;
            }

            var order = CurrentOrder;
            CurrentOrder = null;
            elapsed = 0f;

            var defective = UnityEngine.Random.value < Config.defectChance * defectMultiplier;
            if (defective)
            {
                order.QualityState = ItemQualityState.Defective;
            }

            if (UnityEngine.Random.value < Config.breakdownChancePerCycle)
            {
                IsBroken = true;
                BrokeDown?.Invoke(this);
            }

            OrderProcessed?.Invoke(this, order, defective);
        }

        private void EnsureVisuals(Color fallbackColor)
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.material = new Material(Shader.Find("Standard"));
                bodyRenderer.material.color = Config != null ? Config.color : fallbackColor;
            }

            if (progressBar == null)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Progress Bar";
                bar.transform.SetParent(transform, false);
                bar.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                bar.transform.localScale = new Vector3(0.1f, 0.08f, 0.95f);
                var renderer = bar.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.green;
                progressBar = bar.transform;
            }
        }

        private void UpdateProgress(float normalized)
        {
            if (progressBar == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(normalized);
            progressBar.localScale = new Vector3(Mathf.Lerp(0.1f, 1f, clamped), 0.08f, 0.95f);
        }

        private void Pulse(Color color)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            bodyRenderer.material.color = Color.Lerp(Config.color, color, Mathf.PingPong(Time.time * 3f, 1f));
        }
    }
}
