using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NestLabs.Player
{
    /// <summary>
    /// Reads a tap from the touchscreen, with a mouse fallback so the game is playable in the
    /// editor. Bindings are built in code rather than read from an .inputactions asset because the
    /// entire scheme is a single button — an asset would be more moving parts, not fewer.
    /// </summary>
    public sealed class TouchPlayerInput : IPlayerInput, IDisposable
    {
        private readonly InputAction _tapAction;

        private float _lastTapTime = float.NegativeInfinity;
        private bool _consumed = true;
        private int _lastTapFrame = -1;

        public bool Enabled { get; set; } = true;

        public TouchPlayerInput()
        {
            _tapAction = new InputAction("PlayerTap", InputActionType.Button);
            _tapAction.AddBinding("<Touchscreen>/primaryTouch/press");
            _tapAction.AddBinding("<Mouse>/leftButton");
            _tapAction.performed += OnTapPerformed;
            _tapAction.Enable();
        }

        private void OnTapPerformed(InputAction.CallbackContext _)
        {
            // A device that reports both touch and mouse for one physical press would otherwise
            // register two taps. One tap per frame, always.
            if (_lastTapFrame == Time.frameCount)
            {
                return;
            }

            _lastTapFrame = Time.frameCount;
            _lastTapTime = Time.time;
            _consumed = false;
        }

        public bool HasBufferedTap(float bufferDuration)
        {
            return Enabled && !_consumed && Time.time - _lastTapTime <= bufferDuration;
        }

        public void ConsumeTap()
        {
            _consumed = true;
        }

        public void ClearTap()
        {
            _consumed = true;
            _lastTapTime = float.NegativeInfinity;
        }

        public void Dispose()
        {
            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();
            _tapAction.Dispose();
        }
    }
}
