using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogSea.Editor
{
    // Editor integration check: exercises the real scenes and their UI callbacks.
    [InitializeOnLoad]
    public static class FrogSea2DSmokeTest
    {
        const string Running = "FrogSea2D.SmokeRunning";
        static int phase;
        static double entered, started;
        static FrogSea2DSmokeTest()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.delayCall += Attach;
        }
        [MenuItem("Frog See the Sea/Run 2D flow smoke test")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before starting the flow test.");
            EditorSceneManagerOpenInGameAndRecord();
            FrogSeaProjectSetup.CreateScene();
            TitleSceneAuthoring.ValidateAuthoredTitle();
            SessionState.SetInt("FrogSea.TitleObjectCount", TitleSceneAuthoring.ObjectCount());
            SessionState.SetBool(Running, true); phase = 0;
            EditorApplication.isPlaying = true; Attach();
        }
        static void Attach()
        {
            phase = 0; started = entered = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        static void EditorSceneManagerOpenInGameAndRecord()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
            InGameSceneAuthoring.ValidateScene();
            SessionState.SetInt("FrogSea.InGameObjectCount", InGameSceneAuthoring.ObjectCount());
        }
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("[FrogSea2D smoke] " + message); }
        static void Click(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>())
                if (button.gameObject.name == name + " button") { button.onClick.Invoke(); return; }
            throw new InvalidOperationException("Missing active UI button: " + name);
        }
        static void Advance(int next) { phase = next; entered = EditorApplication.timeSinceStartup; }
        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) { EditorApplication.update -= Tick; return; }
            if (!EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 70, "Scene flow timed out");
                double elapsed = EditorApplication.timeSinceStartup - entered;
                string scene = SceneManager.GetActiveScene().name;
                var game = UnityEngine.Object.FindAnyObjectByType<FrogSeaGame>();
                switch (phase)
                {
                    case 0:
                        if (scene != GameSession.TitleScene || elapsed < 1) return;
                        FrogSeaProjectSetup.ValidateProject();
                        TitleSceneAuthoring.ValidateAuthoredTitle();
                        Require(TitleSceneAuthoring.ObjectCount() == SessionState.GetInt("FrogSea.TitleObjectCount", -1), "Title must not create GameObjects on Play.");
                        Require(game == null, "Title must not instantiate gameplay.");
                        ScreenCapture.CaptureScreenshot("Logs/FrogSea2D-title.png");
                        Advance(10); break;
                    case 10:
                        if (elapsed < 0.2f) return;
                        Click("Start");
                        Require(GameObject.Find("Rule selection") != null, "Rule selection must be a title UI panel.");
                        Click("Stages"); Click(">"); Click("Climb"); Advance(1); break;
                    case 1:
                        if (scene != GameSession.PlayScene || game == null || elapsed < 1) return;
                        FrogSeaProjectSetup.ValidateProject();
                        FrogSeaValidation.Validate();
                        InGameSceneAuthoring.ValidateScene();
                        Require(InGameSceneAuthoring.ObjectCount() == SessionState.GetInt("FrogSea.InGameObjectCount", -1), "InGame must not construct scene objects on Play.");
                        Require(game.Mode == GameMode.Stages && game.StageNumber == 2, "Selected rule/stage must cross scenes.");
                        Require(game.Frog.Jump(new Vector2(-0.23f, 1), 1), "Initial jump rejected.");
                        Advance(2); break;
                    case 2:
                        if (elapsed < 1.5f) return;
                        Require(game.Frog.State == FrogState.Grounded && game.Frog.Position.y > 1, "2D jump must land on the first ledge.");
                        ScreenCapture.CaptureScreenshot("Logs/FrogSea2D-ingame.png");
                        Advance(20); break;
                    case 20:
                        if (elapsed < 0.2f) return;
                        game.TogglePause(); Require(game.Paused, "Pause failed.");
                        Advance(3); break;
                    case 3:
                        if (elapsed < 0.2f) return;
                        game.TogglePause(); Require(!game.Paused, "Resume failed.");
                        game.OpenMenu(); Advance(4); break;
                    case 4:
                        if (scene != GameSession.TitleScene || elapsed < 0.5f) return;
                        Require(game == null, "InGame objects leaked into Title.");
                        Click("Start"); Click("Endless"); Click("Climb"); Advance(5); break;
                    case 5:
                        if (scene != GameSession.PlayScene || game == null || elapsed < 0.5f) return;
                        Require(game.Mode == GameMode.Endless && float.IsPositiveInfinity(game.Stage.GoalHeight), "Endless selection failed.");
                        FrogSeaProjectSetup.ValidateProject();
                        game.TogglePause(); game.Restart();
                        Require(!game.Paused && game.Frog.JumpCount == 0, "Restart must reset the run and pause.");
                        Require(InGameSceneAuthoring.ObjectCount() == SessionState.GetInt("FrogSea.InGameObjectCount", -1), "Endless/restart must reuse authored objects.");
                        game.OpenMenu(); Advance(6); break;
                    case 6:
                        if (scene != GameSession.TitleScene || elapsed < 0.7f) return;
                        Directory.CreateDirectory("Logs");
                        File.WriteAllText("Logs/FrogSea2D-smoke.txt",
                            "PASS: Title home -> rule selection UI -> stage 2 InGame -> jump/landing -> pause/resume -> Title -> Endless InGame -> restart -> Title.\n" +
                            "PASS: orthographic cameras, SpriteRenderer scenery, no 3D meshes or jump guides, no gameplay objects in title.\n" +
                            "PASS: authored title and ingame object counts unchanged on Play/restart/endless; persistent prefab UI callbacks and saved sprite/input/audio assets.\n");
                        Debug.Log("[FrogSea2D] Scene flow smoke test PASS.");
                        Stop(); EditorApplication.isPlaying = false; break;
                }
            }
            catch (Exception error)
            {
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/FrogSea2D-smoke.txt", "FAIL: " + error);
                Debug.LogException(error); Stop(); EditorApplication.isPlaying = false;
            }
        }
        static void Stop() { SessionState.SetBool(Running, false); EditorApplication.update -= Tick; }
    }
}
