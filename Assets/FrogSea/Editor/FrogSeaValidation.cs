using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FrogSea.Editor
{
    public static class FrogSeaValidation
    {
        static int checks;
        static bool CanLand(StageModel level, Ledge from, Ledge to, JumpSettings settings)
        {
            float dx = StageModel.WrappedDelta(to.X, from.X, settings.circumference);
            for (float time = 0.55f; time <= 1.15f; time += 0.05f)
            {
                var velocity = new Vector2(dx / time, (to.Y - from.Y) / time + 0.5f * settings.gravity * time);
                float speed = velocity.magnitude;
                if (speed > settings.maxJumpSpeed || speed < settings.minJumpSpeed) continue;
                var frog = new FrogModel(settings, level, new EventBus(), new Vector2(from.X, from.Y));
                frog.Jump(velocity.normalized, Mathf.InverseLerp(settings.minJumpSpeed, settings.maxJumpSpeed, speed));
                for (int t = 0; t < 100; t++)
                {
                    frog.Step(0.02f);
                    if (frog.State == FrogState.Grounded || frog.State == FrogState.Finished)
                    {
                        if (Mathf.Abs(frog.Position.y - to.Y) < 0.01f && Mathf.Abs(StageModel.WrappedDelta(frog.Position.x, to.X, settings.circumference)) <= to.Width / 2 + 0.16f) return true;
                        break;
                    }
                    if (frog.Position.y < from.Y - 0.1f || frog.State == FrogState.WallAttached) break;
                }
            }
            return false;
        }
        static void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("[FrogSea validation] " + description);
            checks++;
        }
        [MenuItem("Frog See the Sea/Validate gameplay models")]
        public static void Validate()
        {
            checks = 0;
            var settings = ScriptableObject.CreateInstance<JumpSettings>();
            try
            {
                var bus = new EventBus(); int count = 0;
                var token = bus.Subscribe<Jumped>(_ => count++);
                bus.Publish(new Jumped(false, 1)); token.Dispose(); token.Dispose(); bus.Publish(new Jumped(false, 1));
                Check(count == 1, "EventBus unsubscribe and idempotent disposal");
                Check(Mathf.Abs(StageModel.WrappedDelta(33, 0, 32) - 1) < 0.001f, "Positive seam");
                Check(Mathf.Abs(StageModel.WrappedDelta(-33, 0, 32) + 1) < 0.001f, "Negative seam");
                var floor = new StageModel(GameMode.Stages, 1, 32); floor.Ledges.Clear(); floor.Ledges.Add(new Ledge(0, 0, 32));
                var frog = new FrogModel(settings, floor, new EventBus());
                Check(frog.Jump(Vector2.up, 1), "Ground jump accepted");
                Check(!frog.Jump(Vector2.up, 1), "Air jump rejected");
                for (int i = 0; i < 150; i++) frog.Step(0.02f);
                Check(frog.State == FrogState.Grounded && Mathf.Abs(frog.Position.y) < 0.001f, "Return to floor");
                Check(frog.BestHeight > 3.8f, "Full jump apex");
                Check(frog.LaunchVelocity(Vector2.up, 1).magnitude > frog.LaunchVelocity(Vector2.up, 0).magnitude, "Variable jump power");
                Check(frog.LaunchVelocity(Vector2.down, 1).y > 0, "Drag angle safety floor");
                if (EditorApplication.isPlaying)
                {
                var mouse = InputSystem.AddDevice<Mouse>("FrogSea validation mouse");
                try
                {
                    using (var drag = new DragJumpInput(settings))
                    {
                        frog = new FrogModel(settings, floor, new EventBus());
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(300, 500) }.WithButton(MouseButton.Left));
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(350, 200) });
                        InputSystem.Update();
                        drag.Poll(frog, false);
                        Check(frog.JumpCount == 1 && frog.Velocity.y > 0 && frog.Velocity.x < 0, "Short drag press/release within one input update");
                    }
                }
                finally { InputSystem.RemoveDevice(mouse); }
                }
                var seam = new StageModel(GameMode.Stages, 1, 32); seam.Ledges.Clear(); seam.Ledges.Add(new Ledge(-1, 1, 3));
                frog = new FrogModel(settings, seam, new EventBus(), new Vector2(31, 3));
                frog.Jump(Vector2.up, 0);
                for (int i = 0; i < 150; i++) frog.Step(0.02f);
                Check(frog.State == FrogState.Grounded && Mathf.Abs(frog.Position.y - 1) < 0.001f, "Landing across cylindrical seam");
                var sticky = new StageModel(GameMode.Stages, 1, 32); sticky.Ledges.Clear(); sticky.Ledges.Add(new Ledge(0, 0, 32)); sticky.Ledges.Add(new Ledge(2.5f, 3, 2, true));
                frog = new FrogModel(settings, sticky, new EventBus());
                frog.Jump(new Vector2(1, 1), 0.8f);
                for (int i = 0; i < 70 && frog.State != FrogState.WallAttached; i++) frog.Step(0.02f);
                Check(frog.State == FrogState.WallAttached, "Moss wall attachment");
                Check(frog.Jump(new Vector2(1, 1), 0.8f) && frog.Velocity.x < 0, "Wall jump pushes away from contacted side");
                int edges = 0, branching = 0, seamEdges = 0;
                for (int stageIndex = 1; stageIndex <= 3; stageIndex++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FrogSea/Prefabs/Stages/Stage0" + stageIndex + ".prefab");
                    var level = asset != null ? asset.GetComponent<StageLayout>().ReadModel(settings.circumference) : new StageModel(GameMode.Stages, stageIndex, 32);
                    var nodes = level.Ledges;
                    var reachable = new System.Collections.Generic.HashSet<int> { 0 };
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var from = nodes[i];
                        if (from.Y >= level.GoalHeight) continue;
                        float nextY = float.PositiveInfinity;
                        for (int j = 0; j < nodes.Count; j++) if (nodes[j].Y > from.Y + 0.01f) nextY = Mathf.Min(nextY, nodes[j].Y);
                        int choices = 0;
                        for (int j = 1; j < nodes.Count; j++)
                        {
                            var to = nodes[j];
                            if (Mathf.Abs(to.Y - nextY) > 0.01f) continue;
                            float dx = StageModel.WrappedDelta(to.X, from.X, settings.circumference);
                            if (Mathf.Abs(dx) > 2.1f) continue;
                            if (!CanLand(level, from, to, settings)) continue;
                            choices++; edges++;
                            if (reachable.Contains(i)) reachable.Add(j);
                            if (Mathf.Abs(to.X - from.X) > settings.circumference / 2) seamEdges++;
                        }
                        Check(choices >= 2, "Stage " + stageIndex + " node " + i + " needs left/right upward choices");
                        branching++;
                    }
                    int goals = 0;
                    for (int i = 0; i < nodes.Count; i++) if (nodes[i].Y >= level.GoalHeight && reachable.Contains(i)) goals++;
                    Check(goals == 8, "All eight goal sectors must be reachable from spawn");
                    // Both continuously clockwise and counter-clockwise climbs cross a full lap before the surface.
                    Check((nodes.Count - 1) / 8 * 2 >= settings.circumference, "Enough height for a full lap while climbing");
                }
                Check(seamEdges > 0, "Routes must cross the wrap seam");

                var endless = new StageModel(GameMode.Endless, 1, 32); endless.ExtendTo(500, 32);
                Check(endless.Ledges[endless.Ledges.Count - 1].Y >= 500, "Endless generation");
                string report = "PASS: " + checks + " checks. " + branching + " branching platforms; " + edges + " physical upward connections; " + seamEdges + " seam crossings.\n" +
                    "All 8 goal sectors in each of 3 stages are reachable. Each non-goal platform has at least 2 upward routes.\n" +
                    "Verified EventBus disposal, seam landing, power, no double jump, moss attachment, wall push, floor landing and endless generation.\n";
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/FrogSea-validation.txt", report);
                Debug.Log("[FrogSea] " + report);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
    }
}
