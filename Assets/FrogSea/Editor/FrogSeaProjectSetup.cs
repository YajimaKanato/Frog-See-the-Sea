using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FrogSea.Editor
{
    public static class FrogSeaProjectSetup
    {
        const string TitlePath = "Assets/Scenes/Title.unity";
        const string PlayPath = "Assets/Scenes/InGame.unity";
        [InitializeOnLoadMethod]
        static void InitializeProject()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(TitlePath) && File.Exists(PlayPath)) return;
                if (SceneManager.GetActiveScene().isDirty) return;
                CreateScene();
            };
        }
        [MenuItem("Frog See the Sea/Create or open 2D scenes")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Configure2DRenderer();
            if (!AssetDatabase.IsValidFolder("Assets/FrogSea/Resources")) AssetDatabase.CreateFolder("Assets/FrogSea", "Resources");
            const string config = "Assets/FrogSea/Resources/FrogJumpSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<JumpSettings>(config) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<JumpSettings>(), config);
            if (!File.Exists(TitlePath))
            {
                TitleSceneAuthoring.AuthorTitle();
            }
            if (!File.Exists(PlayPath))
            {
                InGameSceneAuthoring.AuthorScene();
            }
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(TitlePath, true),
                new EditorBuildSettingsScene(PlayPath, true)
            };
            EditorSceneManager.OpenScene(TitlePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[FrogSea2D] Title and InGame scenes configured. Title is build scene 0.");
            ValidateProject();
        }
        static void Configure2DRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset");
            if (pipeline == null || renderer == null) throw new System.InvalidOperationException("2D URP assets are missing.");
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList"); list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Frog See the Sea/Validate 2D scene structure")]
        public static void ValidateProject()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 2 || scenes[0].path != TitlePath || scenes[1].path != PlayPath ||
                !scenes[0].enabled || !scenes[1].enabled || !File.Exists(TitlePath) || !File.Exists(PlayPath))
                throw new System.InvalidOperationException("Expected Title and InGame as the two enabled build scenes.");
            if (File.Exists("Assets/FrogSea/Runtime/View/JumpAimView.cs"))
                throw new System.InvalidOperationException("Trajectory guide must be removed.");
            if (Application.isPlaying)
            {
                if (Object.FindObjectsByType<MeshRenderer>().Length > 0 || Object.FindObjectsByType<LineRenderer>().Length > 0)
                    throw new System.InvalidOperationException("2D scenes must not contain 3D meshes or trajectory lines.");
                foreach (var camera in Object.FindObjectsByType<Camera>())
                    if (!camera.orthographic) throw new System.InvalidOperationException("Camera must be orthographic.");
                string name = SceneManager.GetActiveScene().name;
                if (name == GameSession.TitleScene && Object.FindAnyObjectByType<FrogSeaGame>() != null)
                    throw new System.InvalidOperationException("Gameplay must not run in the title.");
                if (name == GameSession.PlayScene && Object.FindAnyObjectByType<TitleController>() != null)
                    throw new System.InvalidOperationException("Title controller must not exist in gameplay.");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/FrogSea-2D-structure.txt", "PASS: Title -> InGame build order, separate scenes, trajectory script removed.\n" +
                (Application.isPlaying ? "PASS: active scene has only 2D rendering, orthographic cameras, and correct scene ownership.\n" : ""));
            Debug.Log("[FrogSea2D] Scene structure validation passed.");
        }
    }
}
