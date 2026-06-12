using FlowForge.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FlowForge.UI
{
    /// <summary>
    /// Minimal HUD designed for readability on PC and future mobile screens.
    /// </summary>
    public class LeanDashboardUI : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private Text metricsText;
        [SerializeField] private Button apply5SButton;
        [SerializeField] private Text feedbackText;

        public void Initialize(SimulationController controller)
        {
            simulation = controller;
            BuildUiIfNeeded();
            apply5SButton.onClick.RemoveAllListeners();
            apply5SButton.onClick.AddListener(Apply5S);
        }

        private void Update()
        {
            if (simulation == null || simulation.Metrics == null || metricsText == null)
            {
                return;
            }

            var metrics = simulation.Metrics;
            var score = simulation.Score;
            metricsText.text =
                $"FLOWFORGE - Prototype atelier montres\n" +
                $"Score Lean : {metrics.LeanScore:0}/100   Étoiles : {score.Stars}/3\n" +
                $"Rebuts : {metrics.ScrapRate:P1}   Lead time : {metrics.AverageLeadTime:0.0}s   TRS/OEE : {metrics.Oee:P0}\n" +
                $"Service : {metrics.ServiceRate:P0}   Stock/WIP : {metrics.GetMetric(FlowForge.Data.MetricType.Inventory):0}   Attente : {metrics.WaitingTime:0.0}\n" +
                $"Distance opérateurs : {metrics.TravelDistance:0.0}m   Retouches : {metrics.ReworkCount}   CNQ : {metrics.NonQualityCost:0}€\n" +
                $"Satisfaction : {metrics.CustomerSatisfaction:P0}   Stress équipe : {metrics.TeamStress:P0}   Argent : {metrics.Money:0}€";
        }

        private void Apply5S()
        {
            simulation.ImprovementSystem.Apply5S();
            apply5SButton.interactable = false;
            feedbackText.text = "5S appliqué : postes rangés, trajets réduits, standards stabilisés.";
        }

        private void BuildUiIfNeeded()
        {
            if (metricsText != null && apply5SButton != null && feedbackText != null)
            {
                return;
            }

            var canvas = new GameObject("Lean Dashboard Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var panel = CreatePanel(canvas.transform);
            metricsText = CreateText(panel.transform, "Metrics", new Vector2(16f, -12f), new Vector2(760f, 170f), 18, TextAnchor.UpperLeft);
            feedbackText = CreateText(panel.transform, "Feedback", new Vector2(16f, -196f), new Vector2(760f, 38f), 17, TextAnchor.MiddleLeft);
            feedbackText.text = "Observe le flux puis applique 5S pour créer un avant/après visible.";
            apply5SButton = CreateButton(panel.transform, "Appliquer 5S", new Vector2(16f, -250f));
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            var panel = new GameObject("HUD Panel").AddComponent<Image>();
            panel.transform.SetParent(parent, false);
            panel.color = new Color(0.04f, 0.06f, 0.09f, 0.82f);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(800f, 320f);
            return rect;
        }

        private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor)
        {
            var text = new GameObject(name).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position)
        {
            var buttonImage = new GameObject("5S Button").AddComponent<Image>();
            buttonImage.transform.SetParent(parent, false);
            buttonImage.color = new Color(1f, 0.72f, 0.22f);
            var rect = buttonImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 46f);

            var button = buttonImage.gameObject.AddComponent<Button>();
            var text = CreateText(buttonImage.transform, "Label", Vector2.zero, rect.sizeDelta, 18, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.08f, 0.06f, 0.02f);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
