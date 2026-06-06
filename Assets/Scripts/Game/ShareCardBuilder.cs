using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Pure builder for the shareable result summary. Mirrors Wordle's emoji grid:
    /// each chain step is one row of L squares; the position that CHANGED from the
    /// previous word is marked with 🟩, the others with ⬛.
    /// Used by ResultsScreen's Share button and by the (optional, gated) PNG path.
    /// </summary>
    public static class ShareCardBuilder
    {
        public const string CHANGED_GLYPH    = "\U0001F7E9"; // 🟩 optimal move
        public const string UNCHANGED_GLYPH  = "⬛";     // ⬛ mistake (also "unchanged" in the classic grid)
        public const string DETOUR_GLYPH     = "\U0001F7E8"; // 🟨 detour

        public enum ModeKind { Classic, Daily, PuzzleShow, TimeAttack }

        public sealed class ShareInput
        {
            public ModeKind mode;
            public string startWord;
            public string endWord;
            public List<string> chain;        // includes startWord at index 0
            public int? dailyIndex;           // 0-based; rendered as Daily #(idx+1)
            public int? puzzleShowTier;       // 1-based tier for PuzzleShow
            public int? timeAttackBaseSeconds; // 60 or 120; null otherwise
            public bool timeAttackSurvival;
            public float totalTimeSeconds;    // only shown for timed modes
            public int? streakCurrent;
            public int? streakBest;

            // Daily 2.0 (Task 36 Phase 4) — path-shape card. When dailyStepClasses is non-null the
            // daily renders SHAPE-ONLY (one glyph row per entry: 0=🟩 optimal, 1=🟨 detour, 2=⬛ mistake)
            // and NEVER the words. par/playerSteps/stars/dailyFailed drive the header.
            public List<int> dailyStepClasses;
            public int? par;
            public int? playerSteps;
            public int? stars;
            public bool dailyFailed;
        }

        public static string Build(ShareInput input)
        {
            if (input == null) return string.Empty;

            // Daily 2.0 (Task 36) — a daily with recorded step classes renders the SHAPE-ONLY card.
            if (input.mode == ModeKind.Daily && input.dailyStepClasses != null)
                return BuildDailyShapeCard(input);

            var sb = new StringBuilder(256);

            sb.Append("Word Ladder — ");
            sb.AppendLine(ModeLabel(input));

            int steps = (input.chain != null && input.chain.Count > 1)
                ? input.chain.Count - 1
                : 0;

            sb.Append(Up(input.startWord))
              .Append(" → ")
              .Append(Up(input.endWord))
              .Append("  •  ")
              .Append(steps)
              .Append(" step")
              .Append(steps == 1 ? "" : "s");

            if (IsTimedMode(input.mode) && input.totalTimeSeconds > 0f)
            {
                sb.Append("  •  ").Append(FormatTime(input.totalTimeSeconds));
            }
            sb.AppendLine();

            // Grid: one row per accepted chain step (indices 1..N-1).
            if (input.chain != null && input.chain.Count >= 2)
            {
                for (int i = 1; i < input.chain.Count; i++)
                {
                    sb.AppendLine(BuildRow(input.chain[i - 1], input.chain[i]));
                }
            }

            // Daily-only streak footer.
            if (input.mode == ModeKind.Daily
                && input.streakCurrent.HasValue
                && input.streakBest.HasValue)
            {
                sb.Append("\U0001F525 Streak ")    // 🔥
                  .Append(input.streakCurrent.Value)
                  .Append(" · Best ")
                  .Append(input.streakBest.Value);
            }

            return sb.ToString().Replace("\r\n", "\n").TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Daily 2.0 (Task 36) — the path-shape share card: a header, then one glyph row per recorded
        /// step (🟩 optimal / 🟨 detour / ⬛ mistake), then the streak line. SHAPE ONLY — never the
        /// words, so it can be pasted into chat without spoiling the puzzle. LF newlines for clean paste.
        /// </summary>
        private static string BuildDailyShapeCard(ShareInput input)
        {
            var sb = new StringBuilder(160);
            int n = (input.dailyIndex ?? 0) + 1;
            int par = input.par ?? 0;
            int stars = Mathf.Clamp(input.stars ?? 0, 0, 3);
            string starGlyphs = new string('★', stars) + new string('☆', 3 - stars); // ★ filled / ☆ empty
            string score = input.dailyFailed ? "X" : (input.playerSteps ?? 0).ToString();

            sb.Append("Word Ladder Daily #").Append(n)
              .Append(" · Par ").Append(par)
              .Append(" · ").Append(score).Append('/').Append(par)
              .Append(" · ").Append(starGlyphs).Append('\n');

            if (input.dailyStepClasses != null)
                foreach (int cls in input.dailyStepClasses)
                    sb.Append(GlyphForClass(cls)).Append('\n');

            if (input.streakCurrent.HasValue && input.streakBest.HasValue)
                sb.Append("\U0001F525 Streak ").Append(input.streakCurrent.Value)
                  .Append(" · Best ").Append(input.streakBest.Value);

            return sb.ToString().Replace("\r\n", "\n").TrimEnd('\r', '\n');
        }

        private static string GlyphForClass(int cls)
        {
            switch (cls)
            {
                case 0:  return CHANGED_GLYPH;    // 🟩 optimal
                case 1:  return DETOUR_GLYPH;     // 🟨 detour
                default: return UNCHANGED_GLYPH;  // ⬛ mistake
            }
        }

        /// <summary>
        /// Optional reusable PNG capture for Task 2B. Renders a minimal colored
        /// grid (no header text) to a Texture2D and returns its PNG bytes.
        /// NOT wired into any default share path — requires the user to approve
        /// adding a NativeShare plugin and swapping in a non-clipboard
        /// <see cref="IShareService"/> implementation.
        /// </summary>
        public static byte[] RenderPng(ShareInput input)
        {
            if (input == null || input.chain == null || input.chain.Count < 2)
                return null;

            int rows = input.chain.Count - 1;
            int cols = input.startWord != null ? input.startWord.Length : 0;
            if (cols <= 0) return null;

            const int CELL = 64;
            const int GAP = 8;
            int w = cols * CELL + (cols + 1) * GAP;
            int h = rows * CELL + (rows + 1) * GAP;

            // Direction B share-card palette (Game assembly can't reference UI.Palette — values mirror the tokens).
            var bg = new Color(0.051f, 0.039f, 0.122f, 1f);    // Palette.SurfaceVoid
            var changed = new Color(0.329f, 0.659f, 0.706f, 1f); // Palette.AccentAqua (retired green)
            var unchanged = new Color(0.180f, 0.145f, 0.376f, 1f); // Palette.Panel

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
            tex.SetPixels(pixels);

            for (int r = 0; r < rows; r++)
            {
                string prev = input.chain[r];
                string curr = input.chain[r + 1];
                int rowTopY = h - GAP - (r + 1) * CELL - r * GAP;
                for (int c = 0; c < cols; c++)
                {
                    bool isChanged = c < prev.Length && c < curr.Length && prev[c] != curr[c];
                    var color = isChanged ? changed : unchanged;
                    int x0 = GAP + c * (CELL + GAP);
                    FillRect(tex, x0, rowTopY, CELL, CELL, color);
                }
            }

            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return png;
        }

        // ---------- internals ----------

        internal static string BuildRow(string prev, string curr)
        {
            int len = curr != null ? curr.Length : 0;
            if (len == 0) return string.Empty;
            var sb = new StringBuilder(len * 2);
            for (int i = 0; i < len; i++)
            {
                bool changed = prev != null && i < prev.Length && prev[i] != curr[i];
                sb.Append(changed ? CHANGED_GLYPH : UNCHANGED_GLYPH);
            }
            return sb.ToString();
        }

        internal static string ModeLabel(ShareInput input)
        {
            switch (input.mode)
            {
                case ModeKind.Daily:
                    int n = (input.dailyIndex ?? 0) + 1;
                    return $"Daily #{n}";
                case ModeKind.PuzzleShow:
                    int t = input.puzzleShowTier ?? 1;
                    return $"Puzzle Show T{t}";
                case ModeKind.TimeAttack:
                    int b = input.timeAttackBaseSeconds ?? 60;
                    return input.timeAttackSurvival
                        ? $"Time Attack {b}s Survival"
                        : $"Time Attack {b}s";
                default:
                    return "Classic";
            }
        }

        internal static bool IsTimedMode(ModeKind mode) => mode == ModeKind.TimeAttack;

        internal static string FormatTime(float totalSeconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(totalSeconds));
            int m = s / 60;
            int rem = s % 60;
            return $"{m}:{rem:D2}";
        }

        internal static string Up(string s) => string.IsNullOrEmpty(s) ? "" : s.ToUpperInvariant();

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            for (int yy = y; yy < y + h && yy < tex.height; yy++)
            {
                if (yy < 0) continue;
                for (int xx = x; xx < x + w && xx < tex.width; xx++)
                {
                    if (xx < 0) continue;
                    tex.SetPixel(xx, yy, color);
                }
            }
        }
    }
}
