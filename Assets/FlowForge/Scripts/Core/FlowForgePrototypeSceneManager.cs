using System;
using UnityEngine;
using UnityEngine.UI;

namespace FlowForge.Core
{
    public enum LeanProblemType
    {
        Bottleneck,
        WrongImprovementTarget,
        QualityDefects
    }

    public enum LeanActionType
    {
        None,
        BuyParallelMachine,
        ImproveMachine,
        RebalanceLine,
        Apply5S,
        ApplyPokaYoke,
        ApplyTPM,
        ApplyStandardWork,
        TrainOperator
    }

    /// <summary>
    /// Main controller for the manually authored FlowForge prototype scene.
    /// Attach this script to GameManager and wire Legacy UI Text/Button fields plus the three machine Transforms.
    /// </summary>
    public class FlowForgePrototypeSceneManager : MonoBehaviour
    {
        [Serializable]
        public class MachineOperation
        {
            public Transform machine;
            public string operationName = "Operation";
            [Min(0.1f)] public float cycleTime = 10f;
            [Range(0f, 1f)] public float availability = 0.85f;
            [Range(0f, 1f)] public float defectRate = 0.06f;
            [Min(1)] public int parallelMachineCount = 1;
            [Min(0f)] public float fixedCost = 350f;
            [Min(0f)] public float maintenanceComplexity = 1f;
            [Min(0)] public int improvementLevel;
        }

        [Serializable]
        public class PuzzleRound
        {
            public string title;
            public LeanProblemType problemType;
            [TextArea] public string briefing;
            [TextArea] public string leanAdvice;
            [TextArea] public string successFeedback;
            [TextArea] public string failureFeedback;
            public LeanActionType recommendedAction;
            public LeanActionType alternateRecommendedAction;
            public int bottleneckMachineIndex;
            public int trapMachineIndex = -1;
            public int customerDemand = 100;
            public float availableTime = 1000f;
            [Range(0f, 1f)] public float targetServiceRate = 0.95f;
            [Range(0f, 1f)] public float targetMaxDefectRate = 0.08f;
            [Range(0f, 1f)] public float targetMinTRS = 0.75f;
            public int targetMaxStock = 60;
            public float[] cycleTimes = Array.Empty<float>();
            public float[] availabilities = Array.Empty<float>();
            public float[] defectRates = Array.Empty<float>();
        }

        [Header("Manual scene machines")]
        [Tooltip("Drag Machine_01_Decoupe, Machine_02_Assemblage and Machine_03_Controle here.")]
        public Transform[] machines = new Transform[3];

        [Tooltip("Optional detailed operation data. If empty, defaults are created from the machine array.")]
        public MachineOperation[] operations = new MachineOperation[3];

        [Header("Lean Flow Puzzle")]
        public PuzzleRound[] puzzleRounds = Array.Empty<PuzzleRound>();
        [Min(0)] public int currentPuzzleRoundIndex;
        public LeanProblemType currentProblemType;
        public LeanActionType lastAction = LeanActionType.None;
        public int lastActionTargetMachineIndex = -1;
        public bool lastActionWasGoodChoice;

        [Header("Customer mission")]
        [Min(1)] public int customerDemand = 100;
        [Min(1f)] public float availableTime = 1000f;
        [Range(0f, 1f)] public float targetServiceRate = 0.95f;
        [Range(0f, 1f)] public float targetMaxDefectRate = 0.08f;
        [Range(0f, 1f)] public float targetMinTRS = 0.75f;
        [Min(0)] public int targetMaxStock = 60;

        [Header("Economy")]
        public float budget = 10000f;
        public float salePricePerWatch = 200f;
        public float variableCostPerWatch = 120f;
        public float marginPerGoodWatch = 80f;
        public float scrapCostPerWatch = 80f;

        [Header("Round results")]
        public float taktTime;
        public float revenue;
        public float productionCost;
        public float scrapCost;
        public float leanBonus;
        public float customerBonus;
        public float reputation = 50f;
        public float totalRoundProfit;
        public int scoreBusiness;
        public int scoreLean = 40;
        public float maintenanceComplexity;
        public int stock = 120;
        public int possibleProduction;
        public int goodWatches;
        public int scrapWatches;
        public float serviceRate;
        public float roundDefectRate = 0.18f;
        public float trs = 0.62f;
        public string currentBottleneck = "-";

        [Header("Legacy UI - existing scene fields")]
        public Text scoreLeanText;
        public Text rebutsText;
        public Text trsText;
        public Text stockText;
        public Button apply5SButton;

        [Header("Optional extended Legacy UI fields")]
        public Text budgetText;
        public Text roundMoneyText;
        public Text leanBonusText;
        public Text reputationText;
        public Text demandText;
        public Text taktTimeText;
        public Text bottleneckText;
        public Text possibleProductionText;
        public Text goodWatchesText;
        public Text serviceRateText;
        public Text scoreBusinessText;
        public Text maintenanceComplexityText;
        public Text feedbackText;

        [Header("Optional decision buttons")]
        public Button startRoundButton;
        public Button endRoundButton;
        public Button buyParallelMachineButton;
        public Button improveMachineButton;
        public Button rebalanceLineButton;
        public Button pokaYokeButton;
        public Button tpmButton;
        public Button standardWorkButton;
        public Button trainOperatorButton;
        public Button nextPuzzleRoundButton;
        public Button restartPuzzleRoundButton;

        [Header("Feedback colors")]
        public Color bottleneckColor = new Color(1f, 0.35f, 0.05f);
        public Color balancedColor = new Color(0.25f, 0.9f, 0.45f);
        public Color neutralMachineColor = new Color(0.65f, 0.68f, 0.72f);
        public bool autoFindSceneReferences = true;

        private int bottleneckIndex;
        private float previousBottleneckCapacity;
        private bool hasRoundResults;
        private bool roundClosed;
        private bool fiveSAppliedThisRound;

        private void Awake()
        {
            if (autoFindSceneReferences)
            {
                AutoFindMissingReferences();
            }

            EnsureOperations();
            EnsurePuzzleRounds();
            ResetRunState();
            WireButtons();
            LoadCurrentPuzzleRound();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void LoadCurrentPuzzleRound()
        {
            EnsureOperations();
            EnsurePuzzleRounds();

            if (puzzleRounds.Length == 0)
            {
                CalculateLinePreview(false);
                RefreshUi("Aucun puzzle round configuré.");
                return;
            }

            currentPuzzleRoundIndex = Mathf.Clamp(currentPuzzleRoundIndex, 0, puzzleRounds.Length - 1);
            var round = puzzleRounds[currentPuzzleRoundIndex];
            currentProblemType = round.problemType;
            customerDemand = round.customerDemand;
            availableTime = round.availableTime;
            targetServiceRate = round.targetServiceRate;
            targetMaxDefectRate = round.targetMaxDefectRate;
            targetMinTRS = round.targetMinTRS;
            targetMaxStock = round.targetMaxStock;
            ApplyPuzzleRoundMachineSetup(round);
            ResetRoundOnlyState();
            CalculateLinePreview(false);
            RefreshUi(BuildBriefing(round));
        }

        public void NextPuzzleRound()
        {
            EnsurePuzzleRounds();
            if (puzzleRounds.Length == 0)
            {
                RefreshUi("Aucun puzzle round disponible.");
                return;
            }

            currentPuzzleRoundIndex = (currentPuzzleRoundIndex + 1) % puzzleRounds.Length;
            LoadCurrentPuzzleRound();
        }

        public void RestartPuzzleRound()
        {
            LoadCurrentPuzzleRound();
        }

        public void RegisterPlayerAction(LeanActionType action, int targetMachineIndex = -1)
        {
            lastAction = action;
            lastActionTargetMachineIndex = targetMachineIndex;
            lastActionWasGoodChoice = false;
        }

        public bool EvaluateActionAgainstCurrentPuzzle()
        {
            EnsurePuzzleRounds();
            if (puzzleRounds.Length == 0)
            {
                return false;
            }

            var round = puzzleRounds[currentPuzzleRoundIndex];
            var correctAction = lastAction == round.recommendedAction ||
                                (round.alternateRecommendedAction != LeanActionType.None && lastAction == round.alternateRecommendedAction);
            var correctTarget = round.bottleneckMachineIndex < 0 || lastActionTargetMachineIndex < 0 || lastActionTargetMachineIndex == round.bottleneckMachineIndex;
            var trapTarget = round.trapMachineIndex >= 0 && lastActionTargetMachineIndex == round.trapMachineIndex;

            lastActionWasGoodChoice = correctAction && correctTarget && !trapTarget;

            if (lastActionWasGoodChoice)
            {
                scoreLean = Mathf.Clamp(scoreLean + 4, 0, 100);
                RefreshUi($"Bon choix Lean. {round.successFeedback}\nConseil : {round.leanAdvice}");
            }
            else
            {
                RefreshUi($"Choix discutable. {round.failureFeedback}\nConseil : {round.leanAdvice}");
            }

            return lastActionWasGoodChoice;
        }

        public void StartRound()
        {
            EnsureOperations();
            CalculateLinePreview();
            previousBottleneckCapacity = GetBottleneckCapacity();

            possibleProduction = Mathf.Max(0, Mathf.FloorToInt(previousBottleneckCapacity));
            var producedThisRound = Mathf.Min(possibleProduction, Mathf.CeilToInt(customerDemand * 1.15f));
            roundDefectRate = CalculateLineDefectRate();
            scrapWatches = Mathf.RoundToInt(producedThisRound * roundDefectRate);
            goodWatches = Mathf.Max(0, producedThisRound - scrapWatches);
            var delivered = Mathf.Min(goodWatches, customerDemand);
            serviceRate = customerDemand <= 0 ? 1f : (float)delivered / customerDemand;

            stock = Mathf.Max(0, producedThisRound - delivered) + CalculateImbalanceStock();
            trs = CalculateTRS();
            revenue = delivered * salePricePerWatch;
            productionCost = producedThisRound * variableCostPerWatch;
            scrapCost = scrapWatches * scrapCostPerWatch;
            customerBonus = 0f;
            leanBonus = 0f;
            totalRoundProfit = revenue - productionCost - scrapCost;
            budget += totalRoundProfit;
            reputation = Mathf.Clamp(reputation - Mathf.Max(0, customerDemand - delivered) * 0.25f, 0f, 100f);
            hasRoundResults = true;
            roundClosed = false;

            EndRound();
            EvaluateActionAgainstCurrentPuzzle();
        }

        public void EndRound()
        {
            if (!hasRoundResults)
            {
                CalculateLinePreview();
                RefreshUi("Aucun round lancé : choisissez une action Lean, puis cliquez sur Lancer round.");
                return;
            }

            if (roundClosed)
            {
                RefreshUi("Round déjà clôturé : passez au puzzle suivant ou relancez ce puzzle.");
                return;
            }

            roundClosed = true;
            customerBonus = 0f;
            leanBonus = 0f;

            if (serviceRate >= targetServiceRate)
            {
                customerBonus += 500f;
                reputation = Mathf.Clamp(reputation + 4f, 0f, 100f);
            }

            if (roundDefectRate <= targetMaxDefectRate)
            {
                leanBonus += 350f;
                scoreLean += 8;
            }

            if (trs >= targetMinTRS)
            {
                leanBonus += 300f;
                scoreLean += 6;
            }

            if (stock <= targetMaxStock)
            {
                leanBonus += 250f;
                scoreLean += 6;
            }

            var newBottleneckCapacity = GetBottleneckCapacity();
            if (newBottleneckCapacity > previousBottleneckCapacity + 0.1f)
            {
                leanBonus += 200f;
                scoreLean += 5;
            }

            totalRoundProfit += customerBonus + leanBonus;
            budget += customerBonus + leanBonus;
            scoreLean = Mathf.Clamp(scoreLean, 0, 100);
            scoreBusiness = Mathf.Clamp(Mathf.RoundToInt((budget / 15000f) * 100f), 0, 100);
            fiveSAppliedThisRound = false;

            ColorMachinesByBottleneck();
            RefreshUi("Round terminé : résultats calculés, bonus Lean/client appliqués.");
        }

        public void BuyParallelMachine()
        {
            EnsureOperations();
            var target = GetBottleneckOperation();
            BuyParallelMachine(target.machine);
        }

        public void BuyParallelMachine(Transform machine)
        {
            var operation = FindOperation(machine) ?? GetBottleneckOperation();
            const float cost = 2500f;
            if (!SpendBudget(cost, "Achat machine impossible : budget insuffisant."))
            {
                return;
            }

            operation.parallelMachineCount += 1;
            operation.fixedCost += 450f;
            operation.maintenanceComplexity += 0.75f;
            scoreLean = Mathf.Clamp(scoreLean + 1, 0, 100);
            RegisterPlayerAction(LeanActionType.BuyParallelMachine, GetOperationIndex(operation));
            CalculateLinePreview();
            RefreshUi($"Machine parallèle ajoutée sur {operation.operationName}. Capacité rapide, complexité en hausse.");
        }

        public void ImproveMachine()
        {
            EnsureOperations();
            ImproveMachine(GetBottleneckOperation().machine);
        }

        public void ImproveMachine(Transform machine)
        {
            var operation = FindOperation(machine) ?? GetBottleneckOperation();
            const float cost = 900f;
            if (!SpendBudget(cost, "Amélioration impossible : budget insuffisant."))
            {
                return;
            }

            operation.improvementLevel += 1;
            operation.cycleTime = Mathf.Max(0.5f, operation.cycleTime * 0.92f);
            operation.defectRate = Mathf.Clamp01(operation.defectRate * 0.94f);
            operation.availability = Mathf.Clamp01(operation.availability + 0.02f);
            scoreLean = Mathf.Clamp(scoreLean + 5, 0, 100);
            RegisterPlayerAction(LeanActionType.ImproveMachine, GetOperationIndex(operation));
            CalculateLinePreview();
            RefreshUi($"Amélioration progressive sur {operation.operationName} : cycle, qualité et TRS progressent.");
        }

        public void ImproveMachine01Decoupe()
        {
            ImproveMachineByIndex(0);
        }

        public void ImproveMachine02Assemblage()
        {
            ImproveMachineByIndex(1);
        }

        public void ImproveMachine03Controle()
        {
            ImproveMachineByIndex(2);
        }

        public void ImproveMachineByIndex(int index)
        {
            EnsureOperations();
            if (index < 0 || index >= operations.Length)
            {
                RefreshUi("Machine cible invalide pour l'amélioration.");
                return;
            }

            ImproveMachine(operations[index].machine);
        }

        public void RebalanceLine()
        {
            EnsureOperations();
            var bottleneck = GetBottleneckOperation();
            var helper = GetHighestCapacityOperation();
            const float cost = 450f;
            if (bottleneck == helper || !SpendBudget(cost, "Rééquilibrage impossible : budget insuffisant."))
            {
                return;
            }

            var movedTime = Mathf.Min(bottleneck.cycleTime * 0.08f, 1.2f);
            bottleneck.cycleTime = Mathf.Max(0.5f, bottleneck.cycleTime - movedTime);
            helper.cycleTime += movedTime * 0.55f;
            scoreLean = Mathf.Clamp(scoreLean + 7, 0, 100);
            RegisterPlayerAction(LeanActionType.RebalanceLine, GetOperationIndex(bottleneck));
            CalculateLinePreview();
            RefreshUi($"Rééquilibrage : temps transféré depuis {bottleneck.operationName} vers {helper.operationName}.");
        }

        public void Apply5S()
        {
            const float cost = 300f;
            if (fiveSAppliedThisRound)
            {
                RefreshUi("5S déjà appliqué pour ce round.");
                return;
            }

            if (!SpendBudget(cost, "5S impossible : budget insuffisant."))
            {
                return;
            }

            fiveSAppliedThisRound = true;
            foreach (var operation in operations)
            {
                operation.cycleTime = Mathf.Max(0.5f, operation.cycleTime * 0.97f);
                operation.defectRate = Mathf.Clamp01(operation.defectRate * 0.96f);
            }

            stock = Mathf.Max(0, stock - 15);
            scoreLean = Mathf.Clamp(scoreLean + 10, 0, 100);
            RegisterPlayerAction(LeanActionType.Apply5S);
            CalculateLinePreview();
            ColorAllMachines(balancedColor);
            RefreshUi("5S appliqué : postes clarifiés, mouvements réduits, stock et rebuts en baisse.");
        }

        public void ApplyPokaYoke()
        {
            const float cost = 700f;
            if (!SpendBudget(cost, "Poka-Yoke impossible : budget insuffisant."))
            {
                return;
            }

            foreach (var operation in operations)
            {
                operation.defectRate = Mathf.Clamp01(operation.defectRate * 0.72f);
            }

            scoreLean = Mathf.Clamp(scoreLean + 9, 0, 100);
            RegisterPlayerAction(LeanActionType.ApplyPokaYoke);
            CalculateLinePreview();
            RefreshUi("Poka-Yoke appliqué : erreurs évitées à la source, rebuts réduits.");
        }

        public void ApplyTPM()
        {
            const float cost = 800f;
            if (!SpendBudget(cost, "TPM impossible : budget insuffisant."))
            {
                return;
            }

            foreach (var operation in operations)
            {
                operation.availability = Mathf.Clamp01(operation.availability + 0.06f);
                operation.maintenanceComplexity = Mathf.Max(0f, operation.maintenanceComplexity - 0.2f);
            }

            scoreLean = Mathf.Clamp(scoreLean + 8, 0, 100);
            RegisterPlayerAction(LeanActionType.ApplyTPM);
            CalculateLinePreview();
            RefreshUi("TPM appliqué : disponibilité améliorée et complexité maintenance maîtrisée.");
        }

        public void ApplyStandardWork()
        {
            const float cost = 600f;
            if (!SpendBudget(cost, "Standard Work impossible : budget insuffisant."))
            {
                return;
            }

            foreach (var operation in operations)
            {
                operation.cycleTime = Mathf.Max(0.5f, operation.cycleTime * 0.95f);
                operation.defectRate = Mathf.Clamp01(operation.defectRate * 0.9f);
            }

            scoreLean = Mathf.Clamp(scoreLean + 8, 0, 100);
            RegisterPlayerAction(LeanActionType.ApplyStandardWork);
            CalculateLinePreview();
            RefreshUi("Standard Work appliqué : cadence plus stable, variabilité réduite.");
        }

        public void TrainOperator()
        {
            const float cost = 500f;
            if (!SpendBudget(cost, "Formation impossible : budget insuffisant."))
            {
                return;
            }

            foreach (var operation in operations)
            {
                operation.defectRate = Mathf.Clamp01(operation.defectRate * 0.92f);
                operation.availability = Mathf.Clamp01(operation.availability + 0.015f);
            }

            reputation = Mathf.Clamp(reputation + 2f, 0f, 100f);
            scoreLean = Mathf.Clamp(scoreLean + 6, 0, 100);
            RegisterPlayerAction(LeanActionType.TrainOperator);
            CalculateLinePreview();
            RefreshUi("Opérateur formé : qualité, stabilité et engagement progressent.");
        }

        [ContextMenu("Reset Prototype")]
        public void ResetPrototype()
        {
            budget = 10000f;
            reputation = 50f;
            scoreLean = 40;
            scoreBusiness = 0;
            currentPuzzleRoundIndex = 0;
            EnsureOperations(true);
            ResetRoundOnlyState();
            LoadCurrentPuzzleRound();
        }

        private void ResetRunState()
        {
            budget = 10000f;
            reputation = 50f;
            scoreLean = 40;
            scoreBusiness = 0;
            ResetRoundOnlyState();
        }

        private void ResetRoundOnlyState()
        {
            stock = 120;
            revenue = 0f;
            productionCost = 0f;
            scrapCost = 0f;
            leanBonus = 0f;
            customerBonus = 0f;
            totalRoundProfit = 0f;
            possibleProduction = 0;
            goodWatches = 0;
            scrapWatches = 0;
            serviceRate = 0f;
            roundDefectRate = 0.18f;
            trs = 0.62f;
            hasRoundResults = false;
            roundClosed = false;
            fiveSAppliedThisRound = false;
            lastAction = LeanActionType.None;
            lastActionTargetMachineIndex = -1;
            lastActionWasGoodChoice = false;
        }

        private void EnsurePuzzleRounds()
        {
            if (puzzleRounds != null && puzzleRounds.Length > 0)
            {
                return;
            }

            puzzleRounds = new[]
            {
                new PuzzleRound
                {
                    title = "Round 1 — Goulot évident",
                    problemType = LeanProblemType.Bottleneck,
                    briefing = "L'assemblage limite clairement le débit. Observe la machine orange : elle pilote toute la ligne.",
                    leanAdvice = "Commence par la contrainte : améliorer le goulot ou rééquilibrer donne du débit global.",
                    successFeedback = "Tu as traité la contrainte : le débit global progresse.",
                    failureFeedback = "Tu n'as pas vraiment traité la contrainte. Le goulot continue de limiter la ligne.",
                    recommendedAction = LeanActionType.ImproveMachine,
                    alternateRecommendedAction = LeanActionType.RebalanceLine,
                    bottleneckMachineIndex = 1,
                    trapMachineIndex = 0,
                    customerDemand = 100,
                    availableTime = 1000f,
                    targetServiceRate = 0.88f,
                    targetMaxDefectRate = 0.16f,
                    targetMinTRS = 0.62f,
                    targetMaxStock = 95,
                    cycleTimes = new[] { 7.5f, 14.5f, 9.0f },
                    availabilities = new[] { 0.9f, 0.82f, 0.9f },
                    defectRates = new[] { 0.03f, 0.05f, 0.03f }
                },
                new PuzzleRound
                {
                    title = "Round 2 — Amélioration hors goulot inutile",
                    problemType = LeanProblemType.WrongImprovementTarget,
                    briefing = "La découpe est déjà rapide. L'améliorer semble tentant, mais elle n'est pas la contrainte.",
                    leanAdvice = "Une ressource non contrainte améliorée ne change presque pas le débit global : cible l'assemblage.",
                    successFeedback = "Tu as évité le piège et agi sur la vraie ressource limitante.",
                    failureFeedback = "Tu as investi hors goulot : localement c'est mieux, globalement la ligne reste limitée.",
                    recommendedAction = LeanActionType.ImproveMachine,
                    alternateRecommendedAction = LeanActionType.RebalanceLine,
                    bottleneckMachineIndex = 1,
                    trapMachineIndex = 0,
                    customerDemand = 110,
                    availableTime = 1000f,
                    targetServiceRate = 0.9f,
                    targetMaxDefectRate = 0.14f,
                    targetMinTRS = 0.66f,
                    targetMaxStock = 80,
                    cycleTimes = new[] { 5.8f, 13.8f, 8.5f },
                    availabilities = new[] { 0.92f, 0.8f, 0.9f },
                    defectRates = new[] { 0.03f, 0.05f, 0.03f }
                },
                new PuzzleRound
                {
                    title = "Round 3 — Défauts qualité",
                    problemType = LeanProblemType.QualityDefects,
                    briefing = "Le débit existe, mais trop de montres sortent avec défaut. Produire plus ne suffit plus.",
                    leanAdvice = "Quand la qualité chute, sécurise le process : Poka-Yoke ou Standard Work avant la vitesse pure.",
                    successFeedback = "Tu as traité la cause qualité : moins de rebuts, meilleure valeur client.",
                    failureFeedback = "Tu as privilégié le débit sans sécuriser la qualité : les rebuts absorbent les gains.",
                    recommendedAction = LeanActionType.ApplyPokaYoke,
                    alternateRecommendedAction = LeanActionType.ApplyStandardWork,
                    bottleneckMachineIndex = -1,
                    trapMachineIndex = 1,
                    customerDemand = 105,
                    availableTime = 1000f,
                    targetServiceRate = 0.88f,
                    targetMaxDefectRate = 0.08f,
                    targetMinTRS = 0.68f,
                    targetMaxStock = 70,
                    cycleTimes = new[] { 7.5f, 9.5f, 8.0f },
                    availabilities = new[] { 0.9f, 0.88f, 0.92f },
                    defectRates = new[] { 0.04f, 0.16f, 0.08f }
                }
            };
        }

        private void ApplyPuzzleRoundMachineSetup(PuzzleRound round)
        {
            for (var i = 0; i < operations.Length; i++)
            {
                if (operations[i] == null)
                {
                    operations[i] = new MachineOperation();
                }

                if (machines != null && machines.Length > i && operations[i].machine == null)
                {
                    operations[i].machine = machines[i];
                }

                operations[i].operationName = GetDefaultOperationName(i);
                operations[i].cycleTime = GetRoundArrayValue(round.cycleTimes, i, operations[i].cycleTime);
                operations[i].availability = Mathf.Clamp01(GetRoundArrayValue(round.availabilities, i, operations[i].availability));
                operations[i].defectRate = Mathf.Clamp01(GetRoundArrayValue(round.defectRates, i, operations[i].defectRate));
                operations[i].parallelMachineCount = 1;
                operations[i].fixedCost = i == 1 ? 420f : 350f;
                operations[i].maintenanceComplexity = i == 1 ? 1.3f : 1f;
                operations[i].improvementLevel = 0;
            }
        }

        private static float GetRoundArrayValue(float[] values, int index, float fallback)
        {
            return values != null && values.Length > index ? values[index] : fallback;
        }

        private static string GetDefaultOperationName(int index)
        {
            switch (index)
            {
                case 0: return "Découpe";
                case 1: return "Assemblage";
                case 2: return "Contrôle";
                default: return $"Opération {index + 1}";
            }
        }

        private string BuildBriefing(PuzzleRound round)
        {
            return $"{round.title}\nObjectif : {round.briefing}\nConseil Lean : {round.leanAdvice}";
        }

        private void EnsureOperations(bool forceDefaults = false)
        {
            if (autoFindSceneReferences)
            {
                AutoFindMissingReferences();
            }

            if (operations == null || operations.Length != 3 || forceDefaults)
            {
                operations = new MachineOperation[3];
            }

            CreateDefaultOperationIfNeeded(0, "Découpe", 9.5f, 0.86f, 0.05f, 350f, 1.0f);
            CreateDefaultOperationIfNeeded(1, "Assemblage", 12.5f, 0.82f, 0.09f, 420f, 1.3f);
            CreateDefaultOperationIfNeeded(2, "Contrôle", 10.5f, 0.9f, 0.04f, 320f, 0.8f);
        }

        private void CreateDefaultOperationIfNeeded(int index, string operationName, float cycleTime, float availability, float defectRate, float fixedCost, float complexity)
        {
            if (operations[index] == null)
            {
                operations[index] = new MachineOperation();
            }

            if (operations[index].machine == null && machines != null && machines.Length > index)
            {
                operations[index].machine = machines[index];
            }

            if (!string.IsNullOrWhiteSpace(operations[index].operationName) && operations[index].operationName != "Operation")
            {
                return;
            }

            operations[index].operationName = operationName;
            operations[index].cycleTime = cycleTime;
            operations[index].availability = availability;
            operations[index].defectRate = defectRate;
            operations[index].parallelMachineCount = 1;
            operations[index].fixedCost = fixedCost;
            operations[index].maintenanceComplexity = complexity;
            operations[index].improvementLevel = 0;
        }

        private void CalculateLinePreview(bool updateRoundKpis = true)
        {
            EnsureOperations();
            taktTime = customerDemand <= 0 ? 0f : availableTime / customerDemand;
            bottleneckIndex = GetBottleneckIndex();
            currentBottleneck = operations[bottleneckIndex].operationName;
            possibleProduction = Mathf.Max(0, Mathf.FloorToInt(GetBottleneckCapacity()));
            if (updateRoundKpis)
            {
                roundDefectRate = CalculateLineDefectRate();
                trs = CalculateTRS();
            }

            maintenanceComplexity = 0f;
            foreach (var operation in operations)
            {
                maintenanceComplexity += operation.maintenanceComplexity + (operation.parallelMachineCount - 1) * 0.35f;
            }

            ColorMachinesByBottleneck();
        }

        private float GetCapacity(MachineOperation operation)
        {
            return operation.cycleTime <= 0f ? 0f : availableTime / operation.cycleTime * operation.parallelMachineCount * operation.availability;
        }

        private int GetBottleneckIndex()
        {
            var index = 0;
            var minCapacity = float.MaxValue;
            for (var i = 0; i < operations.Length; i++)
            {
                var capacity = GetCapacity(operations[i]);
                if (capacity < minCapacity)
                {
                    minCapacity = capacity;
                    index = i;
                }
            }

            return index;
        }

        private float GetBottleneckCapacity()
        {
            return GetCapacity(operations[GetBottleneckIndex()]);
        }

        private MachineOperation GetBottleneckOperation()
        {
            return operations[GetBottleneckIndex()];
        }

        private MachineOperation GetHighestCapacityOperation()
        {
            var selected = operations[0];
            var maxCapacity = float.MinValue;
            foreach (var operation in operations)
            {
                var capacity = GetCapacity(operation);
                if (capacity > maxCapacity)
                {
                    maxCapacity = capacity;
                    selected = operation;
                }
            }

            return selected;
        }

        private MachineOperation FindOperation(Transform machine)
        {
            if (machine == null)
            {
                return null;
            }

            foreach (var operation in operations)
            {
                if (operation.machine == machine)
                {
                    return operation;
                }
            }

            return null;
        }

        private int GetOperationIndex(MachineOperation searchedOperation)
        {
            for (var i = 0; i < operations.Length; i++)
            {
                if (operations[i] == searchedOperation)
                {
                    return i;
                }
            }

            return -1;
        }

        private float CalculateLineDefectRate()
        {
            var goodProbability = 1f;
            foreach (var operation in operations)
            {
                goodProbability *= 1f - Mathf.Clamp01(operation.defectRate);
            }

            return Mathf.Clamp01(1f - goodProbability);
        }

        private float CalculateTRS()
        {
            if (operations.Length == 0)
            {
                return 0f;
            }

            var availabilitySum = 0f;
            foreach (var operation in operations)
            {
                availabilitySum += operation.availability;
            }

            var averageAvailability = availabilitySum / operations.Length;
            var flowPerformance = customerDemand <= 0 ? 1f : Mathf.Clamp01(GetBottleneckCapacity() / customerDemand);
            var quality = 1f - CalculateLineDefectRate();
            return Mathf.Clamp01(averageAvailability * 0.45f + flowPerformance * 0.35f + quality * 0.2f);
        }

        private int CalculateImbalanceStock()
        {
            var bottleneckCapacity = GetBottleneckCapacity();
            var imbalance = 0f;
            foreach (var operation in operations)
            {
                imbalance += Mathf.Max(0f, GetCapacity(operation) - bottleneckCapacity) * 0.08f;
            }

            return Mathf.RoundToInt(imbalance);
        }

        private bool SpendBudget(float cost, string failureMessage)
        {
            if (budget < cost)
            {
                RefreshUi(failureMessage);
                return false;
            }

            budget -= cost;
            return true;
        }

        private void WireButtons()
        {
            WireButton(apply5SButton, Apply5S);
            WireButton(startRoundButton, StartRound);
            WireButton(endRoundButton, EndRound);
            WireButton(buyParallelMachineButton, BuyParallelMachine);
            WireButton(improveMachineButton, ImproveMachine);
            WireButton(rebalanceLineButton, RebalanceLine);
            WireButton(pokaYokeButton, ApplyPokaYoke);
            WireButton(tpmButton, ApplyTPM);
            WireButton(standardWorkButton, ApplyStandardWork);
            WireButton(trainOperatorButton, TrainOperator);
            WireButton(nextPuzzleRoundButton, NextPuzzleRound);
            WireButton(restartPuzzleRoundButton, RestartPuzzleRound);
        }

        private void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void UnwireButtons()
        {
            UnwireButton(apply5SButton, Apply5S);
            UnwireButton(startRoundButton, StartRound);
            UnwireButton(endRoundButton, EndRound);
            UnwireButton(buyParallelMachineButton, BuyParallelMachine);
            UnwireButton(improveMachineButton, ImproveMachine);
            UnwireButton(rebalanceLineButton, RebalanceLine);
            UnwireButton(pokaYokeButton, ApplyPokaYoke);
            UnwireButton(tpmButton, ApplyTPM);
            UnwireButton(standardWorkButton, ApplyStandardWork);
            UnwireButton(trainOperatorButton, TrainOperator);
            UnwireButton(nextPuzzleRoundButton, NextPuzzleRound);
            UnwireButton(restartPuzzleRoundButton, RestartPuzzleRound);
        }

        private void UnwireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void RefreshUi(string message = null)
        {
            SetText(scoreLeanText, $"Score Lean : {scoreLean}/100");
            SetText(rebutsText, $"Rebuts : {roundDefectRate:P0} ({scrapWatches})");
            SetText(trsText, $"TRS : {trs:P0}");
            SetText(stockText, $"Stock : {stock}");
            SetText(budgetText, $"Budget : {budget:0} €");
            SetText(roundMoneyText, $"Argent round : {totalRoundProfit:0} €");
            SetText(leanBonusText, $"Bonus Lean : {leanBonus:0} €");
            SetText(reputationText, $"Réputation : {reputation:0}/100");
            SetText(demandText, $"Demande client : {customerDemand}");
            SetText(taktTimeText, $"Takt time : {taktTime:0.0}s");
            SetText(bottleneckText, $"Goulot : {currentBottleneck}");
            SetText(possibleProductionText, $"Production possible : {possibleProduction}");
            SetText(goodWatchesText, $"Montres conformes : {goodWatches}");
            SetText(serviceRateText, $"Taux de service : {serviceRate:P0}");
            SetText(scoreBusinessText, $"Score Business : {scoreBusiness}/100");
            SetText(maintenanceComplexityText, $"Complexité maintenance : {maintenanceComplexity:0.0}");

            if (!string.IsNullOrEmpty(message))
            {
                SetText(feedbackText, message);
                Debug.Log($"FlowForge: {message}", this);
            }
        }

        private void ColorMachinesByBottleneck()
        {
            for (var i = 0; i < operations.Length; i++)
            {
                var color = i == bottleneckIndex ? bottleneckColor : balancedColor;
                SetMachineColor(operations[i].machine, color);
            }
        }

        private void ColorAllMachines(Color color)
        {
            foreach (var operation in operations)
            {
                SetMachineColor(operation.machine, color);
            }
        }

        private void SetMachineColor(Transform machine, Color color)
        {
            if (machine == null)
            {
                return;
            }

            var renderer = machine.GetComponent<Renderer>() ?? machine.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void AutoFindMissingReferences()
        {
            scoreLeanText = scoreLeanText != null ? scoreLeanText : FindText("ScoreLeanText");
            rebutsText = rebutsText != null ? rebutsText : FindText("RebutsText");
            trsText = trsText != null ? trsText : FindText("TRSText");
            stockText = stockText != null ? stockText : FindText("StockText");
            budgetText = budgetText != null ? budgetText : FindText("BudgetText");
            roundMoneyText = roundMoneyText != null ? roundMoneyText : FindText("RoundMoneyText");
            leanBonusText = leanBonusText != null ? leanBonusText : FindText("LeanBonusText");
            reputationText = reputationText != null ? reputationText : FindText("ReputationText");
            demandText = demandText != null ? demandText : FindText("DemandText");
            taktTimeText = taktTimeText != null ? taktTimeText : FindText("TaktTimeText");
            bottleneckText = bottleneckText != null ? bottleneckText : FindText("BottleneckText");
            possibleProductionText = possibleProductionText != null ? possibleProductionText : FindText("PossibleProductionText");
            goodWatchesText = goodWatchesText != null ? goodWatchesText : FindText("GoodWatchesText");
            serviceRateText = serviceRateText != null ? serviceRateText : FindText("ServiceRateText");
            scoreBusinessText = scoreBusinessText != null ? scoreBusinessText : FindText("ScoreBusinessText");
            maintenanceComplexityText = maintenanceComplexityText != null ? maintenanceComplexityText : FindText("MaintenanceComplexityText");
            feedbackText = feedbackText != null ? feedbackText : FindText("FeedbackText");

            apply5SButton = apply5SButton != null ? apply5SButton : FindButton("Apply5SButton");
            startRoundButton = startRoundButton != null ? startRoundButton : FindButton("StartRoundButton");
            endRoundButton = endRoundButton != null ? endRoundButton : FindButton("EndRoundButton");
            buyParallelMachineButton = buyParallelMachineButton != null ? buyParallelMachineButton : FindButton("BuyParallelMachineButton");
            improveMachineButton = improveMachineButton != null ? improveMachineButton : FindButton("ImproveMachineButton");
            rebalanceLineButton = rebalanceLineButton != null ? rebalanceLineButton : FindButton("RebalanceLineButton");
            nextPuzzleRoundButton = nextPuzzleRoundButton != null ? nextPuzzleRoundButton : FindButton("NextPuzzleRoundButton");
            restartPuzzleRoundButton = restartPuzzleRoundButton != null ? restartPuzzleRoundButton : FindButton("RestartPuzzleRoundButton");

            if (machines == null || machines.Length < 3)
            {
                machines = new Transform[3];
            }

            AssignMachineIfMissing(0, "Machine_01_Decoupe");
            AssignMachineIfMissing(1, "Machine_02_Assemblage");
            AssignMachineIfMissing(2, "Machine_03_Controle");
        }

        private void AssignMachineIfMissing(int index, string objectName)
        {
            if (machines[index] != null)
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

        private static Button FindButton(string objectName)
        {
            var buttonObject = GameObject.Find(objectName);
            return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        }

        private void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
