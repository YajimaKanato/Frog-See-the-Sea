using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogSea.Editor
{
    // Editor-only initial authoring. Existing prefab/scene edits are never regenerated on Play.
    public static class InGameSceneAuthoring
    {
        public const string RigPath = "Assets/FrogSea/Prefabs/InGame/InGameRig.prefab";
        const string Root = "Assets/FrogSea/Prefabs";
        const string ScenePath = "Assets/Scenes/InGame.unity";
        static Sprite square;
        static GameObject Asset(string relative) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + relative + ".prefab");
        static GameObject Instance(string relative, Transform parent = null) => (GameObject)PrefabUtility.InstantiatePrefab(Asset(relative), parent);
        static void Save(GameObject root, string relative)
        { PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + relative + ".prefab"); UnityEngine.Object.DestroyImmediate(root); }
        static void Ref(SerializedObject obj, string name, UnityEngine.Object value) => TitleSceneAuthoring.Ref(obj, name, value);
        static void Record(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(RigPath)) return;
                var scene = SceneManager.GetActiveScene();
                if (!scene.isDirty && (scene.name == GameSession.TitleScene || scene.name == GameSession.PlayScene))
                    AuthorScene();
            };
        }
        [MenuItem("Frog See the Sea/Author or open ingame scene")]
        public static void AuthorScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(RigPath) && File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); ValidateScene(); return; }
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save current scene edits first.");
            foreach (string folder in new[] { "InGame", "Platforms", "Stages", "Environment", "Characters", "UI" })
                TitleSceneAuthoring.Folder(Root + "/" + folder);
            TitleSceneAuthoring.Folder("Assets/FrogSea/Audio");
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FrogSea/Art/Square.png");
            CreatePlatformPrefabs(); CreateWall(); CreateFloor(); CreateSea(); CreatePlayer(); CreateCamera(); CreateHud(); CreateStages();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = new GameObject("InGameRig", typeof(FrogSeaGame), typeof(WellView), typeof(AudioSource), typeof(FeedbackView));
            var game = rig.GetComponent<FrogSeaGame>(); var world = rig.GetComponent<WellView>();
            var camera = Instance("InGame/FollowCamera", rig.transform);
            var frog = Instance("Characters/FrogPlayer", rig.transform);
            var hud = Instance("UI/InGameHUD", rig.transform);
            var wall = Instance("Environment/WellWall", rig.transform); wall.transform.localPosition = new Vector3(0, 36, 0);
            var floor = Instance("Environment/WellFloor", rig.transform);
            var sea = Instance("Environment/SeaEnding", rig.transform); sea.SetActive(false);
            var layouts = new StageLayout[3];
            for (int i = 0; i < 3; i++)
            {
                var stage = Instance("Stages/Stage0" + (i + 1), rig.transform);
                layouts[i] = stage.GetComponent<StageLayout>(); stage.SetActive(i == 0);
            }
            var poolRoot = new GameObject("Endless visual pool"); poolRoot.transform.SetParent(rig.transform, false);
            var slots = new PlatformDefinition[128];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = Instance("Platforms/Platform", poolRoot.transform); slot.name = "Endless slot " + i;
                slots[i] = slot.GetComponent<PlatformDefinition>(); slot.SetActive(false);
            }
            poolRoot.SetActive(false);
            var worldData = new SerializedObject(world);
            Array(worldData, "stages", layouts); Array(worldData, "endlessPool", slots);
            Ref(worldData, "backdrop", wall.transform); Ref(worldData, "floor", floor); Ref(worldData, "sea", sea.transform);
            Ref(worldData, "endlessRoot", poolRoot); Ref(worldData, "viewCamera", camera.GetComponent<Camera>()); worldData.ApplyModifiedPropertiesWithoutUndo();
            var gameData = new SerializedObject(game);
            Ref(gameData, "settings", AssetDatabase.LoadAssetAtPath<JumpSettings>("Assets/FrogSea/Resources/FrogJumpSettings.asset"));
            Ref(gameData, "world", world); Ref(gameData, "cameraView", camera.GetComponent<WellCameraView>());
            Ref(gameData, "frogView", frog.GetComponent<FrogView>()); Ref(gameData, "hud", hud.GetComponent<GameHudView>());
            Ref(gameData, "feedback", rig.GetComponent<FeedbackView>()); gameData.ApplyModifiedPropertiesWithoutUndo();
            var hudData = new SerializedObject(hud.GetComponent<GameHudView>()); Ref(hudData, "game", game); hudData.ApplyModifiedPropertiesWithoutUndo();
            var audio = rig.GetComponent<AudioSource>(); audio.playOnAwake = false; audio.volume = 0.2f;
            var feedback = new SerializedObject(rig.GetComponent<FeedbackView>()); Ref(feedback, "source", audio);
            Ref(feedback, "jump", Tone("Jump", 370, 620, 0.12f)); Ref(feedback, "land", Tone("Land", 130, 65, 0.08f));
            Ref(feedback, "wall", Tone("Wall", 480, 720, 0.09f)); Ref(feedback, "goal", Tone("Goal", 520, 1040, 0.8f));
            feedback.ApplyModifiedPropertiesWithoutUndo();
            Record(rig);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, RigPath, InteractionMode.AutomatedAction);
            TitleSceneAuthoring.CreateEventSystem();
            UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>().name = "InGame EventSystem";
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            ValidateScene();
            Debug.Log("[InGameAuthoring] Scene, platform variants, walls, stages, player, camera, HUD and audio saved.");
        }
        static void Array<T>(SerializedObject data, string field, T[] values) where T : UnityEngine.Object
        {
            var array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        static SpriteRenderer Block(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
        static void CreatePlatformPrefabs()
        {
            if (Asset("Environment/MossWall") == null)
            {
                var wall = new GameObject("MossWall");
                Block(wall.transform, "Grippable moss", Vector2.zero, new Vector2(0.52f, 1.5f), new Color(0.42f, 0.63f, 0.35f), 5);
                Save(wall, "Environment/MossWall");
            }
            if (Asset("Platforms/Platform") == null)
            {
                var root = new GameObject("Platform", typeof(PlatformDefinition));
                var art = new GameObject("Artwork", typeof(PlatformArtwork)); art.transform.SetParent(root.transform, false);
                var shadow = Block(art.transform, "Lower edge", new Vector2(0, -0.24f), Vector2.one, new Color(0.22f, 0.27f, 0.24f), 2);
                var stone = Block(art.transform, "Stone", new Vector2(0, -0.13f), Vector2.one, new Color(0.56f, 0.55f, 0.43f), 3);
                var top = Block(art.transform, "Moss top", new Vector2(0, -0.025f), Vector2.one, new Color(0.43f, 0.62f, 0.32f), 4);
                var wall = Instance("Environment/MossWall", art.transform); wall.transform.localPosition = new Vector3(0, -0.95f, 0); wall.SetActive(false);
                var artData = new SerializedObject(art.GetComponent<PlatformArtwork>());
                Ref(artData, "stone", stone); Ref(artData, "shadow", shadow.transform); Ref(artData, "top", top.transform); Ref(artData, "mossWall", wall); artData.ApplyModifiedPropertiesWithoutUndo();
                var definition = new SerializedObject(root.GetComponent<PlatformDefinition>());
                Ref(definition, "artwork", art.GetComponent<PlatformArtwork>()); definition.ApplyModifiedPropertiesWithoutUndo();
                root.GetComponent<PlatformDefinition>().RefreshArtwork(); Record(root); Save(root, "Platforms/Platform");
            }
            foreach (string variant in new[] { "MossPlatform", "GoalPlatform" })
            {
                if (Asset("Platforms/" + variant) != null) continue;
                var root = Instance("Platforms/Platform"); root.name = variant;
                var data = new SerializedObject(root.GetComponent<PlatformDefinition>());
                data.FindProperty("sticky").boolValue = variant == "MossPlatform"; data.FindProperty("goal").boolValue = variant == "GoalPlatform";
                data.ApplyModifiedPropertiesWithoutUndo(); root.GetComponent<PlatformDefinition>().RefreshArtwork(); Record(root);
                Save(root, "Platforms/" + variant);
            }
        }
        static void CreateStages()
        {
            for (int number = 1; number <= 3; number++)
            {
                string path = "Stages/Stage0" + number; if (Asset(path) != null) continue;
                var root = new GameObject("Stage0" + number, typeof(StageLayout)); var model = new StageModel(GameMode.Stages, number, 32);
                var platforms = new System.Collections.Generic.List<PlatformDefinition>();
                foreach (var ledge in model.Ledges)
                {
                    if (ledge.Y == 0) continue;
                    var platform = Instance("Platforms/" + (ledge.Y >= model.GoalHeight ? "GoalPlatform" : ledge.Sticky ? "MossPlatform" : "Platform"), root.transform);
                    platform.name = "Ledge y" + ledge.Y.ToString("0.0") + " x" + ledge.X.ToString("0");
                    platform.transform.localPosition = new Vector3(ledge.X, ledge.Y, 0);
                    var definition = platform.GetComponent<PlatformDefinition>(); var data = new SerializedObject(definition);
                    data.FindProperty("width").floatValue = ledge.Width; data.ApplyModifiedPropertiesWithoutUndo();
                    definition.RefreshArtwork(); Record(platform); platforms.Add(definition);
                }
                var stage = new SerializedObject(root.GetComponent<StageLayout>());
                stage.FindProperty("stageNumber").intValue = number; stage.FindProperty("goalHeight").floatValue = model.GoalHeight;
                Array(stage, "platforms", platforms.ToArray()); stage.ApplyModifiedPropertiesWithoutUndo();
                Save(root, path);
            }
        }
        static void CreateWall()
        {
            if (Asset("Environment/WellWall") != null) return;
            string path = "Assets/FrogSea/Art/WellBricks.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
                {
                    bool mortar = y % 32 < 2 || (x + (y / 32) * 32) % 64 < 2;
                    float tone = ((x / 64 + y / 32) % 3) * 0.008f;
                    texture.SetPixel(x, y, mortar ? new Color(0.085f, 0.135f, 0.16f) : new Color(0.12f + tone, 0.19f + tone, 0.205f + tone));
                }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            var root = new GameObject("WellWall", typeof(SpriteRenderer)); var renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(96, 100); renderer.sortingOrder = -90;
            Save(root, "Environment/WellWall");
        }
        static void CreateFloor()
        {
            if (Asset("Environment/WellFloor") != null) return;
            var root = new GameObject("WellFloor");
            Block(root.transform, "Bottom stone", new Vector2(0, -0.65f), new Vector2(96, 1.3f), new Color(0.27f, 0.32f, 0.28f), 0);
            Block(root.transform, "Bottom moss", new Vector2(0, -0.035f), new Vector2(96, 0.07f), new Color(0.37f, 0.48f, 0.29f), 1);
            Save(root, "Environment/WellFloor");
        }
        static void CreateSea()
        {
            if (Asset("Environment/SeaEnding") != null) return;
            var root = new GameObject("SeaEnding");
            Block(root.transform, "Sky", new Vector2(0, 9), new Vector2(96, 42), new Color(0.67f, 0.83f, 0.79f), -80);
            Block(root.transform, "Sea", new Vector2(0, -2), new Vector2(96, 14), new Color(0.26f, 0.60f, 0.63f), -70);
            Block(root.transform, "Horizon", new Vector2(0, 4.9f), new Vector2(96, 0.12f), new Color(0.79f, 0.89f, 0.8f), -69);
            for (int i = 0; i < 23; i++)
                Block(root.transform, "Water glint", new Vector2(i * 7 % 37 - 18, 4.1f - (i % 7) * 0.6f), new Vector2(1 + i % 4, 0.035f), new Color(0.62f, 0.80f, 0.73f), -65);
            Block(root.transform, "Sun", new Vector2(7, 9.5f), new Vector2(1.7f, 1.7f), new Color(0.98f, 0.86f, 0.57f), -60);
            Block(root.transform, "Shore", new Vector2(0, -1.2f), new Vector2(96, 2.4f), new Color(0.79f, 0.73f, 0.51f), 0);
            Block(root.transform, "Grass", new Vector2(0, -0.03f), new Vector2(96, 0.1f), new Color(0.45f, 0.61f, 0.34f), 1);
            Save(root, "Environment/SeaEnding");
        }
        static void CreatePlayer()
        {
            if (Asset("Characters/FrogPlayer") != null) return;
            var root = new GameObject("FrogPlayer", typeof(FrogView));
            var art = Instance("Characters/FrogArtwork", root.transform); art.GetComponent<SpriteRenderer>().sortingOrder = 20;
            var data = new SerializedObject(root.GetComponent<FrogView>()); Ref(data, "artwork", art.transform); data.ApplyModifiedPropertiesWithoutUndo();
            Record(root); Save(root, "Characters/FrogPlayer");
        }
        static void CreateCamera()
        {
            if (Asset("InGame/FollowCamera") != null) return;
            var root = new GameObject("FollowCamera", typeof(Camera), typeof(AudioListener), typeof(WellCameraView));
            root.tag = "MainCamera"; root.transform.position = new Vector3(0, 2.8f, -10);
            var camera = root.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6.7f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 50; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.12f, 0.15f); Save(root, "InGame/FollowCamera");
        }
        static GameObject Modal(Transform canvas, string name, out RectTransform content)
        {
            var overlay = GameUi.Panel(canvas, name, 0, 0, 0, 0, new Color(0.025f, 0.06f, 0.08f, 0.65f));
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            content = GameUi.Panel(overlay, "Dialog", 0, 0, 500, 260, GameUi.PanelColor);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f); content.anchoredPosition = Vector2.zero;
            return overlay.gameObject;
        }
        static void CreateHud()
        {
            if (Asset("UI/InGameHUD") != null) return;
            var root = new GameObject("InGameHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameHudView));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var view = root.GetComponent<GameHudView>();
            var header = GameUi.Panel(root.transform, "HUD", 0, 0, 1280, 82, GameUi.PanelColor);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1); header.sizeDelta = new Vector2(0, 82);
            GameUi.Text(header, "FROG / SEE THE SEA", 26, 15, 320, 30, 22, GameUi.Ink, true);
            var details = GameUi.Text(header, "STAGE 1 / 42 m", 28, 49, 500, 24, 13, GameUi.Muted); details.name = "Run details";
            var altitude = GameUi.Text(header, "0.0 m", 0, 18, 230, 45, 30, GameUi.Gold, true); altitude.name = "Altitude";
            altitude.rectTransform.anchorMin = altitude.rectTransform.anchorMax = new Vector2(0.5f, 1); altitude.rectTransform.anchoredPosition = new Vector2(-70, -18);
            var pause = TitleSceneAuthoring.Button(header, "Pause", 0, 16, 94, view.TogglePause).GetComponent<RectTransform>();
            pause.anchorMin = pause.anchorMax = new Vector2(1, 1); pause.anchoredPosition = new Vector2(-224, -16);
            var title = TitleSceneAuthoring.Button(header, "Title", 0, 16, 94, view.ReturnToTitle).GetComponent<RectTransform>();
            title.anchorMin = title.anchorMax = new Vector2(1, 1); title.anchoredPosition = new Vector2(-116, -16);
            var state = GameUi.Text(root.transform, "Drag back and release to jump. / R Restart / ESC Pause", 24, 0, 1000, 36, 15, GameUi.Ink); state.name = "State";
            state.rectTransform.anchorMin = state.rectTransform.anchorMax = new Vector2(0, 0); state.rectTransform.anchoredPosition = new Vector2(24, 46);
            var notice = GameUi.Text(root.transform, "", 28, 100, 1000, 50, 16, GameUi.Gold); notice.name = "Notice";
            var pausePanel = Modal(root.transform, "Pause overlay", out var pauseContent);
            GameUi.Text(pauseContent, "Take a breath.", 30, 22, 460, 60, 32, GameUi.Ink, true);
            TitleSceneAuthoring.Button(pauseContent, "Resume", 30, 105, 440, view.TogglePause, true);
            TitleSceneAuthoring.Button(pauseContent, "Restart", 30, 173, 210, view.Restart);
            TitleSceneAuthoring.Button(pauseContent, "Title", 260, 173, 210, view.ReturnToTitle);
            var finishPanel = Modal(root.transform, "Clear overlay", out var finishContent);
            GameUi.Text(finishContent, "So this is the sea.", 30, 22, 460, 60, 32, GameUi.Ink, true);
            var result = GameUi.Text(finishContent, "Stage cleared", 30, 90, 440, 55, 18, GameUi.Ink); result.name = "Result";
            TitleSceneAuthoring.Button(finishContent, "Next climb", 30, 169, 230, view.NextClimb, true);
            TitleSceneAuthoring.Button(finishContent, "Title", 280, 169, 190, view.ReturnToTitle);
            var data = new SerializedObject(view);
            Ref(data, "altitude", altitude); Ref(data, "details", details); Ref(data, "state", state); Ref(data, "notice", notice);
            Ref(data, "result", result); Ref(data, "pausePanel", pausePanel); Ref(data, "finishPanel", finishPanel); data.ApplyModifiedPropertiesWithoutUndo();
            pausePanel.SetActive(false); finishPanel.SetActive(false); Record(root); Save(root, "UI/InGameHUD");
        }
        static AudioClip Tone(string name, float start, float end, float duration)
        {
            string path = "Assets/FrogSea/Audio/" + name + ".wav";
            if (!File.Exists(path))
            {
                const int rate = 22050; int count = (int)(rate * duration);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                    writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2); float phase = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / count; phase += Mathf.Lerp(start, end, t) * Mathf.PI * 2 / rate;
                        writer.Write((short)(Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * 0.22f * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("Frog See the Sea/Validate authored ingame")]
        public static void ValidateScene()
        {
            if (SceneManager.GetActiveScene().name != GameSession.PlayScene) throw new InvalidOperationException("Open InGame first.");
            var game = UnityEngine.Object.FindAnyObjectByType<FrogSeaGame>(); if (game == null) throw new InvalidOperationException("Missing game.");
            var data = new SerializedObject(game);
            foreach (string field in new[] { "settings", "world", "cameraView", "frogView", "hud", "feedback" })
                if (data.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Missing authored reference: " + field);
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var platforms = roots.SelectMany(root => root.GetComponentsInChildren<PlatformDefinition>(true)).ToArray();
            if (platforms.Length < 600) throw new InvalidOperationException("Expected three authored full-circle stages and an endless pool.");
            if (!Application.isPlaying && !PrefabUtility.IsPartOfPrefabInstance(game)) throw new InvalidOperationException("Rig must be a prefab instance.");
            var buttons = roots.SelectMany(root => root.GetComponentsInChildren<Button>(true));
            foreach (var button in buttons)
                if (button.onClick.GetPersistentEventCount() != 1) throw new InvalidOperationException("HUD callback is not saved.");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/FrogSea-ingame-authoring.txt",
                "PASS: authored camera, player, wall, floor, sea, HUD, saved audio and " + platforms.Length + " platform prefab instances.\n");
            Debug.Log("[InGameAuthoring] Authored ingame validation PASS.");
        }
        public static int ObjectCount() => SceneManager.GetActiveScene().GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }
}
