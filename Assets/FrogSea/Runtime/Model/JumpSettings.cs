using UnityEngine;

namespace FrogSea
{
    [CreateAssetMenu(menuName = "Frog See the Sea/Jump Settings")]
    public sealed class JumpSettings : ScriptableObject
    {
        [Header("Movement")]
        [Min(1)] public float circumference = 32;
        [Min(1)] public float gravity = 24;
        [Min(1)] public float minJumpSpeed = 5;
        [Min(1)] public float maxJumpSpeed = 14;
        [Range(0, 1)] public float airDrag = 0.12f;
        [Header("Drag input (fraction of shorter screen dimension)")]
        [Range(0.05f, 0.6f)] public float fullDrag = 0.24f;
        [Range(0.005f, 0.1f)] public float deadZone = 0.025f;
        [Header("Wall jump")]
        [Min(0.1f)] public float wallHoldSeconds = 2.2f;
        [Min(0.1f)] public float wallJumpMultiplier = 1.08f;
        [Min(0.1f)] public float minimumWallPush = 4.5f;
    }
}
