using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FlowForge.Core;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity editor entry point for rebuilding the concrete FlowForge prototype scene.
/// The file intentionally lives in Assets/Editor so Unity exposes the menu item.
/// </summary>
public static class FlowForgeSceneBuilder
{
    private const string ScenePath = "Assets/FlowForge/Scenes/FlowForgePrototype.unity";
    private const string MaterialFolder = "Assets/FlowForge/Materials";

    [MenuItem("Tools/FlowForge/Build Prototype Scene", false, 10)]
    public static void BuildPrototypeScene()
    {
        EnsureFolder("Assets/FlowForge");
        EnsureFolder("Assets/FlowForge/Scenes");
        EnsureFolder(MaterialFolder);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FlowForgePrototype";

        var camera = CreateIsometricCamera();
        var light = CreateDirectionalLight();
        CreateWorkshopGrid();
        var machines = CreateMachines();
        CreateFlowMarkers(machines);
        var operatorAvatar = CreateOperator();
        var hud = CreateHud();
        var gameManager = CreateGameManager(camera, light, machines, operatorAvatar, hud);

        Selection.activeGameObject = gameManager.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"FlowForge prototype scene generated: {ScenePath}");
    }

    private static Camera CreateIsometricCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(5.8f, 8.2f, -7.6f);
        cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.67f, 0.78f, 0.92f);
        camera.orthographic = true;
        camera.orthographicSize = 8f;
        return camera;
    }

    private static Light CreateDirectionalLight()
    {
        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.96f, 0.86f);
        return light;
    }

    private static void CreateWorkshopGrid()
    {
        var root = new GameObject("Workshop Grid").transform;
        var tileA = GetOrCreateMaterial("Grid Tile Warm", new Color(0.78f, 0.74f, 0.66f));
        var tileB = GetOrCreateMaterial("Grid Tile Light", new Color(0.87f, 0.84f, 0.76f));

        const int width = 8;
        const int height = 6;
        const float cellSize = 1.35f;

        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < height; z++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Grid Tile {x},{z}";
                tile.transform.SetParent(root);
                tile.transform.position = new Vector3(x * cellSize, -0.05f, z * cellSize);
                tile.transform.localScale = new Vector3(cellSize * 0.96f, 0.08f, cellSize * 0.96f);
                tile.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? tileA : tileB;
            }
        }
    }

    private static Transform[] CreateMachines()
    {
        var root = new GameObject("Machines").transform;
        var definitions = new[]
        {
            new MachineDefinition("Machine 1 - Preparation", new Vector3(2.7f, 0.45f, 4.05f), new Color(0.25f, 0.56f, 1f)),
            new MachineDefinition("Machine 2 - Assembly", new Vector3(5.4f, 0.45f, 4.05f), new Color(0.96f, 0.6f, 0.18f)),
            new MachineDefinition("Machine 3 - Quality", new Vector3(8.1f, 0.45f, 4.05f), new Color(0.34f, 0.86f, 0.48f))
        };

        var machines = new Transform[definitions.Length];
        for (var i = 0; i < definitions.Length; i++)
        {
            var machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.name = definitions[i].Name;
            machine.transform.SetParent(root);
            machine.transform.position = definitions[i].Position;
            machine.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);
            machine.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(definitions[i].Name, definitions[i].Color);
            machines[i] = machine.transform;
        }

        return machines;
    }

    private static void CreateFlowMarkers(Transform[] machines)
    {
        var root = new GameObject("Production Flow").transform;
        var material = GetOrCreateMaterial("Flow Arrow Gold", new Color(1f, 0.82f, 0.18f));

        for (var i = 0; i < machines.Length - 1; i++)
        {
            var start = machines[i].position;
            var end = machines[i + 1].position;
            var direction = end - start;

            var flow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flow.name = $"Flow Marker {i + 1}";
            flow.transform.SetParent(root);
            flow.transform.position = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.15f;
            flow.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            flow.transform.localScale = new Vector3(0.16f, 0.08f, direction.magnitude * 0.55f);
            flow.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private static Transform CreateOperator()
    {
        var op = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        op.name = "Operator 1";
        op.transform.position = new Vector3(1.35f, 0.55f, 1.35f);
        op.transform.localScale = Vector3.one * 0.6f;
        op.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Operator Cyan", new Color(0.18f, 0.88f, 1f));
        return op.transform;
    }

    private static HudReferences CreateHud()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var panel = CreatePanel(canvasObject.transform);
        var title = CreateText(panel, "RoundTitleText", "Round 1 — Goulot évident", new Vector2(18f, -14f), new Vector2(800f, 34f), 22, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        var briefing = CreateText(panel, "BriefingText", "Briefing : observez le goulot et choisissez une action Lean.", new Vector2(18f, -54f), new Vector2(805f, 70f), 15, TextAnchor.UpperLeft);
        var feedback = CreateText(panel, "FeedbackText", "Feedback pédagogique : choisissez une action, puis lancez le round.", new Vector2(18f, -132f), new Vector2(805f, 56f), 15, TextAnchor.UpperLeft);
        feedback.fontStyle = FontStyle.Italic;

        var score = CreateText(panel, "ScoreLeanText", "Score Lean : 40/100", new Vector2(18f, -205f), new Vector2(245f, 26f), 16, TextAnchor.MiddleLeft);
        score.fontStyle = FontStyle.Bold;
        var scraps = CreateText(panel, "RebutsText", "Rebuts : 18 %", new Vector2(285f, -205f), new Vector2(220f, 26f), 16, TextAnchor.MiddleLeft);
        var trs = CreateText(panel, "TRSText", "TRS : 62 %", new Vector2(525f, -205f), new Vector2(180f, 26f), 16, TextAnchor.MiddleLeft);
        var stock = CreateText(panel, "StockText", "Stock : 120", new Vector2(18f, -236f), new Vector2(200f, 26f), 16, TextAnchor.MiddleLeft);
        var bottleneck = CreateText(panel, "BottleneckText", "Goulot : -", new Vector2(285f, -236f), new Vector2(220f, 26f), 16, TextAnchor.MiddleLeft);
        var budget = CreateText(panel, "BudgetText", "Budget : 10000 €", new Vector2(525f, -236f), new Vector2(230f, 26f), 16, TextAnchor.MiddleLeft);
        var roundMoney = CreateText(panel, "RoundMoneyText", "Profit round : 0 €", new Vector2(18f, -267f), new Vector2(260f, 26f), 16, TextAnchor.MiddleLeft);

        var startRound = CreateButton(panel, "StartRoundButton", "Start Round", new Vector2(18f, -318f), new Vector2(150f, 38f));
        var nextRound = CreateButton(panel, "NextPuzzleRoundButton", "Next Round", new Vector2(180f, -318f), new Vector2(150f, 38f));
        var restartRound = CreateButton(panel, "RestartPuzzleRoundButton", "Restart Round", new Vector2(342f, -318f), new Vector2(165f, 38f));

        var improve01 = CreateButton(panel, "ImproveMachine01Button", "Improve Machine 01", new Vector2(18f, -372f), new Vector2(185f, 38f));
        var improve02 = CreateButton(panel, "ImproveMachine02Button", "Improve Machine 02", new Vector2(215f, -372f), new Vector2(185f, 38f));
        var improve03 = CreateButton(panel, "ImproveMachine03Button", "Improve Machine 03", new Vector2(412f, -372f), new Vector2(185f, 38f));
        var rebalance = CreateButton(panel, "RebalanceLineButton", "Rebalance Line", new Vector2(609f, -372f), new Vector2(170f, 38f));

        var apply5S = CreateButton(panel, "Apply5SButton", "Apply 5S", new Vector2(18f, -426f), new Vector2(150f, 38f));
        var pokaYoke = CreateButton(panel, "PokaYokeButton", "Poka-Yoke", new Vector2(180f, -426f), new Vector2(150f, 38f));
        var standardWork = CreateButton(panel, "StandardWorkButton", "Standard Work", new Vector2(342f, -426f), new Vector2(165f, 38f));

        return new HudReferences(
            title, briefing, feedback,
            score, scraps, trs, stock, bottleneck, budget, roundMoney,
            startRound, nextRound, restartRound,
            improve01, improve02, improve03, rebalance,
            apply5S, pokaYoke, standardWork);
    }

    private static RectTransform CreatePanel(Transform parent)
    {
        var panelObject = new GameObject("Lean KPI Panel");
        panelObject.transform.SetParent(parent, false);
        var image = panelObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.045f, 0.07f, 0.86f);

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(860f, 650f);
        return rect;
    }

    private static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
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

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 0.72f, 0.22f);

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var button = buttonObject.AddComponent<Button>();
        var labelText = CreateText(buttonObject.transform, "Label", label, Vector2.zero, size, 18, TextAnchor.MiddleCenter);
        labelText.color = new Color(0.08f, 0.06f, 0.02f);
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static FlowForgePrototypeSceneManager CreateGameManager(Camera camera, Light light, Transform[] machines, Transform operatorAvatar, HudReferences hud)
    {
        var gameManagerObject = new GameObject("GameManager");
        var manager = gameManagerObject.AddComponent<FlowForgePrototypeSceneManager>();
        manager.machines = machines;
        manager.roundTitleText = hud.RoundTitleText;
        manager.briefingText = hud.BriefingText;
        manager.feedbackText = hud.FeedbackText;
        manager.scoreLeanText = hud.ScoreLeanText;
        manager.rebutsText = hud.ScrapsText;
        manager.trsText = hud.TrsText;
        manager.stockText = hud.StockText;
        manager.bottleneckText = hud.BottleneckText;
        manager.budgetText = hud.BudgetText;
        manager.roundMoneyText = hud.RoundMoneyText;
        manager.startRoundButton = hud.StartRoundButton;
        manager.nextPuzzleRoundButton = hud.NextRoundButton;
        manager.restartPuzzleRoundButton = hud.RestartRoundButton;
        manager.improveMachine01Button = hud.ImproveMachine01Button;
        manager.improveMachine02Button = hud.ImproveMachine02Button;
        manager.improveMachine03Button = hud.ImproveMachine03Button;
        manager.rebalanceLineButton = hud.RebalanceLineButton;
        manager.apply5SButton = hud.Apply5SButton;
        manager.pokaYokeButton = hud.PokaYokeButton;
        manager.standardWorkButton = hud.StandardWorkButton;

        EditorUtility.SetDirty(manager);
        return manager;
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        var path = $"{MaterialFolder}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        material = new Material(Shader.Find("Standard"))
        {
            name = materialName,
            color = color
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        for (var i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes;
                return;
            }
        }

        var updatedScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(updatedScenes, 0);
        updatedScenes[updatedScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private struct MachineDefinition
    {
        public MachineDefinition(string name, Vector3 position, Color color)
        {
            Name = name;
            Position = position;
            Color = color;
        }

        public string Name;
        public Vector3 Position;
        public Color Color;
    }

    private struct HudReferences
    {
        public HudReferences(
            Text roundTitleText, Text briefingText, Text feedbackText,
            Text scoreLeanText, Text scrapsText, Text trsText, Text stockText, Text bottleneckText, Text budgetText, Text roundMoneyText,
            Button startRoundButton, Button nextRoundButton, Button restartRoundButton,
            Button improveMachine01Button, Button improveMachine02Button, Button improveMachine03Button, Button rebalanceLineButton,
            Button apply5SButton, Button pokaYokeButton, Button standardWorkButton)
        {
            RoundTitleText = roundTitleText;
            BriefingText = briefingText;
            FeedbackText = feedbackText;
            ScoreLeanText = scoreLeanText;
            ScrapsText = scrapsText;
            TrsText = trsText;
            StockText = stockText;
            BottleneckText = bottleneckText;
            BudgetText = budgetText;
            RoundMoneyText = roundMoneyText;
            StartRoundButton = startRoundButton;
            NextRoundButton = nextRoundButton;
            RestartRoundButton = restartRoundButton;
            ImproveMachine01Button = improveMachine01Button;
            ImproveMachine02Button = improveMachine02Button;
            ImproveMachine03Button = improveMachine03Button;
            RebalanceLineButton = rebalanceLineButton;
            Apply5SButton = apply5SButton;
            PokaYokeButton = pokaYokeButton;
            StandardWorkButton = standardWorkButton;
        }

        public Text RoundTitleText;
        public Text BriefingText;
        public Text FeedbackText;
        public Text ScoreLeanText;
        public Text ScrapsText;
        public Text TrsText;
        public Text StockText;
        public Text BottleneckText;
        public Text BudgetText;
        public Text RoundMoneyText;
        public Button StartRoundButton;
        public Button NextRoundButton;
        public Button RestartRoundButton;
        public Button ImproveMachine01Button;
        public Button ImproveMachine02Button;
        public Button ImproveMachine03Button;
        public Button RebalanceLineButton;
        public Button Apply5SButton;
        public Button PokaYokeButton;
        public Button StandardWorkButton;
    }

}
