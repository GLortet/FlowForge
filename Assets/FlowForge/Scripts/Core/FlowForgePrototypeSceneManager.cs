using UnityEngine;
using UnityEngine.UI;

namespace FlowForge.Core
{
    /// <summary>
    /// Simple controller for the manually authored FlowForge prototype scene.
    /// Attach it to the GameManager, then wire the three machine Transforms and Legacy UI fields in the Inspector.
    /// </summary>
    public class FlowForgePrototypeSceneManager : MonoBehaviour
    {
        private const int InitialLeanScore = 40;
        private const int InitialScrapPercent = 18;
        private const int InitialTrsPercent = 62;
        private const int InitialStock = 120;

        [Header("Machines to recolor after 5S")]
        [Tooltip("Drag Machine_01_Decoupe, Machine_02_Assemblage and Machine_03_Controle here.")]
        public Transform[] machines = new Transform[3];

        [Header("Legacy UI - drag the scene Text/Button objects here")]
        public Text scoreLeanText;
        public Text rebutsText;
        public Text trsText;
        public Text stockText;
        public Button apply5SButton;

        [Header("Optional Feedback")]
        public Color improvedMachineColor = new Color(0.25f, 0.9f, 0.45f);
        public bool autoFindSceneReferences = true;

        private int leanScore;
        private int scrapPercent;
        private int trsPercent;
        private int stock;
        private bool fiveSApplied;

        private void Awake()
        {
            if (autoFindSceneReferences)
            {
                AutoFindMissingReferences();
            }

            ResetMetrics();
            WireButton();
            RefreshUi();
        }

        private void OnDestroy()
        {
            if (apply5SButton != null)
            {
                apply5SButton.onClick.RemoveListener(Apply5S);
            }
        }

        /// <summary>
        /// Called by Apply5SButton. Applies the first Lean improvement and updates the HUD immediately.
        /// </summary>
        public void Apply5S()
        {
            if (fiveSApplied)
            {
                return;
            }

            fiveSApplied = true;
            leanScore = Mathf.Clamp(leanScore + 10, 0, 100);
            scrapPercent = Mathf.Max(0, scrapPercent - 4);
            trsPercent = Mathf.Clamp(trsPercent + 6, 0, 100);
            stock = Mathf.Max(0, stock - 15);

            RecolorMachines();
            RefreshUi();
        }

        /// <summary>
        /// Useful from the component context menu while testing in the Editor.
        /// </summary>
        [ContextMenu("Reset Prototype Metrics")]
        public void ResetMetrics()
        {
            leanScore = InitialLeanScore;
            scrapPercent = InitialScrapPercent;
            trsPercent = InitialTrsPercent;
            stock = InitialStock;
            fiveSApplied = false;

            if (apply5SButton != null)
            {
                apply5SButton.interactable = true;
            }
        }

        private void WireButton()
        {
            if (apply5SButton == null)
            {
                Debug.LogWarning("FlowForgePrototypeSceneManager: Apply5SButton is not assigned.", this);
                return;
            }

            apply5SButton.onClick.RemoveListener(Apply5S);
            apply5SButton.onClick.AddListener(Apply5S);
        }

        private void RefreshUi()
        {
            SetText(scoreLeanText, $"Score Lean : {leanScore}");
            SetText(rebutsText, $"Rebuts : {scrapPercent} %");
            SetText(trsText, $"TRS : {trsPercent} %");
            SetText(stockText, $"Stock : {stock}");
        }

        private void RecolorMachines()
        {
            foreach (var machine in machines)
            {
                if (machine == null)
                {
                    continue;
                }

                var renderer = machine.GetComponent<Renderer>();
                if (renderer == null)
                {
                    renderer = machine.GetComponentInChildren<Renderer>();
                }

                if (renderer == null)
                {
                    Debug.LogWarning($"FlowForgePrototypeSceneManager: {machine.name} has no Renderer to recolor.", machine);
                    continue;
                }

                renderer.material.color = improvedMachineColor;
            }
        }

        private void AutoFindMissingReferences()
        {
            if (scoreLeanText == null)
            {
                scoreLeanText = FindText("ScoreLeanText");
            }

            if (rebutsText == null)
            {
                rebutsText = FindText("RebutsText");
            }

            if (trsText == null)
            {
                trsText = FindText("TRSText");
            }

            if (stockText == null)
            {
                stockText = FindText("StockText");
            }

            if (apply5SButton == null)
            {
                var buttonObject = GameObject.Find("Apply5SButton");
                if (buttonObject != null)
                {
                    apply5SButton = buttonObject.GetComponent<Button>();
                }
            }

            if (machines == null || machines.Length == 0)
            {
                machines = new Transform[3];
            }

            AssignMachineIfMissing(0, "Machine_01_Decoupe");
            AssignMachineIfMissing(1, "Machine_02_Assemblage");
            AssignMachineIfMissing(2, "Machine_03_Controle");
        }

        private void AssignMachineIfMissing(int index, string objectName)
        {
            if (machines.Length <= index || machines[index] != null)
            {
                return;
            }

            var machineObject = GameObject.Find(objectName);
            if (machineObject != null)
            {
                machines[index] = machineObject.transform;
            }
        }

        private static Text FindText(string objectName)
        {
            var textObject = GameObject.Find(objectName);
            return textObject != null ? textObject.GetComponent<Text>() : null;
        }

        private void SetText(Text target, string value)
        {
            if (target == null)
            {
                Debug.LogWarning($"FlowForgePrototypeSceneManager: missing UI Text for '{value}'.", this);
                return;
            }

            target.text = value;
        }
    }
}
