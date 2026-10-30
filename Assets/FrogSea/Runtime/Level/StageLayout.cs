using System.Collections.Generic;
using UnityEngine;

namespace FrogSea
{
    public sealed class StageLayout : MonoBehaviour
    {
        [SerializeField, Min(1)] int stageNumber = 1;
        [SerializeField, Min(1)] float goalHeight = 42;
        [SerializeField] PlatformDefinition[] platforms;
        public IReadOnlyList<PlatformDefinition> Platforms => platforms;
        public StageModel ReadModel(float circumference)
        {
            // Includes prefab instances added/duplicated in the editor without manual array maintenance.
            platforms = GetComponentsInChildren<PlatformDefinition>(true);
            var ledges = new List<Ledge>();
            foreach (var platform in platforms) ledges.Add(platform.Data);
            return new StageModel(stageNumber, circumference, goalHeight, ledges);
        }
        public void ResetDisplay()
        {
            platforms = GetComponentsInChildren<PlatformDefinition>(true);
            foreach (var platform in platforms) platform.ResetDisplay();
        }
    }
}
