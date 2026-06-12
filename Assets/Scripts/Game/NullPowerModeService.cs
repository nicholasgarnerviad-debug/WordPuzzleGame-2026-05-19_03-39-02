/// <summary>
/// Task 44 — the shipped no-op power-mode reader (mirrors <see cref="NullAdService"/>):
/// never reports low power, so the video backdrop behaves exactly as before on every
/// platform until a native implementation lands (§13 tech debt).
/// </summary>
public class NullPowerModeService : IPowerModeService
{
    public bool LowPowerActive => false;
}
