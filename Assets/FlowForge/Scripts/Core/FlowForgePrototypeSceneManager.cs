using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        public Text actionToastText;
        public Text roundTitleText;
        public Text briefingText;

        [Header("Optional decision buttons")]
        public Button startRoundButton;
        public Button endRoundButton;
        public Button buyParallelMachineButton;
        public Button improveMachineButton;
        public Button improveMachine01Button;
        public Button improveMachine02Button;
        public Button improveMachine03Button;
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
        private Coroutine actionToastCoroutine;
        private Coroutine flowFeedbackCoroutine;

        private void Awake()
        {
            if (autoFindSceneReferences)
            {
                AutoFindMissingReferences();
                EnsurePuzzleHud();
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

            if (!roundClosed)
            {
                RefreshUi("Lance d'abord le round pour obtenir le résultat.");
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
                RefreshUi($"{BuildRoundResultSummary()}\nBon choix Lean. {round.successFeedback}\nConseil : {round.leanAdvice}");
            }
            else
            {
                RefreshUi($"{BuildRoundResultSummary()}\nChoix discutable. {round.failureFeedback}\nConseil : {round.leanAdvice}");
            }

            return lastActionWasGoodChoice;
        }

        public void StartRound()
        {
            EnsureOperations();
            PlayProductionFlowFeedback();
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
            ShowActionToast($"Action choisie : machine parallèle sur {operation.operationName}\nCoût : {cost:0} €\nEffet : capacité rapide, complexité maintenance en hausse.\nL'action sera évaluée au lancement du round.");
            PlayMachineFeedback(operation, $"{operation.operationName} renforcée", new Color(1f, 0.82f, 0.25f));
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
            ShowActionToast($"Action choisie : Improve Machine {GetOperationIndex(operation) + 1:00}\nCoût : {cost:0} €\nEffet : cycle réduit, disponibilité améliorée, rebuts légèrement réduits.\nL'action sera évaluée au lancement du round.");
            PlayMachineFeedback(operation, $"{operation.operationName} améliorée", new Color(1f, 0.82f, 0.25f));
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
            ShowActionToast($"Action choisie : Rebalance Line\nCoût : {cost:0} €\nEffet : charge transférée du goulot vers une ressource plus disponible.\nL'action sera évaluée au lancement du round.");
            PlayMachineFeedback(bottleneck, "Goulot allégé", new Color(1f, 0.45f, 0.1f));
            PlayMachineFeedback(helper, "Charge rééquilibrée", new Color(0.35f, 1f, 0.55f));
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
            ShowActionToast($"Action choisie : Apply 5S\nCoût : {cost:0} €\nEffet : atelier clarifié, mouvements et rebuts réduits.\nL'action sera évaluée au lancement du round.");
            PlayWorkshopFeedback("Atelier clarifié", new Color(0.35f, 1f, 0.55f));
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
            ShowActionToast($"Action choisie : Poka-Yoke\nCoût : {cost:0} €\nEffet : erreurs évitées à la source, rebuts fortement réduits.\nL'action sera évaluée au lancement du round.");
            PlayMachineFeedback(GetOperationAtOrFallback(2), "Erreurs évitées", new Color(0.25f, 0.75f, 1f));
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
            ShowActionToast($"Action choisie : Standard Work\nCoût : {cost:0} €\nEffet : cadence stabilisée, variabilité et défauts réduits.\nL'action sera évaluée au lancement du round.");
            PlayWorkshopFeedback("Cadence stabilisée", new Color(0.45f, 0.95f, 1f));
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
            WireButton(improveMachine01Button, ImproveMachine01Decoupe);
            WireButton(improveMachine02Button, ImproveMachine02Assemblage);
            WireButton(improveMachine03Button, ImproveMachine03Controle);
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
            UnwireButton(improveMachine01Button, ImproveMachine01Decoupe);
            UnwireButton(improveMachine02Button, ImproveMachine02Assemblage);
            UnwireButton(improveMachine03Button, ImproveMachine03Controle);
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
            var currentRound = GetCurrentPuzzleRoundOrNull();
            SetText(roundTitleText, currentRound != null ? currentRound.title : "FlowForge — Lean Puzzle");
            SetText(briefingText, currentRound != null ? $"Briefing : {currentRound.briefing}\nConseil Lean : {currentRound.leanAdvice}" : "Briefing : aucun round chargé.");
            SetText(scoreLeanText, $"Score Lean : {scoreLean}/100");
            SetText(rebutsText, $"Rebuts : {roundDefectRate:P0} ({scrapWatches})");
            SetText(trsText, $"TRS : {trs:P0}");
            SetText(stockText, $"Stock : {stock}");
            SetText(budgetText, $"Budget : {budget:0} €");
            SetText(roundMoneyText, hasRoundResults ? $"Résultat round : {totalRoundProfit:0} €" : "Résultat round : en attente");
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
            actionToastText = actionToastText != null ? actionToastText : FindText("ActionToastText");
            roundTitleText = roundTitleText != null ? roundTitleText : FindText("RoundTitleText");
            briefingText = briefingText != null ? briefingText : FindText("BriefingText");

            apply5SButton = apply5SButton != null ? apply5SButton : FindButton("Apply5SButton");
            startRoundButton = startRoundButton != null ? startRoundButton : FindButton("StartRoundButton");
            endRoundButton = endRoundButton != null ? endRoundButton : FindButton("EndRoundButton");
            buyParallelMachineButton = buyParallelMachineButton != null ? buyParallelMachineButton : FindButton("BuyParallelMachineButton");
            improveMachineButton = improveMachineButton != null ? improveMachineButton : FindButton("ImproveMachineButton");
            improveMachine01Button = improveMachine01Button != null ? improveMachine01Button : FindButton("ImproveMachine01Button");
            improveMachine02Button = improveMachine02Button != null ? improveMachine02Button : FindButton("ImproveMachine02Button");
            improveMachine03Button = improveMachine03Button != null ? improveMachine03Button : FindButton("ImproveMachine03Button");
            rebalanceLineButton = rebalanceLineButton != null ? rebalanceLineButton : FindButton("RebalanceLineButton");
            pokaYokeButton = pokaYokeButton != null ? pokaYokeButton : FindButton("PokaYokeButton");
            standardWorkButton = standardWorkButton != null ? standardWorkButton : FindButton("StandardWorkButton");
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


        private PuzzleRound GetCurrentPuzzleRoundOrNull()
        {
            EnsurePuzzleRounds();
            if (puzzleRounds == null || puzzleRounds.Length == 0)
            {
                return null;
            }

            currentPuzzleRoundIndex = Mathf.Clamp(currentPuzzleRoundIndex, 0, puzzleRounds.Length - 1);
            return puzzleRounds[currentPuzzleRoundIndex];
        }

        private void EnsurePuzzleHud()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ConfigureCanvasScaler(canvasObject.AddComponent<CanvasScaler>());
                canvasObject.AddComponent<GraphicRaycaster>();
            }
            else
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                }

                ConfigureCanvasScaler(scaler);

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            var panelTransform = canvas.transform.Find("FlowForgePuzzleHudPanel") as RectTransform;
            if (panelTransform == null)
            {
                var panelObject = new GameObject("FlowForgePuzzleHudPanel");
                panelObject.transform.SetParent(canvas.transform, false);
                var panelImage = panelObject.AddComponent<Image>();
                panelImage.color = new Color(0.035f, 0.045f, 0.07f, 0.9f);
                panelTransform = panelImage.rectTransform;
            }

            panelTransform.anchorMin = new Vector2(0f, 1f);
            panelTransform.anchorMax = new Vector2(0f, 1f);
            panelTransform.pivot = new Vector2(0f, 1f);
            panelTransform.anchoredPosition = new Vector2(18f, -18f);
            panelTransform.sizeDelta = new Vector2(820f, 520f);
            DisableLegacyHudSiblings(canvas.transform, panelTransform);

            roundTitleText = CreateOrFindHudText(panelTransform, "RoundTitleText", "Round courant", new Vector2(16f, -10f), new Vector2(780f, 28f), 20, FontStyle.Bold);
            briefingText = CreateOrFindHudText(panelTransform, "BriefingText", "Briefing", new Vector2(16f, -42f), new Vector2(780f, 58f), 14, FontStyle.Normal);

            scoreLeanText = CreateOrFindHudText(panelTransform, "ScoreLeanText", "Score Lean : 40/100", new Vector2(16f, -112f), new Vector2(210f, 24f), 15, FontStyle.Bold);
            rebutsText = CreateOrFindHudText(panelTransform, "RebutsText", "Rebuts : 18 %", new Vector2(236f, -112f), new Vector2(160f, 24f), 15, FontStyle.Normal);
            trsText = CreateOrFindHudText(panelTransform, "TRSText", "TRS : 62 %", new Vector2(406f, -112f), new Vector2(140f, 24f), 15, FontStyle.Normal);
            stockText = CreateOrFindHudText(panelTransform, "StockText", "Stock : 120", new Vector2(556f, -112f), new Vector2(140f, 24f), 15, FontStyle.Normal);
            bottleneckText = CreateOrFindHudText(panelTransform, "BottleneckText", "Goulot : -", new Vector2(16f, -140f), new Vector2(240f, 24f), 15, FontStyle.Normal);
            budgetText = CreateOrFindHudText(panelTransform, "BudgetText", "Budget : 10000 €", new Vector2(266f, -140f), new Vector2(210f, 24f), 15, FontStyle.Normal);
            roundMoneyText = CreateOrFindHudText(panelTransform, "RoundMoneyText", "Résultat round : en attente", new Vector2(486f, -140f), new Vector2(280f, 24f), 15, FontStyle.Bold);

            startRoundButton = CreateOrFindHudButton(panelTransform, "StartRoundButton", "Start Round", new Vector2(16f, -188f), new Vector2(135f, 32f));
            restartPuzzleRoundButton = CreateOrFindHudButton(panelTransform, "RestartPuzzleRoundButton", "Restart Round", new Vector2(161f, -188f), new Vector2(145f, 32f));
            nextPuzzleRoundButton = CreateOrFindHudButton(panelTransform, "NextPuzzleRoundButton", "Next Round", new Vector2(316f, -188f), new Vector2(135f, 32f));

            improveMachine01Button = CreateOrFindHudButton(panelTransform, "ImproveMachine01Button", "Improve Machine 01", new Vector2(16f, -236f), new Vector2(176f, 32f));
            improveMachine02Button = CreateOrFindHudButton(panelTransform, "ImproveMachine02Button", "Improve Machine 02", new Vector2(202f, -236f), new Vector2(176f, 32f));
            improveMachine03Button = CreateOrFindHudButton(panelTransform, "ImproveMachine03Button", "Improve Machine 03", new Vector2(388f, -236f), new Vector2(176f, 32f));
            rebalanceLineButton = CreateOrFindHudButton(panelTransform, "RebalanceLineButton", "Rebalance Line", new Vector2(574f, -236f), new Vector2(160f, 32f));

            apply5SButton = CreateOrFindHudButton(panelTransform, "Apply5SButton", "Apply 5S", new Vector2(16f, -280f), new Vector2(135f, 32f));
            pokaYokeButton = CreateOrFindHudButton(panelTransform, "PokaYokeButton", "Poka-Yoke", new Vector2(161f, -280f), new Vector2(135f, 32f));
            standardWorkButton = CreateOrFindHudButton(panelTransform, "StandardWorkButton", "Standard Work", new Vector2(306f, -280f), new Vector2(150f, 32f));

            feedbackText = CreateOrFindHudText(panelTransform, "FeedbackText", "Résultat / feedback pédagogique : en attente", new Vector2(16f, -340f), new Vector2(780f, 82f), 15, FontStyle.Italic);
            actionToastText = CreateOrFindHudText(panelTransform, "ActionToastText", string.Empty, new Vector2(16f, -438f), new Vector2(780f, 54f), 14, FontStyle.Bold);
            actionToastText.color = new Color(1f, 0.92f, 0.45f);
        }

        private static Text CreateOrFindHudText(RectTransform parent, string objectName, string value, Vector2 position, Vector2 size, int fontSize, FontStyle fontStyle)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
            {
                existingText.transform.SetParent(parent, false);
                ConfigureHudText(existingText, value, position, size, fontSize, fontStyle);
                return existingText;
            }

            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            ConfigureHudText(text, value, position, size, fontSize, fontStyle);
            return text;
        }

        private static Button CreateOrFindHudButton(RectTransform parent, string objectName, string label, Vector2 position, Vector2 size)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null && existing.TryGetComponent<Button>(out var existingButton))
            {
                existingButton.transform.SetParent(parent, false);
                ConfigureHudButton(existingButton, label, position, size);
                return existingButton;
            }

            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 0.72f, 0.22f);
            var button = buttonObject.AddComponent<Button>();
            ConfigureHudButton(button, label, position, size);
            return button;
        }


        private MachineOperation GetOperationAtOrFallback(int index)
        {
            EnsureOperations();
            if (index >= 0 && index < operations.Length)
            {
                return operations[index];
            }

            return GetBottleneckOperation();
        }

        private void ShowActionToast(string message)
        {
            if (actionToastCoroutine != null)
            {
                StopCoroutine(actionToastCoroutine);
            }

            actionToastCoroutine = StartCoroutine(ActionToastRoutine(message));
            Debug.Log($"FlowForge action: {message}", this);
        }

        private IEnumerator ActionToastRoutine(string message)
        {
            SetText(actionToastText, message);
            if (actionToastText != null)
            {
                actionToastText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(3f);
            SetText(actionToastText, string.Empty);
        }

        private void PlayMachineFeedback(MachineOperation operation, string floatingMessage, Color flashColor)
        {
            if (operation == null || operation.machine == null)
            {
                return;
            }

            StartCoroutine(MachinePulseRoutine(operation.machine, floatingMessage, flashColor));
        }

        private void PlayWorkshopFeedback(string floatingMessage, Color flashColor)
        {
            foreach (var operation in operations)
            {
                PlayMachineFeedback(operation, floatingMessage, flashColor);
            }
        }

        private IEnumerator MachinePulseRoutine(Transform target, string floatingMessage, Color flashColor)
        {
            var originalScale = target.localScale;
            var renderer = target.GetComponent<Renderer>() ?? target.GetComponentInChildren<Renderer>();
            var originalColor = renderer != null ? renderer.material.color : Color.white;
            var floatingText = CreateFloatingText(target, floatingMessage, flashColor);

            const float duration = 0.42f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.Lerp(originalScale, originalScale * 1.14f, pulse);
                if (renderer != null)
                {
                    renderer.material.color = Color.Lerp(originalColor, flashColor, pulse);
                }

                if (floatingText != null)
                {
                    floatingText.transform.position += Vector3.up * (Time.deltaTime * 0.35f);
                    var color = floatingText.color;
                    color.a = 1f - t;
                    floatingText.color = color;
                }

                yield return null;
            }

            target.localScale = originalScale;
            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }

            if (floatingText != null)
            {
                Destroy(floatingText.gameObject);
            }
        }

        private TextMesh CreateFloatingText(Transform target, string message, Color color)
        {
            var textObject = new GameObject($"Feedback_{target.name}");
            textObject.transform.position = target.position + Vector3.up * 1.35f;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.font = GetLegacyUiFont();
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.045f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                textObject.transform.rotation = Quaternion.LookRotation(textObject.transform.position - mainCamera.transform.position);
            }

            return textMesh;
        }

        private void PlayProductionFlowFeedback()
        {
            if (flowFeedbackCoroutine != null)
            {
                StopCoroutine(flowFeedbackCoroutine);
            }

            flowFeedbackCoroutine = StartCoroutine(ProductionFlowRoutine());
        }

        private IEnumerator ProductionFlowRoutine()
        {
            var flowRenderers = FindFlowRenderers();
            foreach (var flowRenderer in flowRenderers)
            {
                if (flowRenderer == null)
                {
                    continue;
                }

                var originalColor = flowRenderer.material.color;
                flowRenderer.material.color = new Color(1f, 0.95f, 0.25f);
                yield return new WaitForSeconds(0.16f);
                flowRenderer.material.color = originalColor;
            }
        }

        private Renderer[] FindFlowRenderers()
        {
            var allRenderers = FindObjectsOfType<Renderer>();
            var flowRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer.name.Contains("Flow Marker") || renderer.transform.parent != null && renderer.transform.parent.name.Contains("Production Flow"))
                {
                    flowRenderers.Add(renderer);
                }
            }

            return flowRenderers.ToArray();
        }

        private string BuildRoundResultSummary()
        {
            var resultLabel = totalRoundProfit >= 0f ? "gain" : "perte";
            return $"ROUND TERMINÉ\nRésultat : {resultLabel} de {Mathf.Abs(totalRoundProfit):0} € | Score Lean : {scoreLean}/100 | Goulot : {currentBottleneck}";
        }

        private static void ConfigureCanvasScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static Font GetLegacyUiFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void ConfigureHudText(Text text, string value, Vector2 position, Vector2 size, int fontSize, FontStyle fontStyle)
        {
            text.text = value;
            text.font = GetLegacyUiFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureHudButton(Button button, string label, Vector2 position, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 0.72f, 0.22f);

            var labelTransform = button.transform.Find($"{button.name}Text") ?? button.transform.Find("Text") ?? button.transform.Find("Label");
            Text labelText;
            if (labelTransform != null && labelTransform.TryGetComponent<Text>(out var existingLabel))
            {
                labelText = existingLabel;
            }
            else
            {
                var labelObject = new GameObject($"{button.name}Text");
                labelObject.transform.SetParent(button.transform, false);
                labelText = labelObject.AddComponent<Text>();
            }

            ConfigureHudText(labelText, label, Vector2.zero, size, 13, FontStyle.Bold);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.08f, 0.06f, 0.02f);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
        }

        private static void DisableLegacyHudSiblings(Transform canvasTransform, RectTransform activePanel)
        {
            for (var i = 0; i < canvasTransform.childCount; i++)
            {
                var child = canvasTransform.GetChild(i);
                if (child == activePanel || child.IsChildOf(activePanel))
                {
                    continue;
                }

                if (child.name.Contains("HUD") || child.name.Contains("KPI") || child.name.Contains("Panel"))
                {
                    child.gameObject.SetActive(false);
                }
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
