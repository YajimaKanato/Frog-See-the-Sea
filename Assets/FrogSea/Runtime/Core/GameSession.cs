using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrogSea
{
    // Selection data only. Scene-owned gameplay objects never persist.
    public static class GameSession
    {
        public const string TitleScene = "Title";
        public const string PlayScene = "InGame";
        public static GameMode Mode { get; private set; } = GameMode.Stages;
        public static int Stage { get; private set; } = 1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Mode = GameMode.Stages; Stage = 1; }
        public static void Start(GameMode mode, int stage = 1)
        {
            Mode = mode; Stage = Mathf.Clamp(stage, 1, 3);
            SceneManager.LoadScene(PlayScene);
        }
        public static void ReturnToTitle() => SceneManager.LoadScene(TitleScene);
    }
}
