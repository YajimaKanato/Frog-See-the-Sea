using UnityEngine;

namespace FrogSea
{
    public enum FrogState { Grounded, Airborne, WallAttached, Finished }
    /// <summary>Deterministic fixed-step movement and one-way ledge/adhesive collision rules.</summary>
    public sealed class FrogModel
    {
        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public FrogState State { get; private set; } = FrogState.Grounded;
        public float BestHeight { get; private set; }
        public float WallTimeLeft { get; private set; }
        public int JumpCount { get; private set; }
        public int FallCount { get; private set; }
        public float Elapsed { get; private set; }
        public bool CanJump => State == FrogState.Grounded || State == FrogState.WallAttached;
        readonly JumpSettings settings;
        readonly StageModel stage;
        readonly EventBus events;
        float attachCooldown;
        float wallNormal = 1;
        public FrogModel(JumpSettings settings, StageModel stage, EventBus events, Vector2? spawn = null)
        { this.settings = settings; this.stage = stage; this.events = events; Position = spawn ?? Vector2.zero; }
        public Vector2 LaunchVelocity(Vector2 direction, float power)
        {
            direction.y = Mathf.Max(0.18f, direction.y);
            var velocity = direction.normalized * Mathf.Lerp(settings.minJumpSpeed, settings.maxJumpSpeed, Mathf.Clamp01(power));
            if (State == FrogState.WallAttached)
            {
                velocity *= settings.wallJumpMultiplier;
                velocity.x = wallNormal * Mathf.Max(settings.minimumWallPush, Mathf.Abs(velocity.x));
            }
            return velocity;
        }
        public bool Jump(Vector2 direction, float power)
        {
            if (!CanJump || direction.sqrMagnitude < 0.001f) return false;
            bool wall = State == FrogState.WallAttached;
            Velocity = LaunchVelocity(direction, power);
            State = FrogState.Airborne; attachCooldown = 0.28f; JumpCount++;
            events.Publish(new Jumped(wall, power)); return true;
        }
        public void Step(float dt)
        {
            if (State == FrogState.Finished) return;
            Elapsed += dt; attachCooldown -= dt;
            if (State == FrogState.WallAttached)
            {
                WallTimeLeft -= dt;
                if (WallTimeLeft > 0) return;
                State = FrogState.Airborne; attachCooldown = 0.45f;
                Velocity = new Vector2(wallNormal * 1.6f, -1);
            }
            if (State == FrogState.Grounded) return;
            Vector2 previous = Position;
            Velocity += Vector2.down * settings.gravity * dt;
            Velocity = new Vector2(Velocity.x / (1 + settings.airDrag * dt), Velocity.y);
            Position += Velocity * dt;
            BestHeight = Mathf.Max(BestHeight, Position.y);
            // Descending feet cross the top of a ledge, including the cylindrical seam.
            if (Velocity.y <= 0)
            {
                for (int i = stage.Ledges.Count - 1; i >= 0; i--)
                {
                    var ledge = stage.Ledges[i];
                    if (previous.y < ledge.Y || Position.y > ledge.Y) continue;
                    float t = Mathf.InverseLerp(previous.y, Position.y, ledge.Y);
                    float crossingX = Mathf.Lerp(previous.x, Position.x, t);
                    if (Mathf.Abs(StageModel.WrappedDelta(crossingX, ledge.X, settings.circumference)) > ledge.Width / 2 + 0.16f) continue;
                    float speed = -Velocity.y;
                    Position = new Vector2(crossingX, ledge.Y); Velocity = Vector2.zero;
                    State = FrogState.Grounded; events.Publish(new Landed(speed));
                    if (ledge.Y >= stage.GoalHeight)
                    { State = FrogState.Finished; events.Publish(new GoalReached()); }
                    break;
                }
            }
            // Moss columns hanging from marked ledges can be grabbed from either side.
            if (State == FrogState.Airborne && attachCooldown <= 0)
            {
                foreach (var ledge in stage.Ledges)
                {
                    if (!ledge.Sticky || Position.y < ledge.Y - 1.6f || Position.y > ledge.Y - 0.25f) continue;
                    float dx = StageModel.WrappedDelta(Position.x, ledge.X, settings.circumference);
                    float oldDx = StageModel.WrappedDelta(previous.x, ledge.X, settings.circumference);
                    const float contact = 0.47f;
                    bool crossed = oldDx * dx <= 0 || Mathf.Abs(dx) <= contact;
                    if (!crossed || Mathf.Abs(oldDx) > contact + Mathf.Abs(Velocity.x * dt)) continue;
                    wallNormal = oldDx >= 0 ? 1 : -1;
                    Position = new Vector2(Position.x + wallNormal * contact - dx, Position.y);
                    Velocity = Vector2.zero; State = FrogState.WallAttached;
                    WallTimeLeft = settings.wallHoldSeconds; events.Publish(new WallAttached()); break;
                }
            }
            if (Position.y < -3)
            {
                Position = Vector2.zero; Velocity = Vector2.zero; State = FrogState.Grounded;
                FallCount++; events.Publish(new Fell());
            }
        }
    }
}
