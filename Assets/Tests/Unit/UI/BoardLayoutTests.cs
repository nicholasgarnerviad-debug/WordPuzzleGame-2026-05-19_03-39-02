using NUnit.Framework;
using WordPuzzle.UI;

// ============================================================
//  Task 47 — the centred board zone. Pins the pure centring
//  math LadderLayoutDriver uses to place the OCCUPIED ladder
//  block in the live play zone (header bottom ↔ power-up bar
//  top): centred when it fits, pinned to the zone top when the
//  chain outgrows the zone, and safe on degenerate zones. The
//  visual result (no dead void under the TO row) is the human
//  Simulator gate.
// ============================================================
public class BoardLayoutTests
{
    [Test]
    public void CenteredBlockTop_CentresABlockThatFits()
    {
        // Zone from +700 down to -300 (height 1000); a 400-tall block.
        float top = LadderLayoutDriver.CenteredBlockTop(700f, -300f, 400f);
        // Block top at 400 ⇒ block spans 400..0 ⇒ its centre (200) == the zone centre (200).
        Assert.AreEqual(400f, top, 0.001f);
        float blockCentre = top - 200f;
        float zoneCentre = (700f + -300f) * 0.5f;
        Assert.AreEqual(zoneCentre, blockCentre, 0.001f);
    }

    [Test]
    public void CenteredBlockTop_PinsToZoneTop_WhenBlockOutgrowsTheZone()
    {
        Assert.AreEqual(700f, LadderLayoutDriver.CenteredBlockTop(700f, -300f, 1000f), 0.001f,
            "a block exactly the zone height starts at the top");
        Assert.AreEqual(700f, LadderLayoutDriver.CenteredBlockTop(700f, -300f, 1400f), 0.001f,
            "an overgrown block pins to the top (the chain cap absorbs the rest)");
    }

    [Test]
    public void CenteredBlockTop_DegenerateZone_FallsBackToTop()
    {
        Assert.AreEqual(500f, LadderLayoutDriver.CenteredBlockTop(500f, 500f, 100f), 0.001f,
            "zero-height zone — never divide into nonsense");
        Assert.AreEqual(500f, LadderLayoutDriver.CenteredBlockTop(500f, 900f, 100f), 0.001f,
            "inverted zone — same safe fallback");
    }

    [Test]
    public void CenteredBlockTop_TallerZoneCentresLower_NeverAboveTop()
    {
        // Monotonic sanity: growing the block raises its top edge but never past the zone top.
        float prev = float.NegativeInfinity;
        for (float h = 100f; h <= 1100f; h += 100f)
        {
            float top = LadderLayoutDriver.CenteredBlockTop(700f, -300f, h);
            Assert.GreaterOrEqual(top, prev, "a taller block never centres HIGHER-edge-lower");
            Assert.LessOrEqual(top, 700f + 0.001f, "the block top never escapes the zone");
            prev = top;
        }
    }
}
