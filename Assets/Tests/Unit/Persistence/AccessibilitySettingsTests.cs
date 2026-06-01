using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using WordPuzzle.Persistence;

// Task 9E (Persistence half) — accessibility settings round-trip through the REAL
// DataManager, plus graceful migration from a legacy settings_v1 JSON blob that
// lacks the new accessibility keys.
[TestFixture]
public class AccessibilitySettingsTests
{
    private const string SETTINGS_KEY = "settings_v1";

    [SetUp]
    public void Setup()
    {
        PlayerPrefs.DeleteKey(SETTINGS_KEY);
        PlayerPrefs.DeleteKey("Coins");
        PlayerPrefs.Save();
    }

    [TearDown]
    public void Teardown()
    {
        PlayerPrefs.DeleteKey(SETTINGS_KEY);
        PlayerPrefs.DeleteKey("Coins");
        PlayerPrefs.Save();
    }

    [Test]
    public async Task SettingsData_RoundTrips_AccessibilityFields()
    {
        var dm = new DataManager();

        var toSave = new SettingsData
        {
            colorBlindMode = ColorBlindMode.Deuteranopia,
            highContrast = true,
            textScale = 1.3f
        };
        await dm.SaveSettingsAsync(toSave);

        // Fresh DataManager forces a load from PlayerPrefs (no in-memory cache).
        var dm2 = new DataManager();
        var loaded = await dm2.LoadSettingsAsync();

        Assert.AreEqual(ColorBlindMode.Deuteranopia, loaded.colorBlindMode);
        Assert.IsTrue(loaded.highContrast);
        Assert.AreEqual(1.3f, loaded.textScale, 1e-3f);
    }

    [Test]
    public void LoadSettings_FromLegacyJson_MissingAccessibilityKeys_UsesSafeDefaults()
    {
        // Legacy blob: ONLY pre-9E fields present; no colorBlindMode/highContrast/textScale.
        // JsonUtility leaves absent fields at their C# initializer defaults.
        string legacyJson = "{\"masterVolume\":0.5,\"sfxVolume\":0.6,\"musicVolume\":0.4,\"muted\":false,\"version\":1}";
        PlayerPrefs.SetString(SETTINGS_KEY, legacyJson);
        PlayerPrefs.Save();

        var dm = new DataManager();
        SettingsData loaded = null;
        Assert.DoesNotThrowAsync(async () => { loaded = await dm.LoadSettingsAsync(); },
            "Loading a legacy settings blob must not throw.");

        Assert.IsNotNull(loaded);
        Assert.AreEqual(ColorBlindMode.Off, loaded.colorBlindMode, "Default colorBlindMode is Off.");
        Assert.IsFalse(loaded.highContrast, "Default highContrast is false.");
        Assert.AreEqual(1.0f, loaded.textScale, 1e-3f, "Default textScale is 1.0.");

        // And the legacy fields that WERE present are still honored.
        Assert.AreEqual(0.5f, loaded.masterVolume, 1e-3f);
    }
}
