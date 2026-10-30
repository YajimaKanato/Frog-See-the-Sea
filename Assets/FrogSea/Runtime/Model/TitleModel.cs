using UnityEngine;

namespace FrogSea
{
    public sealed class TitleModel
    {
        public bool ChoosingRules { get; private set; }
        public GameMode Mode { get; private set; } = GameMode.Stages;
        public int Stage { get; private set; } = 1;
        public void ShowRules(bool show) => ChoosingRules = show;
        public void Select(GameMode mode) => Mode = mode;
        public void ChangeStage(int delta) => Stage = Mathf.Clamp(Stage + delta, 1, 3);
    }
}
