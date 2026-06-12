/// <summary>
/// Power-mode abstraction (Task 44). Placed in the Puzzle layer (no dependencies) so every
/// assembly — including tests — can reference it without circular deps, mirroring
/// <see cref="IAdService"/> / IStoreService.
///
/// When <see cref="LowPowerActive"/> is true the looping video backdrop resolves to the
/// still image instead (see UIThemeManager.ResolveBackdrop) — a session-long VideoPlayer
/// is a real battery cost on a session-heavy puzzle game. The shipped implementation is
/// the always-false <c>NullPowerModeService</c>; native iOS
/// (NSProcessInfo.lowPowerModeEnabled) / Android (PowerManager.isPowerSaveMode) readers
/// are §13 tech debt.
/// </summary>
public interface IPowerModeService
{
    /// <summary>True when the OS reports battery-saver / low-power mode.</summary>
    bool LowPowerActive { get; }
}
