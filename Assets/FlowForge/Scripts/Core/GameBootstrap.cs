using FlowForge.Lean;
using FlowForge.UI;
using UnityEngine;

namespace FlowForge.Core
{
    /// <summary>
    /// Drop this component into an empty Unity scene and press Play to run the prototype.
    /// It creates camera, light, simulation, grid, machines, operators and HUD at runtime.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private SimulationController simulationController;
        [SerializeField] private LeanDashboardUI dashboardUI;

        private void Awake()
        {
            EnsureCamera();
            EnsureLighting();
            EnsureSimulation();
            simulationController.StartPrototype();
            dashboardUI.Initialize(simulationController);
        }

        private void EnsureSimulation()
        {
            if (simulationController == null)
            {
                simulationController = new GameObject("Simulation Controller").AddComponent<SimulationController>();
            }

            if (simulationController.GetComponent<LeanImprovementSystem>() == null)
            {
                simulationController.gameObject.AddComponent<LeanImprovementSystem>();
            }

            if (dashboardUI == null)
            {
                dashboardUI = new GameObject("Lean Dashboard UI").AddComponent<LeanDashboardUI>();
            }
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(5.5f, 8f, -7.5f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.backgroundColor = new Color(0.68f, 0.78f, 0.9f);
        }

        private static void EnsureLighting()
        {
            if (FindObjectOfType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
