using System.IO;
using FlowForge.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlowForge.EditorTools
{
    /// <summary>
    /// Builds the first concrete FlowForge prototype scene with real GameObjects, UI links and
    /// playable 5S feedback. Use Tools > FlowForge > Build Prototype Scene from the Unity editor.
    /// </summary>
    public static class FlowForgeSceneBuilder
    {
        private const string ScenePath = "Assets/FlowForge/Scenes/FlowForgePrototype.unity";
        private const string MaterialFolder = "Assets/FlowForge/Materials";

        [MenuItem("Tools/FlowForge/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureFolder("Assets/FlowForge");
            EnsureFolder("Assets/FlowForge/Scenes");
            EnsureFolder(MaterialFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FlowForgePrototype";

            var camera = CreateCamera();
            var light = CreateDirectionalLight();
            var gridRoot = new GameObject("Workshop Grid").transform;
            CreateWorkshopGrid(gridRoot);

            var machineRoot = new GameObject("Machines").transform;
            var machines = CreateMachines(machineRoot);
            CreateFlowMarkers(machineRoot, machines);

            var operatorAvatar = CreateOperator();
            var ui = CreateHud();
            var gameManager = CreateGameManager(camera, light, machines, operatorAvatar, ui);

            Selection.activeGameObject = gameManager.gameObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"FlowForge prototype scene built at {ScenePath}. Open it and press Play.");
        }

        private static Camera CreateCamera()
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

        private static void CreateWorkshopGrid(Transform parent)
        {
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
                    tile.transform.SetParent(parent);
                    tile.transform.position = new Vector3(x * cellSize, -0.05f, z * cellSize);
                    tile.transform.localScale = new Vector3(cellSize * 0.96f, 0.08f, cellSize * 0.96f);
                    tile.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? tileA : tileB;
                }
            }
        }

        private static Transform[] CreateMachines(Transform parent)
        {
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
                machine.transform.SetParent(parent);
                machine.transform.position = definitions[i].Position;
                machine.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);
                machine.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(definitions[i].Name, definitions[i].Color);
                machines[i] = machine.transform;
            }

            return machines;
        }

        private static void CreateFlowMarkers(Transform parent, Transform[] machines)
        {
            var material = GetOrCreateMaterial("Flow Arrow Gold", new Color(1f, 0.82f, 0.18f));
            for (var i = 0; i < machines.Length - 1; i++)
            {
                var start = machines[i].position;
                var end = machines[i + 1].position;
                var direction = end - start;

                var flow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flow.name = $"Flow Marker {i + 1}";
                flow.transform.SetParent(parent);
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

            var canvasObject = new GameObject("Canvas - Lean HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateUiPanel(canvasObject.transform);
            var title = CreateText(panel, "Title", "FLOWFORGE - Prototype jouable", new Vector2(18f, -14f), new Vector2(560f, 34f), 22, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;

            var score = CreateText(panel, "Score Lean Text", "Score Lean : 55/100", new Vector2(18f, -58f), new Vector2(320f, 30f), 19, TextAnchor.MiddleLeft);
            var scraps = CreateText(panel, "Rebuts Text", "Rebuts : 18%", new Vector2(18f, -94f), new Vector2(320f, 30f), 19, TextAnchor.MiddleLeft);
            var trs = CreateText(panel, "TRS Text", "TRS : 62%", new Vector2(18f, -130f), new Vector2(320f, 30f), 19, TextAnchor.MiddleLeft);
            var stock = CreateText(panel, "Stock Text", "Stock : 14", new Vector2(18f, -166f), new Vector2(320f, 30f), 19, TextAnchor.MiddleLeft);
            var feedback = CreateText(panel, "Feedback Text", "Cliquez sur Appliquer 5S pour améliorer l'atelier.", new Vector2(18f, -212f), new Vector2(620f, 32f), 17, TextAnchor.MiddleLeft);
            var button = CreateButton(panel, "Apply 5S Button", "Appliquer 5S", new Vector2(18f, -260f), new Vector2(220f, 48f));

            return new HudReferences(score, scraps, trs, stock, feedback, button);
        }

        private static RectTransform CreateUiPanel(Transform parent)
        {
            var panelObject = new GameObject("Lean KPI Panel");
            panelObject.transform.SetParent(parent, false);
            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.045f, 0.07f, 0.86f);

            var rect = panelImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(680f, 330f);
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

        private static FlowForgePrototypeSceneManager CreateGameManager(Camera camera, Light light, Transform[] machines, Transform operatorAvatar, HudReferences ui)
        {
            var gameManagerObject = new GameObject("GameManager");
            var manager = gameManagerObject.AddComponent<FlowForgePrototypeSceneManager>();
            manager.isoCamera = camera;
            manager.directionalLight = light;
            manager.machines = machines;
            manager.operatorAvatar = operatorAvatar;
            manager.visualFeedbackAnchor = machines.Length > 1 ? machines[1] : operatorAvatar;
            manager.scoreLeanText = ui.ScoreLeanText;
            manager.scrapsText = ui.ScrapsText;
            manager.trsText = ui.TrsText;
            manager.stockText = ui.StockText;
            manager.feedbackText = ui.FeedbackText;
            manager.apply5SButton = ui.Apply5SButton;

            UnityEventTools.AddPersistentListener(ui.Apply5SButton.onClick, manager.Apply5S);
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

        private readonly struct MachineDefinition
        {
            public MachineDefinition(string name, Vector3 position, Color color)
            {
                Name = name;
                Position = position;
                Color = color;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public Color Color { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(Text scoreLeanText, Text scrapsText, Text trsText, Text stockText, Text feedbackText, Button apply5SButton)
            {
                ScoreLeanText = scoreLeanText;
                ScrapsText = scrapsText;
                TrsText = trsText;
                StockText = stockText;
                FeedbackText = feedbackText;
                Apply5SButton = apply5SButton;
            }

            public Text ScoreLeanText { get; }
            public Text ScrapsText { get; }
            public Text TrsText { get; }
            public Text StockText { get; }
            public Text FeedbackText { get; }
            public Button Apply5SButton { get; }
        }
    }
}
