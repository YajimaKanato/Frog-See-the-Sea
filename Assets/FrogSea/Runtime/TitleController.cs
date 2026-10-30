using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogSea
{
    // All visuals, camera, EventSystem and buttons are authored in Title.unity/prefabs.
    public sealed class TitleController : MonoBehaviour
    {
        [SerializeField] TitleView view;
        readonly TitleModel model = new TitleModel();
        void Awake()
        {
            if (view == null)
            {
                Debug.LogError("TitleView is not assigned. Assign the scene/prefab reference.", this);
                enabled = false; return;
            }
            Render();
        }
        public void OpenRules() { model.ShowRules(true); Render(); }
        public void Back() { model.ShowRules(false); Render(); }
        public void SelectStages() { model.Select(GameMode.Stages); Render(); }
        public void SelectEndless() { model.Select(GameMode.Endless); Render(); }
        public void PreviousStage() { model.ChangeStage(-1); Render(); }
        public void NextStage() { model.ChangeStage(1); Render(); }
        public void StartGame() => GameSession.Start(model.Mode, model.Stage);
        void Render() => view.Render(model);
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && model.ChoosingRules) Back();
        }
    }
}
