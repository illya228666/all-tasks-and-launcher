using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Rendering;
using Launcher.Pet.Speech;

namespace Launcher.Pet;

internal sealed class PetController : IDisposable
{
    private readonly Form _window;
    private readonly ScrollableControl _viewport;
    private readonly Random _random = new();
    private readonly PetState _state = new();
    private readonly PetRenderer _renderer;
    private readonly PetSpeechController _speech;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly System.Windows.Forms.Timer _movementTimer = new();
    private readonly System.Windows.Forms.Timer _jumpTimer = new();
    private readonly System.Windows.Forms.Timer _cursorTimer;
    private bool _started;
    private bool _disposed;

    internal PetController(Form window, ScrollableControl viewport, Action<string> hintRequested)
    {
        _window = window;
        _viewport = viewport;
        _renderer = new PetRenderer(window, _state, hintRequested);
        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.IdleRow][0]
        };
        _cursorTimer = new System.Windows.Forms.Timer { Interval = PetAnimationCatalog.CursorPollIntervalMs };

        _animationTimer.Tick += AnimationTimer_Tick;
        _movementTimer.Tick += MovementTimer_Tick;
        _jumpTimer.Tick += JumpTimer_Tick;
        _cursorTimer.Tick += CursorTimer_Tick;
        _speech = new PetSpeechController(
            _random,
            () => _state.Mode == PetMode.TrackingCursor,
            () => _state.Mode is PetMode.Idle or PetMode.Waving,
            _renderer.GetSpeechHead,
            () => _state.JumpPending || _state.MovementPending,
            TryStartPendingAction,
            hintRequested);

        _window.LocationChanged += HostPlacementChanged;
        _window.VisibleChanged += HostPlacementChanged;
        _window.Disposed += Window_Disposed;
        _viewport.Scroll += Viewport_Scroll;
    }

    internal int RequiredHostWidth => _renderer.RequiredHostWidth;
    internal int RequiredHostHeight => _renderer.RequiredHostHeight;

    internal void AttachHost(Panel panel, int groundY) => _renderer.AttachHost(panel, groundY);

    internal void BeginHostChange(bool preserveSpeech) => _speech.BeginHostChange(preserveSpeech);

    internal void EndHostChange() => _speech.EndHostChange();

    internal void ApplyTheme(PetTheme theme)
    {
        _renderer.ApplyTheme(theme);
        _speech.ApplyTheme(theme);
    }

    internal void Start()
    {
        if (_started || _disposed)
            return;
        _started = true;

        _animationTimer.Start();
        ScheduleNextJump();
        ScheduleNextMovement();
        _cursorTimer.Start();
        UpdateCursorTracking();
        _speech.Start();
    }

    internal void Stop()
    {
        if (!_started)
            return;
        _started = false;

        _animationTimer.Stop();
        _movementTimer.Stop();
        _jumpTimer.Stop();
        _cursorTimer.Stop();
        _speech.Stop();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_state.Mode == PetMode.TrackingCursor)
            return;
        if (_state.Mode == PetMode.Jumping)
        {
            AdvanceJump();
            return;
        }

        int[] durations = PetAnimationCatalog.FrameDurationsByRow[_state.Row];
        bool isMoving = _state.Mode == PetMode.Moving;
        if (isMoving)
        {
            _state.MoveElapsedMs += durations[_state.Frame];
            float progress = Math.Min(1f, (float)_state.MoveElapsedMs / _state.MoveDurationMs);
            _state.X = _state.MoveStartX + ((_state.MoveTargetX - _state.MoveStartX) * progress);
        }
        else if (_state.Mode == PetMode.Idle)
        {
            _state.IdleElapsedMs += durations[_state.Frame];
        }

        _state.Frame++;
        if (_state.Frame >= durations.Length)
        {
            _state.Frame = 0;
            if (_state.Mode == PetMode.Waving && --_state.WaveLoopsRemaining == 0)
            {
                _state.Mode = PetMode.Idle;
                _state.Row = PetAnimationCatalog.IdleRow;
            }
        }

        if (isMoving && _state.MoveElapsedMs >= _state.MoveDurationMs)
        {
            _state.X = _state.MoveTargetX;
            _renderer.ClampPosition();
            _state.Mode = PetMode.Idle;
            _state.Row = PetAnimationCatalog.IdleRow;
            _state.Frame = 0;
            _state.MoveElapsedMs = 0;
            ScheduleNextMovement();
        }

        if (TryStartPendingAction())
            return;

        if (_state.Mode == PetMode.Idle && _state.IdleElapsedMs >= PetAnimationCatalog.WaveIntervalMs)
        {
            _state.Mode = PetMode.Waving;
            _state.Row = PetAnimationCatalog.WaveRow;
            _state.Frame = 0;
            _state.IdleElapsedMs = 0;
            _state.WaveLoopsRemaining = PetAnimationCatalog.WaveLoopCount;
        }

        durations = PetAnimationCatalog.FrameDurationsByRow[_state.Row];
        _animationTimer.Interval = durations[_state.Frame];
        _renderer.Invalidate();
    }

    private void JumpTimer_Tick(object? sender, EventArgs e)
    {
        _jumpTimer.Stop();
        if (_state.Mode == PetMode.TrackingCursor)
            return;
        if (_speech.IsSpeaking)
        {
            _state.JumpPending = true;
            return;
        }
        if (_state.Mode == PetMode.Moving)
        {
            ScheduleNextJump();
            return;
        }
        if (_state.Mode != PetMode.Idle)
        {
            _state.JumpPending = true;
            return;
        }
        StartJump();
    }

    private void StartJump()
    {
        bool failed = _renderer.IsBelowBottomCardRow();
        _jumpTimer.Stop();
        _state.JumpPending = false;
        _state.Mode = PetMode.Jumping;
        _state.JumpSequence = failed
            ? PetAnimationCatalog.FailedJumpFrames
            : PetAnimationCatalog.SuccessfulJumpFrames;
        _state.JumpIndex = 0;
        _state.JumpPeak = PetAnimationCatalog.FrameHeight / (failed ? 4f : 3f);
        _state.IdleElapsedMs = 0;
        ApplyCurrentJumpFrame();
    }

    private void AdvanceJump()
    {
        _state.JumpIndex++;
        if (_state.JumpSequence is null || _state.JumpIndex >= _state.JumpSequence.Length)
        {
            _state.JumpSequence = null;
            _state.JumpIndex = 0;
            _state.Mode = PetMode.Idle;
            _state.Row = PetAnimationCatalog.IdleRow;
            _state.Frame = 0;
            _state.IdleElapsedMs = 0;
            _animationTimer.Interval = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.IdleRow][0];
            ScheduleNextJump();
            TryStartPendingAction();
            _renderer.Invalidate();
            return;
        }
        ApplyCurrentJumpFrame();
    }

    private void ApplyCurrentJumpFrame()
    {
        PetJumpFrame current = _state.JumpSequence![_state.JumpIndex];
        _state.Row = current.Row;
        _state.Frame = current.Frame;
        _animationTimer.Interval = PetAnimationCatalog.FrameDurationsByRow[current.Row][current.Frame];
        _renderer.Invalidate();
    }

    private void ScheduleNextJump()
    {
        _state.JumpPending = false;
        _jumpTimer.Stop();
        if (_state.Mode == PetMode.TrackingCursor)
            return;
        _jumpTimer.Interval = _random.Next(
            PetAnimationCatalog.JumpMinDelayMs, PetAnimationCatalog.JumpMaxDelayMs + 1);
        _jumpTimer.Start();
    }

    private void MovementTimer_Tick(object? sender, EventArgs e)
    {
        _movementTimer.Stop();
        if (_state.Mode == PetMode.TrackingCursor)
            return;
        if (_speech.IsSpeaking || _state.JumpPending || _state.MovementPending)
        {
            _state.MovementPending = true;
            TryStartPendingAction();
            return;
        }
        if (_state.Mode != PetMode.Idle)
        {
            ScheduleNextMovement();
            return;
        }
        StartMovement();
    }

    private bool StartMovement()
    {
        _state.MovementPending = false;
        _movementTimer.Stop();
        _renderer.ClampPosition();

        float minX = PetAnimationCatalog.EdgePadding;
        float maxX = Math.Max(minX,
            _renderer.ClientWidth - PetAnimationCatalog.FrameWidth - PetAnimationCatalog.EdgePadding);
        float minDistance = _window.ClientSize.Width / 8f;
        float availableLeft = _state.X - minX;
        float availableRight = maxX - _state.X;
        bool canMoveLeft = availableLeft >= minDistance;
        bool canMoveRight = availableRight >= minDistance;
        if (!canMoveLeft && !canMoveRight)
        {
            ScheduleNextMovement();
            return false;
        }

        bool moveRight = canMoveRight && (!canMoveLeft || _random.Next(2) == 0);
        float available = moveRight ? availableRight : availableLeft;
        float distance = minDistance + ((float)_random.NextDouble() * (available - minDistance));
        _state.MoveStartX = _state.X;
        _state.MoveTargetX = _state.X + (moveRight ? distance : -distance);
        int movementCycleMs = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.MoveRightRow].Sum();
        int movementCycles = Math.Max(1,
            (int)Math.Round(distance / PetAnimationCatalog.PixelsPerMovementCycle));
        _state.MoveDurationMs = movementCycles * movementCycleMs;
        _state.MoveElapsedMs = 0;
        _state.IdleElapsedMs = 0;
        _state.Mode = PetMode.Moving;
        _state.Row = moveRight ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow;
        _state.Frame = 0;

        System.Diagnostics.Debug.Assert(distance >= minDistance && distance <= available);
        _animationTimer.Interval = PetAnimationCatalog.FrameDurationsByRow[_state.Row][0];
        _renderer.Invalidate();
        return true;
    }

    private void ScheduleNextMovement()
    {
        _movementTimer.Stop();
        if (_state.Mode == PetMode.TrackingCursor)
            return;
        _movementTimer.Interval = _random.Next(
            PetAnimationCatalog.MovementMinDelayMs, PetAnimationCatalog.MovementMaxDelayMs + 1);
        _movementTimer.Start();
    }

    private void CursorTimer_Tick(object? sender, EventArgs e) => UpdateCursorTracking();

    private void UpdateCursorTracking()
    {
        if (!_window.IsHandleCreated || !_renderer.IsReady)
            return;

        Point? center = _renderer.GetCenterScreen();
        if (center is null)
            return;
        Point cursor = Cursor.Position;
        int deltaX = cursor.X - center.Value.X;
        int deltaY = cursor.Y - center.Value.Y;
        long distanceSquared = ((long)deltaX * deltaX) + ((long)deltaY * deltaY);
        int previousLookIndex = _state.LookIndex;
        if (distanceSquared > PetAnimationCatalog.LookDeadzoneRadius * PetAnimationCatalog.LookDeadzoneRadius)
            _state.LookIndex = PetAnimationCatalog.GetLookIndex(deltaX, deltaY);

        bool cursorInside = _window.WindowState != FormWindowState.Minimized
            && _window.RectangleToScreen(_window.ClientRectangle).Contains(cursor);
        if (cursorInside && _state.Mode != PetMode.TrackingCursor)
            StartCursorTracking();
        else if (!cursorInside && _state.Mode == PetMode.TrackingCursor)
            StopCursorTracking();
        else if (_state.Mode == PetMode.TrackingCursor && _state.LookIndex != previousLookIndex)
            ApplyLookFrame();
    }

    private void StartCursorTracking()
    {
        _state.Mode = PetMode.TrackingCursor;
        _speech.Reset();
        _animationTimer.Stop();
        _movementTimer.Stop();
        _jumpTimer.Stop();
        _state.JumpSequence = null;
        _state.JumpIndex = 0;
        _state.JumpPending = false;
        _state.MovementPending = false;
        _state.MoveElapsedMs = 0;
        _state.IdleElapsedMs = 0;
        _state.WaveLoopsRemaining = 0;
        ApplyLookFrame();
    }

    private void StopCursorTracking()
    {
        _speech.Reset();
        _state.Mode = PetMode.Idle;
        _state.Row = PetAnimationCatalog.IdleRow;
        _state.Frame = 0;
        _state.IdleElapsedMs = 0;
        _animationTimer.Interval = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.IdleRow][0];
        _animationTimer.Start();
        ScheduleNextMovement();
        ScheduleNextJump();
        _renderer.Invalidate();
    }

    private void ApplyLookFrame()
    {
        _state.Row = PetAnimationCatalog.LookFirstRow + (_state.LookIndex / 8);
        _state.Frame = _state.LookIndex % 8;
        _renderer.Invalidate();
    }

    private bool TryStartPendingAction()
    {
        if (_speech.IsSpeaking || _state.Mode != PetMode.Idle)
            return false;
        if (_state.JumpPending)
        {
            StartJump();
            return true;
        }
        return _state.MovementPending && StartMovement();
    }

    private void HostPlacementChanged(object? sender, EventArgs e) => _speech.UpdatePlacement();

    private void Viewport_Scroll(object? sender, ScrollEventArgs e) => _speech.UpdatePlacement();

    private void Window_Disposed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();

        _window.LocationChanged -= HostPlacementChanged;
        _window.VisibleChanged -= HostPlacementChanged;
        _window.Disposed -= Window_Disposed;
        _viewport.Scroll -= Viewport_Scroll;
        _animationTimer.Tick -= AnimationTimer_Tick;
        _movementTimer.Tick -= MovementTimer_Tick;
        _jumpTimer.Tick -= JumpTimer_Tick;
        _cursorTimer.Tick -= CursorTimer_Tick;
        _animationTimer.Dispose();
        _movementTimer.Dispose();
        _jumpTimer.Dispose();
        _cursorTimer.Dispose();
        _speech.Dispose();
        _renderer.Dispose();
    }
}
