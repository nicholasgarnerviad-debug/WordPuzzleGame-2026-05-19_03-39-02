using NUnit.Framework;
using WordPuzzle.UI;
using WordPuzzle.Persistence;

namespace WordPuzzle.Tests.Unit.Engine
{
    /// <summary>
    /// EditMode tests for Task 7 (juice): haptics toggle (7B) and SFX mute/volume (7C).
    /// All assertions verified against:
    ///   - HandheldHaptics: each method returns early if !enabled(), else calls vibrate().
    ///   - SfxManager.EffectiveSfxVolume: 0 when null or muted, else Clamp01(master)*Clamp01(sfx).
    ///   - SettingsData defaults: reduceMotion=false, hapticsEnabled=true; Clone() preserves both.
    /// </summary>
    [TestFixture]
    public class JuiceFeedbackTests
    {
        // ----- ACCEPTANCE 7B: Haptics honors its toggle -----

        [Test]
        public void Haptics_Disabled_DoesNotVibrate()
        {
            int calls = 0;
            var h = new HandheldHaptics(() => false, () => calls++);
            h.LightTap();
            h.MediumTap();
            h.Buzz();
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Haptics_Enabled_Vibrates()
        {
            int calls = 0;
            var h = new HandheldHaptics(() => true, () => calls++);
            h.LightTap();
            h.MediumTap();
            h.Buzz();
            Assert.AreEqual(3, calls);
        }

        [Test]
        public void Haptics_TogglesLive()
        {
            int calls = 0;
            bool enabled = false;
            var h = new HandheldHaptics(() => enabled, () => calls++);

            // Disabled: no vibrate.
            h.LightTap();
            Assert.AreEqual(0, calls, "vibrate should not fire while disabled");

            // Enable live: vibrate fires.
            enabled = true;
            h.LightTap();
            h.MediumTap();
            Assert.AreEqual(2, calls, "vibrate should fire while enabled");

            // Disable live again: count frozen.
            enabled = false;
            h.Buzz();
            Assert.AreEqual(2, calls, "vibrate should stop firing once disabled again");
        }

        // ----- ACCEPTANCE 7C: SfxManager honors mute/volume -----

        [Test]
        public void Sfx_Muted_VolumeZero()
        {
            var s = new SettingsData { muted = true, masterVolume = 1f, sfxVolume = 1f };
            Assert.AreEqual(0f, SfxManager.EffectiveSfxVolume(s));
        }

        [Test]
        public void Sfx_Unmuted_VolumeIsMasterTimesSfx()
        {
            var s = new SettingsData { muted = false, masterVolume = 0.5f, sfxVolume = 0.5f };
            Assert.AreEqual(0.25f, SfxManager.EffectiveSfxVolume(s), 1e-4f);
        }

        [Test]
        public void Sfx_NullSettings_Zero()
        {
            Assert.AreEqual(0f, SfxManager.EffectiveSfxVolume(null));
        }

        // ----- SettingsData defaults + Clone fidelity for new fields -----

        [Test]
        public void SettingsData_Defaults_ReduceMotionFalse_HapticsEnabledTrue()
        {
            var s = new SettingsData();
            Assert.IsFalse(s.reduceMotion, "reduceMotion should default to false");
            Assert.IsTrue(s.hapticsEnabled, "hapticsEnabled should default to true");
        }

        [Test]
        public void SettingsData_Clone_PreservesReduceMotionAndHaptics()
        {
            var s = new SettingsData { reduceMotion = true, hapticsEnabled = true };
            var clone = s.Clone();
            Assert.IsTrue(clone.reduceMotion, "Clone should preserve reduceMotion");
            Assert.IsTrue(clone.hapticsEnabled, "Clone should preserve hapticsEnabled");
        }
    }
}
