using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogSea.Editor
{
    // One-time editor authoring/migration only. Runtime title code never constructs visuals.
    public static class TitleSceneAuthoring
    {
        public const string ScenePath = "Assets/Scenes/Title.unity";
        public const string ScreenPath = "Assets/FrogSea/Prefabs/Title/TitleScreen.prefab";
        const string Prefabs = "Assets/FrogSea/Prefabs";
        const string Art = "Assets/FrogSea/Art";
        [InitializeOnLoadMethod]
        static void MigrateOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var active = SceneManager.GetActiveScene();
                if (active.name != GameSession.TitleScene || active.isDirty) return;
                if (!File.Exists(ScreenPath)) AuthorTitle();
                else RepairInputReferences();
            };
        }
        public static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
        [MenuItem("Frog See the Sea/Author or open title scene")]
        public static void AuthorTitle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScreenPath) && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                RepairInputReferences();
                ValidateAuthoredTitle(); return;
            }
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save scene changes before authoring the title.");
            Folder(Prefabs + "/Title"); Folder(Prefabs + "/UI"); Folder(Prefabs + "/Environment");
            Folder(Prefabs + "/Characters"); Folder(Art); Folder("Assets/FrogSea/Settings/UI");
            var square = BakeSprite("Square", SpriteArt.Square, 1, new Vector2(0.5f, 0.5f));
            var frog = BakeSprite("Frog", SpriteArt.Frog, 24, new Vector2(0.5f, 0));
            CreateButtonPrefabs();
            CreateArtPrefabs(square, frog);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Title Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.115f, 0.14f);
            CreateBackdrop();
            CreateScreen();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidateAuthoredTitle();
            Debug.Log("[TitleAuthoring] Title scene authored with persistent UI, camera, sprites, input and prefab instances.");
        }
        static Sprite BakeSprite(string name, Sprite source, float pixelsPerUnit, Vector2 pivot)
        {
            string path = Art + "/" + name + ".png";
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, source.texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit; importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void CreateButtonPrefabs()
        {
            string basePath = Prefabs + "/UI/MenuButton.prefab";
            if (!File.Exists(basePath))
            {
                var temporary = new GameObject("Button authoring");
                var button = GameUi.Button(temporary.transform, "Button", 0, 0, 210, () => { });
                button.gameObject.name = "MenuButton"; button.onClick.RemoveAllListeners();
                var label = button.GetComponentInChildren<Text>(); label.gameObject.name = "Label";
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(10, 0); label.rectTransform.offsetMax = new Vector2(-10, 0);
                PrefabUtility.SaveAsPrefabAsset(button.gameObject, basePath);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
            string primaryPath = Prefabs + "/UI/PrimaryMenuButton.prefab";
            if (!File.Exists(primaryPath))
            {
                var primary = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(basePath));
                primary.name = "PrimaryMenuButton"; primary.GetComponent<Image>().color = GameUi.Gold;
                primary.GetComponentInChildren<Text>().color = GameUi.PanelColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(primary.GetComponent<Image>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(primary.GetComponentInChildren<Text>());
                PrefabUtility.SaveAsPrefabAsset(primary, primaryPath);
                UnityEngine.Object.DestroyImmediate(primary);
            }
        }
        static void CreateArtPrefabs(Sprite square, Sprite frog)
        {
            string brickPath = Prefabs + "/Environment/WellBrick.prefab";
            if (!File.Exists(brickPath))
            {
                var root = new GameObject("WellBrick", typeof(SpriteRenderer));
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = square;
                renderer.color = new Color(0.09f, 0.16f, 0.18f); renderer.sortingOrder = -20;
                root.transform.localScale = new Vector3(1.25f, 0.94f, 1);
                PrefabUtility.SaveAsPrefabAsset(root, brickPath); UnityEngine.Object.DestroyImmediate(root);
            }
            string frogPath = Prefabs + "/Characters/FrogArtwork.prefab";
            if (!File.Exists(frogPath))
            {
                var root = new GameObject("FrogArtwork", typeof(SpriteRenderer));
                root.GetComponent<SpriteRenderer>().sprite = frog;
                PrefabUtility.SaveAsPrefabAsset(root, frogPath); UnityEngine.Object.DestroyImmediate(root);
            }
        }
        static void CreateBackdrop()
        {
            string path = Prefabs + "/Title/TitleBackdrop.prefab";
            if (File.Exists(path)) { PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)); return; }
            var root = new GameObject("TitleBackdrop");
            var brickAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Environment/WellBrick.prefab");
            for (int row = 0; row < 12; row++)
            for (int col = 0; col < 7; col++)
            {
                var brick = (GameObject)PrefabUtility.InstantiatePrefab(brickAsset, root.transform);
                brick.name = "Brick " + row + "-" + col;
                brick.transform.localPosition = new Vector3(2.2f + col * 1.3f + row % 2 * 0.65f, row - 5.5f, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(brick.transform);
            }
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Square.png");
            Block(root.transform, "Moss ledge", new Vector2(5, -2.2f), new Vector2(4, 0.13f), new Color(0.43f, 0.6f, 0.34f), 0, square);
            Block(root.transform, "Ledge stone", new Vector2(5, -2.45f), new Vector2(4, 0.4f), new Color(0.45f, 0.48f, 0.36f), -1, square);
            var frog = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Characters/FrogArtwork.prefab"), root.transform);
            frog.name = "Title frog"; frog.transform.localPosition = new Vector3(5, -2.12f, 0); frog.transform.localScale = Vector3.one * 2.3f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(frog.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Prefabs + "/Title/TitleBackdrop.prefab", InteractionMode.AutomatedAction);
        }
        static void Block(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order, Sprite sprite)
        {
            var root = new GameObject(name, typeof(SpriteRenderer)); root.transform.SetParent(parent, false);
            root.transform.localPosition = position; root.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        }
        public static Button Button(Transform parent, string label, float x, float y, float width, UnityAction action, bool primary = false)
        {
            string path = Prefabs + "/UI/" + (primary ? "PrimaryMenuButton" : "MenuButton") + ".prefab";
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            root.name = label + " button";
            var rect = root.GetComponent<RectTransform>(); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, 50);
            var text = root.GetComponentInChildren<Text>(); text.text = label;
            var button = root.GetComponent<Button>(); UnityEventTools.AddPersistentListener(button.onClick, action);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            return button;
        }
        public static void Ref(SerializedObject serialized, string name, UnityEngine.Object value)
        { serialized.FindProperty(name).objectReferenceValue = value; }
        static void CreateScreen()
        {
            if (File.Exists(ScreenPath)) { PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath)); return; }
            var root = new GameObject("TitleScreen", typeof(TitleController), typeof(TitleView));
            var controller = root.GetComponent<TitleController>();
            // Build Canvas in the editor without GameUi.Canvas's runtime EventSystem creation.
            var canvasObject = new GameObject("Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false); canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var frame = GameUi.Rect(canvasObject.transform, "Centered title layout", 0, 0, 1280, 720);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f); frame.anchoredPosition = Vector2.zero;
            var home = GameUi.Rect(frame, "Title home", 0, 0, 1280, 720);
            var selection = GameUi.Rect(frame, "Rule selection", 0, 0, 1280, 720);
            GameUi.Text(home, "A SMALL FROG. A VERY TALL WELL.", 106, 114, 700, 32, 14, GameUi.Muted).name = "Tagline";
            GameUi.Text(home, "FROG\nSEE THE SEA", 102, 170, 750, 180, 66, GameUi.Ink, true).name = "Game title";
            GameUi.Text(home, "One leap at a time. Somewhere above, the sea awaits.", 108, 378, 650, 48, 18, GameUi.Muted).name = "Introduction";
            Button(home, "Start", 108, 463, 236, controller.OpenRules, true);
            GameUi.Text(home, "2D JUMP GAME   /   DRAG & RELEASE", 108, 631, 650, 30, 13, GameUi.Muted).name = "Footer";
            GameUi.Panel(selection, "Rule panel", 80, 85, 730, 560, GameUi.PanelColor);
            GameUi.Text(selection, "CHOOSE YOUR CLIMB", 112, 115, 650, 45, 30, GameUi.Ink, true).name = "Rule heading";
            Button(selection, "Stages", 112, 185, 210, controller.SelectStages);
            Button(selection, "Endless", 336, 185, 210, controller.SelectEndless);
            var rule = GameUi.Text(selection, "STAGES / Find the sea", 112, 267, 640, 40, 26, GameUi.Gold, true); rule.name = "Rule name";
            var description = GameUi.Text(selection, "Three wells. Reach the surface to see the sea.\nA missed jump can send you back down.", 112, 322, 620, 85, 18, GameUi.Ink); description.name = "Rule description";
            var previous = Button(selection, "<", 112, 418, 55, controller.PreviousStage);
            var stage = GameUi.Text(selection, "Stage 1  /  42 m", 190, 428, 210, 35, 20, GameUi.Ink); stage.name = "Stage label";
            var next = Button(selection, ">", 415, 418, 55, controller.NextStage);
            previous.interactable = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(previous);
            Button(selection, "Climb", 112, 530, 250, controller.StartGame, true);
            Button(selection, "Back", 382, 530, 160, controller.Back);
            var view = root.GetComponent<TitleView>(); var serializedView = new SerializedObject(view);
            Ref(serializedView, "home", home.gameObject); Ref(serializedView, "selection", selection.gameObject);
            Ref(serializedView, "ruleName", rule); Ref(serializedView, "description", description); Ref(serializedView, "stageLabel", stage);
            Ref(serializedView, "previous", previous); Ref(serializedView, "next", next);
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            var serializedController = new SerializedObject(controller); Ref(serializedController, "view", view); serializedController.ApplyModifiedPropertiesWithoutUndo();
            selection.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, ScreenPath, InteractionMode.AutomatedAction);
        }
        static InputActionReference ActionRef(InputActionAsset asset, string actionName)
        {
            string path = "Assets/FrogSea/Settings/UI/" + actionName + ".asset";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (reference == null)
            {
                reference = InputActionReference.Create(asset.FindAction("UI/" + actionName, true));
                AssetDatabase.CreateAsset(reference, path);
            }
            return reference;
        }
        public static void CreateEventSystem()
        {
            var root = new GameObject("Title EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = root.GetComponent<InputSystemUIInputModule>();
            BindSavedInput(module);
        }
        static void BindSavedInput(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            module.actionsAsset = asset;
            // Property setters keep the old reference when its action is equal. Assign saved references directly.
            var serialized = new SerializedObject(module);
            Ref(serialized, "m_PointAction", ActionRef(asset, "Point")); Ref(serialized, "m_LeftClickAction", ActionRef(asset, "Click"));
            Ref(serialized, "m_RightClickAction", ActionRef(asset, "RightClick")); Ref(serialized, "m_MiddleClickAction", ActionRef(asset, "MiddleClick"));
            Ref(serialized, "m_ScrollWheelAction", ActionRef(asset, "ScrollWheel")); Ref(serialized, "m_MoveAction", ActionRef(asset, "Navigate"));
            Ref(serialized, "m_SubmitAction", ActionRef(asset, "Submit")); Ref(serialized, "m_CancelAction", ActionRef(asset, "Cancel"));
            Ref(serialized, "m_TrackedDevicePositionAction", ActionRef(asset, "TrackedDevicePosition"));
            Ref(serialized, "m_TrackedDeviceOrientationAction", ActionRef(asset, "TrackedDeviceOrientation"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void RepairInputReferences()
        {
            var module = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
            if (module == null || (module.leftClick != null && AssetDatabase.Contains(module.leftClick))) return;
            BindSavedInput(module); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        [MenuItem("Frog See the Sea/Validate authored title")]
        public static void ValidateAuthoredTitle()
        {
            if (SceneManager.GetActiveScene().name != GameSession.TitleScene)
                throw new InvalidOperationException("Open Title scene before validating authored title.");
            var scene = SceneManager.GetActiveScene();
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var buttons = transforms.Select(t => t.GetComponent<Button>()).Where(button => button != null).ToArray();
            if (buttons.Length != 7) throw new InvalidOperationException("Expected all 7 title buttons to exist before Play.");
            foreach (var button in buttons)
                if (button.onClick.GetPersistentEventCount() != 1 || !(button.onClick.GetPersistentTarget(0) is TitleController))
                    throw new InvalidOperationException("Button needs an Inspector-visible persistent callback: " + button.name);
            foreach (var sprite in transforms.Select(t => t.GetComponent<SpriteRenderer>()).Where(sprite => sprite != null))
                if (!AssetDatabase.Contains(sprite.sprite) || !AssetDatabase.Contains(sprite.sprite.texture))
                    throw new InvalidOperationException("Title artwork must reference saved sprite assets.");
            if (!transforms.Any(t => t.GetComponent<Camera>() != null) || !transforms.Any(t => t.GetComponent<Canvas>() != null) ||
                !transforms.Any(t => t.GetComponent<EventSystem>() != null))
                throw new InvalidOperationException("Camera, Canvas and EventSystem must be placed in the title scene.");
            var controller = UnityEngine.Object.FindAnyObjectByType<TitleController>();
            if (controller == null || new SerializedObject(controller).FindProperty("view").objectReferenceValue == null)
                throw new InvalidOperationException("TitleController must have a serialized TitleView reference.");
            if (!Application.isPlaying && !PrefabUtility.IsPartOfPrefabInstance(controller))
                throw new InvalidOperationException("Title UI must be an authored prefab instance.");
            var module = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
            if (module == null || module.actionsAsset == null || module.leftClick == null || !AssetDatabase.Contains(module.leftClick))
                throw new InvalidOperationException("UI input bindings must be saved asset references.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/FrogSea-title-authoring.txt", "PASS: " + transforms.Length +
                " preplaced objects; 7 persistent button callbacks; saved sprite/input assets; assigned view; prefab UI; camera/Canvas/EventSystem.\n");
            Debug.Log("[TitleAuthoring] Authored title validation PASS (" + transforms.Length + " objects).");
        }
        public static int ObjectCount() => SceneManager.GetActiveScene().GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }
}
