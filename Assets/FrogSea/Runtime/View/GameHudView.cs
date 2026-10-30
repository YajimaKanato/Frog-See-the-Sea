using UnityEngine;
using UnityEngine.UI;

namespace FrogSea
{
    // Scene-authored HUD, dialogs and persistent button callbacks.
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] FrogSeaGame game;
        [SerializeField] Text altitude;
        [SerializeField] Text details;
        [SerializeField] Text state;
        [SerializeField] Text notice;
        [SerializeField] Text result;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject finishPanel;
        public void Bind(FrogSeaGame game) { this.game = game; Refresh(); }
        public void TogglePause() => game.TogglePause();
        public void Restart() => game.Restart();
        public void ReturnToTitle() => game.OpenMenu();
        public void NextClimb() => game.NextClimb();
        void Update() => Refresh();
        void Refresh()
        {
            if (game == null || game.Frog == null) return;
            altitude.text = Mathf.Max(0, game.Frog.Position.y).ToString("0.0") + " m";
            details.text = (game.Mode == GameMode.Endless ? "ENDLESS" : "STAGE " + game.StageNumber + " / " + game.Stage.GoalHeight + " m") +
                "     /     " + game.Frog.JumpCount + " jumps";
            state.text = game.Frog.State == FrogState.WallAttached ? "MOSS GRIP   " + game.Frog.WallTimeLeft.ToString("0.0") + " s" :
                "Drag back and release to jump.     /     R  Restart     /     ESC  Pause";
            notice.text = game.NoticeTime > 0 ? game.Notice : "";
            pausePanel.SetActive(game.Paused); bool clear = game.Frog.State == FrogState.Finished;
            finishPanel.SetActive(clear);
            if (clear) result.text = "Stage " + game.StageNumber + " cleared\n" + game.Frog.JumpCount + " jumps / " + game.Frog.Elapsed.ToString("0.0") + " seconds";
        }
    }
}
