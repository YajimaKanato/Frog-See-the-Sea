using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogSea
{
    /// <summary>Input adapter. Drag anywhere outside HUD; release the slingshot to jump.</summary>
    public sealed class DragJumpInput : System.IDisposable
    {
        public bool Dragging { get; private set; }
        public Vector2 Start { get; private set; }
        public Vector2 Current { get; private set; }
        public Vector2 Direction => Start - Current;
        public float Power { get; private set; }
        readonly JumpSettings settings;
        readonly InputAction press;
        Vector2 pressedAt, releasedAt;
        bool pressed, released;
        public DragJumpInput(JumpSettings settings)
        {
            this.settings = settings;
            press = new InputAction("Drag jump", InputActionType.Button, "<Pointer>/press");
            press.performed += _ => { pressedAt = Pointer.current.position.ReadValue(); pressed = true; };
            press.canceled += _ => { releasedAt = Pointer.current.position.ReadValue(); released = true; };
            press.Enable();
        }
        public void Cancel() { Dragging = false; Power = 0; pressed = released = false; }
        public void Dispose() { Cancel(); press.Dispose(); }
        public void Poll(FrogModel model, bool blocked, bool pointerOverUi = false)
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 position = pointer.position.ReadValue();
            float dimension = Mathf.Min(Screen.width, Screen.height);
            bool began = pressed, ended = released;
            pressed = released = false;
            if (began && model.CanJump && !blocked && !pointerOverUi)
            { Dragging = true; Start = Current = pressedAt; }
            if (!Dragging) return;
            if (!model.CanJump || blocked) { Cancel(); return; }
            Current = ended ? releasedAt : position;
            Power = Mathf.Clamp01(Direction.magnitude / (dimension * settings.fullDrag));
            if (ended)
            {
                if (Direction.magnitude >= dimension * settings.deadZone) model.Jump(Direction, Power);
                Cancel();
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
        }
    }
}
