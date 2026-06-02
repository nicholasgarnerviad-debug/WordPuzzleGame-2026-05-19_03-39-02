#!/usr/bin/env python3
"""
dictionary_build.py  --  TASK 14 reproducible dictionary cleanup + expansion tool.

NOT shipped in the build (lives under Tools/, outside Assets/). Re-runnable and
idempotent: it defines the *target* state of the player-facing dictionary from the
current data plus two cited reference lists, so running it twice yields the same files.

What it does
------------
1. Loads Assets/Resources/Data/{word_library,common_words,tier_definitions,daily_puzzles}.json
2. Builds the PROTECTED set = every word used by any curated tier/daily solution.
   Protected words are NEVER removed, regardless of any other rule.
3. 14B  cleanup:  keep = (library  ENABLE)  protected.  Removes abbreviations /
   acronyms / initialisms (fbi, abc, cpu, ...) and obscure non-words (abaff, abaka, ...)
   and pure proper nouns (adam, mary) -- ENABLE excludes all of these by construction,
   while keeping homographs that are valid lowercase words (john, paris).
4. 14C  expansion:  add the top-FREQUENCY ENABLE 6- and 7-letter words (TARGET_LONG each)
   to word_library.json, and the same set to common_words.json so RANDOM long-puzzle
   generation draws fair, well-known endpoints.
5. Re-validates ALL 90 tier + 450 daily puzzles against the edited dictionary
   (every consecutive solution pair must be same-length Hamming-1 and present) and
   FAILS LOUDLY (exit 1) if any curated puzzle would become unsolvable.

Reference lists (cited)
-----------------------
* Validity / cleanliness:  ENABLE word list (enable1.txt, 172,823 words; public domain)
  https://raw.githubusercontent.com/dolph/dictionary/master/enable1.txt
  Scrabble-style lexicon -> no abbreviations, acronyms, or proper nouns.
* Commonness ranking:  Peter Norvig count_1w.txt (Google Web Trillion-Word Corpus freq)
  https://norvig.com/ngrams/count_1w.txt

Usage:  python Tools/dictionary_build.py            (writes files + prints report)
        python Tools/dictionary_build.py --dry-run  (report only, no writes)
"""
import os, sys, json, tempfile, urllib.request, collections

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))
CACHE = os.path.join(tempfile.gettempdir(), "wl_refs")

ENABLE_URL = "https://raw.githubusercontent.com/dolph/dictionary/master/enable1.txt"
FREQ_URL   = "https://norvig.com/ngrams/count_1w.txt"

TARGET_LONG = 2500          # per length (6 and 7); "Dense" target chosen in PLAN
MIN_LEN, MAX_LEN = 3, 7

# Known junk that must never appear (belt-and-suspenders; ENABLE already excludes these).
JUNK_BLOCKLIST = ["abc","abp","abr","abt","acc","adp","afb","cpu","fbi","gps","ibm",
                  "irs","mph","mpg","rpm","std","qty","asst","dept","govt"]


def _load(name):
    with open(os.path.join(DATA, name), encoding="utf-8") as f:
        return json.load(f)


def _fetch(url, fn):
    os.makedirs(CACHE, exist_ok=True)
    p = os.path.join(CACHE, fn)
    if not os.path.exists(p):
        print(f"  downloading {fn} ...")
        urllib.request.urlretrieve(url, p)
    return p


def _read_wordset(path):
    return set(w.strip().lower() for w in open(path, encoding="utf-8") if w.strip())


def _read_freq_rank(path):
    rank = {}
    for i, line in enumerate(open(path, encoding="utf-8")):
        w = line.split("\t")[0].strip().lower()
        if w and w not in rank:
            rank[w] = i
    return rank


def collect_protected(*roots):
    prot = set()
    def walk(o):
        if isinstance(o, dict):
            for k, v in o.items():
                if k in ("startWord", "endWord") and isinstance(v, str):
                    prot.add(v.lower())
                elif k == "solution" and isinstance(v, list):
                    prot.update(str(x).lower() for x in v)
                else:
                    walk(v)
        elif isinstance(o, list):
            for x in o:
                walk(x)
    for r in roots:
        walk(r)
    return prot


def hamming1(a, b):
    if len(a) != len(b):
        return False
    return sum(1 for x, y in zip(a, b) if x != y) == 1


def iter_puzzles(td, dp):
    for t in td["tiers"]:
        for p in t["puzzles"]:
            yield ("tier", t.get("tierId"), p)
    for p in dp["puzzles"]:
        yield ("daily", None, p)


def validate_solvable(td, dp, dictionary):
    """Every curated solution must be present + a valid one-letter ladder. Returns failures."""
    failures = []
    for kind, tid, p in iter_puzzles(td, dp):
        sol = [w.lower() for w in p.get("solution", [])]
        pid = p.get("puzzleId")
        if len(sol) < 2:
            failures.append(f"{kind} {pid}: solution < 2 words")
            continue
        for w in sol:
            if w not in dictionary:
                failures.append(f"{kind} {pid}: solution word '{w}' missing from dictionary")
        for a, b in zip(sol, sol[1:]):
            if not hamming1(a, b):
                failures.append(f"{kind} {pid}: '{a}'->'{b}' is not a single-letter edit")
        if sol[0] != p.get("startWord", "").lower():
            failures.append(f"{kind} {pid}: solution[0] != startWord")
        if sol[-1] != p.get("endWord", "").lower():
            failures.append(f"{kind} {pid}: solution[-1] != endWord")
    return failures


def dist(words):
    return dict(sorted(collections.Counter(len(w) for w in words).items()))


def write_words(name, words):
    """Preserve the {"words":[...]} shape JsonUtility depends on; one word per line, sorted."""
    body = ",\n".join(json.dumps(w) for w in words)
    text = '{\n"words":[\n' + body + "\n]\n}\n"
    with open(os.path.join(DATA, name), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def main():
    dry = "--dry-run" in sys.argv
    print(f"DATA dir: {DATA}")
    lib_words = [w.lower() for w in _load("word_library.json")["words"]]
    common_words = [w.lower() for w in _load("common_words.json")["words"]]
    td = _load("tier_definitions.json")
    dp = _load("daily_puzzles.json")

    protected = collect_protected(td, dp)
    print(f"\nPROTECTED set (curated tier+daily solution words): {len(protected)}")

    print("\nReference lists:")
    ENABLE = _read_wordset(_fetch(ENABLE_URL, "enable1.txt"))
    rank   = _read_freq_rank(_fetch(FREQ_URL, "count_1w.txt"))
    print(f"  ENABLE: {len(ENABLE)} words   freq corpus: {len(rank)} tokens")

    lib_set = set(lib_words)
    before_total = len(lib_set)
    before_dist = dist(lib_set)

    # --- 14B: cleanup -----------------------------------------------------
    keep = {w for w in lib_set if w in ENABLE or w in protected}
    removed = lib_set - keep
    print(f"\n14B cleanup:  {before_total} -> {len(keep)}  (removed {len(removed)})")
    print(f"  removed length dist: {dist(removed)}")

    # sanity: protected fully retained
    lost_protected = protected - keep
    assert not lost_protected, f"BUG: protected words dropped: {sorted(lost_protected)[:20]}"

    # --- 14C: expansion ---------------------------------------------------
    new_long = set()
    for L in (6, 7):
        pool = sorted([w for w in ENABLE if len(w) == L and w in rank], key=lambda w: rank[w])
        new_long.update(pool[:TARGET_LONG])
    final_lib = keep | new_long
    added = final_lib - keep
    print(f"\n14C expansion:  +{len(added)} common 6/7-letter words (target {TARGET_LONG} each)")

    # common_words: drop anything no longer valid, then seed with the new long words.
    final_common = ({w for w in common_words if w in final_lib} | (new_long & final_lib))

    final_lib_sorted = sorted(final_lib)
    final_common_sorted = sorted(final_common)

    print(f"\nFINAL word_library: {before_total} -> {len(final_lib_sorted)}")
    print(f"  before dist: {before_dist}")
    print(f"  after  dist: {dist(final_lib_sorted)}")
    print(f"FINAL common_words: {len(common_words)} -> {len(final_common_sorted)}")
    print(f"  after  dist: {dist(final_common_sorted)}")

    # junk gone?
    still_junk = [j for j in JUNK_BLOCKLIST if j in final_lib]
    print(f"\nJunk-blocklist still present: {still_junk if still_junk else 'NONE (good)'}")

    # --- re-validate curated puzzles -------------------------------------
    failures = validate_solvable(td, dp, final_lib)
    n_puz = sum(len(t["puzzles"]) for t in td["tiers"]) + len(dp["puzzles"])
    if failures:
        print(f"\n!!! SOLVABILITY FAILURES ({len(failures)}/{n_puz} puzzles affected):")
        for f in failures[:30]:
            print("   ", f)
        sys.exit(1)
    print(f"\nSolvability: ALL {n_puz} curated puzzles (90 tier + 450 daily) still solvable.")

    if dry:
        print("\n--dry-run: no files written.")
        return
    write_words("word_library.json", final_lib_sorted)
    write_words("common_words.json", final_common_sorted)
    print("\nWrote word_library.json and common_words.json (shape: {\"words\":[...]}).")


if __name__ == "__main__":
    main()
