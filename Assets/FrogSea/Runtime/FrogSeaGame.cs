using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FrogSea
{
    // References are authored in InGameRig. Restart replaces data, never scene objects.
    public sealed class FrogSeaGame : MonoBehaviour
    {
        [SerializeField] JumpSettings settings;
        [SerializeField] WellView world;
        [SerializeField] WellCameraView cameraView;
        [SerializeField] FrogView frogView;
        [SerializeField] GameHudView hud;
        [SerializeField] FeedbackView feedback;
        public FrogModel Frog { get; private set; }
        public StageModel Stage { get; private set; }
        public DragJumpInput Input { get; private set; }
        public bool Paused { get; private set; }
        public GameMode Mode => GameSession.Mode;
        public int StageNumber => GameSession.Stage;
        public float Record { get; private set; }
        public string Notice { get; private set; }
        public float NoticeTime { get; private set; }
        IDisposable wallSubscription, landSubscription, fallSubscription, goalSubscription;
        void Awake()
        {
            Application.targetFrameRate = 60; Record = PlayerPrefs.GetFloat("FrogSea.Record", 0);
            BuildRun(); hud.Bind(this);
        }
        public void Restart() { SaveRecord(); Paused = false; BuildRun(); }
        public void NextClimb() => StartRun(StageNumber < 3 ? GameMode.Stages : GameMode.Endless, StageNumber + 1);
        public void StartRun(GameMode mode, int stageNumber = 1) { SaveRecord(); GameSession.Start(mode, stageNumber); }
        public void OpenMenu() { Input.Cancel(); SaveRecord(); GameSession.ReturnToTitle(); }
        public void TogglePause()
        {
            if (Frog.State == FrogState.Finished) return;
            Paused = !Paused; Input.Cancel();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        void DisposeRun()
        {
            Input?.Dispose();
            wallSubscription?.Dispose(); landSubscription?.Dispose(); fallSubscription?.Dispose(); goalSubscription?.Dispose();
        }
        void BuildRun()
        {
            DisposeRun(); var events = new EventBus();
            Stage = world.Prepare(Mode, StageNumber, settings.circumference);
            Frog = new FrogModel(settings, Stage, events); Input = new DragJumpInput(settings);
            world.Bind(Frog); frogView.Initialize(Frog, events); feedback.Initialize(events);
            cameraView.Initialize(Frog, settings.circumference);
            wallSubscription = events.Subscribe<WallAttached>(_ => ShowNotice("MOSS GRIP  /  Pull and release to jump away"));
            landSubscription = events.Subscribe<Landed>(e => { if (e.Speed > 16) ShowNotice("A long fall. Keep climbing."); });
            fallSubscription = events.Subscribe<Fell>(_ => ShowNotice("Back at the bottom."));
            goalSubscription = events.Subscribe<GoalReached>(_ => { Input.Cancel(); world.RevealSea(); SaveRecord(); });
            Notice = ""; NoticeTime = 0; events.Publish(new RunStarted());
        }
        void ShowNotice(string message) { Notice = message; NoticeTime = 3; }
        void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame && !Input.Dragging) TogglePause();
                if (Keyboard.current.rKey.wasPressedThisFrame) Restart();
            }
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Input.Poll(Frog, Paused || Frog.State == FrogState.Finished, overUi);
            NoticeTime = Mathf.Max(0, NoticeTime - Time.deltaTime);
        }
        void FixedUpdate()
        {
            if (Paused) return;
            Frog.Step(Time.fixedDeltaTime);
            if (Mode == GameMode.Endless) Stage.ExtendTo(Frog.BestHeight + 16, settings.circumference);
        }
        void SaveRecord()
        {
            if (Frog == null || Frog.BestHeight <= Record) return;
            Record = Frog.BestHeight; PlayerPrefs.SetFloat("FrogSea.Record", Record); PlayerPrefs.Save();
        }
        void OnApplicationPause(bool paused) { if (paused) { SaveRecord(); Paused = true; Input?.Cancel(); } }
        void OnDestroy() { SaveRecord(); DisposeRun(); }
    }
}
