#!/usr/bin/env python3
"""
dictionary_build.py  --  reproducible player-dictionary cleanup + expansion tool.

NOT shipped in the build (lives under Tools/, outside Assets/). Re-runnable and
idempotent: it defines the *target* state of the player-facing dictionary purely as a
function of two cited public reference lists plus the curated-puzzle PROTECTED set, so
running it twice yields byte-identical files.

LICENSE (verified — Task 14 STEP 0)
-----------------------------------
Shipped word CONTENT comes only from the ENABLE word list, which is PUBLIC DOMAIN
(purpose-built free Scrabble-style lexicon — NOT the copyrighted TWL or SOWPODS/Collins).
=> commercial redistribution in a paid/ad-supported app is permitted.
Peter Norvig's count_1w.txt frequency list is used at BUILD TIME ONLY, to RANK which
public-domain ENABLE words to include (obscurity gate). The frequency numbers themselves
are not redistributed; the shipped JSON contains only ENABLE words. (See ConPort decision #5.)

Target-state definition (deterministic)
---------------------------------------
  PROTECTED  = every word used by any curated tier/daily start/end/solution. NEVER removed.
  library    = { w in ENABLE : MIN_LEN<=len(w)<=MAX_LEN and rank(w) < LIB_RANK }   (the
               obscurity gate: drops ENABLE words too rare to be fair — abaka, abmho, abos…)
               UNION PROTECTED,  MINUS OFFENSIVE_BLOCKLIST (protected words are never dropped).
  common     = { w in library : rank(w) < COMMON_RANK }  UNION (PROTECTED ∩ library)
               (the well-known subset RANDOM puzzle generation draws fair endpoints from).

This simultaneously CLEANS (removes obscure/unfair entries that pass ENABLE but are far
down the frequency tail, plus any offensive terms) and EXPANDS (adds every fair, common-
enough ENABLE word at ALL supported lengths 3–7, not just 6/7), keeping library⊇common
consistent by construction.

Guarantees re-validated against the edited dictionary (FAIL LOUD, exit 1 on any breach):
  * every curated tier+daily solution word is present,
  * every consecutive solution pair is same-length Hamming-1,
  * solution[0]==startWord, solution[-1]==endWord,
  * MULTI-ROUTE: report how many curated puzzles still have >1 distinct shortest path,
    and FAIL if any curated puzzle becomes UNSOLVABLE in the cleaned graph.

Reference lists (cited)
-----------------------
* Validity / cleanliness:  ENABLE word list (enable1.txt, 172,823 words; PUBLIC DOMAIN)
  https://raw.githubusercontent.com/dolph/dictionary/master/enable1.txt
* Commonness ranking:  Peter Norvig count_1w.txt (Google Web Trillion-Word Corpus freq)
  https://norvig.com/ngrams/count_1w.txt

Usage:  python Tools/dictionary_build.py            (writes files + prints report)
        python Tools/dictionary_build.py --dry-run  (report only, no writes)
"""
import os, sys, json, tempfile, urllib.request, collections
from collections import deque, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Data"))
CACHE = os.path.join(tempfile.gettempdir(), "wl_refs")

ENABLE_URL = "https://raw.githubusercontent.com/dolph/dictionary/master/enable1.txt"
FREQ_URL   = "https://norvig.com/ngrams/count_1w.txt"

MIN_LEN, MAX_LEN = 3, 7
# Daily puzzles with id >= this are APPENDED by daily_expand.py and are regenerated every
# run (so their words must NOT pin the dictionary). Must match daily_expand.APPEND_ID_BASE.
DAILY_APPEND_ID_BASE = 20001
# Obscurity gate (Norvig rank, 0 = most frequent). Calibrated so the classic obscure
# ENABLE junk (abaka/abmho/abos/abysm…) is dropped while fair words at every length stay.
LIB_RANK    = 60000     # word_library inclusion ceiling
COMMON_RANK = 15000     # common_words (random-generation endpoints) — tighter, well-known

# Offensive / slur blocklist — words that exist in ENABLE but must never surface in a
# family-friendly puzzle. Protected curated words are NEVER affected (none of these are).
# Lower-case, de-duplicated. Kept deliberately conservative + auditable.
OFFENSIVE_BLOCKLIST = {
    # racial / ethnic slurs and variants present in word lists
    "nigger", "niggers", "nigga", "niggas", "niggah", "coon", "coons", "kike", "kikes",
    "spic", "spics", "spik", "spiks", "wop", "wops", "dago", "dagos", "dagoes",
    "chink", "chinks", "gook", "gooks", "kraut", "krauts", "wetback", "wetbacks",
    "darkie", "darkies", "darky", "negro", "negros", "negroes", "gyp", "gyps", "gypped",
    # sexual / vulgar
    "fuck", "fucks", "fucked", "fucker", "fuckers", "fucking", "fuk",
    "shit", "shits", "shitty", "shat", "cunt", "cunts", "twat", "twats",
    "cock", "cocks", "dick", "dicks", "prick", "pricks", "pussy", "pussies",
    "tits", "titty", "titties", "boob", "boobs", "boner", "boners",
    "cum", "cums", "jizz", "jism", "jism", "wank", "wanks", "wanker", "wankers",
    "slut", "sluts", "slutty", "whore", "whores", "whored", "skank", "skanks",
    "bastard", "bastards", "bitch", "bitches", "bitchy", "damn", "damns",
    # slurs targeting orientation / disability
    "fag", "fags", "faggot", "faggots", "faggy", "dyke", "dykes", "homo", "homos",
    "queer", "queers", "tranny", "trannies", "retard", "retards", "retarded", "spaz", "spazzes",
    "cripple", "cripples",
    # misc offensive
    "anus", "anuses", "arse", "arses", "turd", "turds", "crap", "craps", "crappy",
    "piss", "pisses", "pissed", "feces", "porn", "porno", "pornos",
}


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
    return len(a) == len(b) and sum(1 for x, y in zip(a, b) if x != y) == 1


def iter_puzzles(td, dp):
    for t in td["tiers"]:
        for p in t["puzzles"]:
            yield ("tier", t.get("tierId"), p)
    for p in dp["puzzles"]:
        yield ("daily", None, p)


def _references_offensive(p):
    sol = [str(w).lower() for w in p.get("solution", [])]
    pts = set(sol) | {p.get("startWord", "").lower(), p.get("endWord", "").lower()}
    return bool(pts & OFFENSIVE_BLOCKLIST)


def validate_solvable(td, dp, dictionary):
    """Every curated solution must be present + a valid one-letter ladder. Returns failures.
    Tier puzzles referencing an offensive word are skipped (they are regenerated clean
    by puzzleshow_build.py after this tool)."""
    failures = []
    for kind, tid, p in iter_puzzles(td, dp):
        if kind == "tier" and _references_offensive(p):
            continue
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


def build_graph(words):
    """Hamming-1 adjacency via wildcard bucketing (same shape as WordGraph)."""
    words = set(words)
    buckets = defaultdict(list)
    for w in words:
        for i in range(len(w)):
            buckets[w[:i] + "*" + w[i + 1:]].append(w)
    adj = defaultdict(set)
    for ws in buckets.values():
        for a in ws:
            for c in ws:
                if a != c:
                    adj[a].add(c)
    return adj


def count_shortest_paths(adj, s, e):
    """(#distinct shortest paths s->e, shortest distance) ; (0,-1) if unreachable."""
    if s == e:
        return 1, 0
    dist = {s: 0}
    npaths = {s: 1}
    q = deque([s])
    found = None
    while q:
        u = q.popleft()
        if found is not None and dist[u] >= found:
            continue
        for v in adj[u]:
            if v not in dist:
                dist[v] = dist[u] + 1
                npaths[v] = npaths[u]
                if v == e:
                    found = dist[v]
                q.append(v)
            elif dist[v] == dist[u] + 1:
                npaths[v] += npaths[u]
    return npaths.get(e, 0), dist.get(e, -1)


def multi_route_report(td, dp, library):
    """Count curated puzzles with >1 distinct shortest path; FAIL if any unsolvable."""
    by_len = defaultdict(list)
    for w in library:
        by_len[len(w)].append(w)
    adj_by_len = {L: build_graph(by_len[L]) for L in set(len(w) for w in library)}
    multi = single = unsolvable = 0
    unsolv_list = []
    for kind, tid, p in iter_puzzles(td, dp):
        if kind == "tier" and _references_offensive(p):
            continue
        s = p["startWord"].lower(); e = p["endWord"].lower()
        adj = adj_by_len.get(len(s), {})
        n, dd = count_shortest_paths(adj, s, e)
        if dd < 0:
            unsolvable += 1
            unsolv_list.append(f"{kind} {p.get('puzzleId')}: {s}->{e} UNSOLVABLE")
        elif n > 1:
            multi += 1
        else:
            single += 1
    return multi, single, unsolvable, unsolv_list


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
    prev_lib = set(w.lower() for w in _load("word_library.json")["words"])
    prev_common = set(w.lower() for w in _load("common_words.json")["words"])
    td = _load("tier_definitions.json")
    dp = _load("daily_puzzles.json")

    # Which puzzles are REGENERATED downstream (so their words must NOT pin the dictionary,
    # else library<->puzzles becomes a circular dependency and the pipeline is non-repro):
    #   * ALL tier puzzles      -> regenerated by puzzleshow_build.py
    #   * APPENDED daily puzzles (id >= DAILY_APPEND_ID_BASE) -> regenerated by daily_expand.py
    # Only the ORIGINAL, hand-curated daily puzzles (id < base) are permanent and must be
    # protected so cleaning never deletes a word one of them depends on.
    protected_tier = collect_protected(td)
    orig_daily = {"puzzles": [p for p in dp["puzzles"]
                              if p.get("puzzleId", 0) < DAILY_APPEND_ID_BASE]}
    append_daily = {"puzzles": [p for p in dp["puzzles"]
                                if p.get("puzzleId", 0) >= DAILY_APPEND_ID_BASE]}
    protected_daily = collect_protected(orig_daily)           # PERMANENT -> pins dictionary
    protected_daily_append = collect_protected(append_daily)  # regenerated -> does NOT pin
    protected_all = protected_tier | protected_daily | protected_daily_append
    print(f"\nPROTECTED words: permanent(orig-daily)={len(protected_daily)}  "
          f"regenerated(tier+appended-daily)={len(protected_tier | protected_daily_append)}")

    print("\nReference lists:")
    ENABLE = _read_wordset(_fetch(ENABLE_URL, "enable1.txt"))
    rank   = _read_freq_rank(_fetch(FREQ_URL, "count_1w.txt"))
    print(f"  ENABLE: {len(ENABLE)} words   freq corpus: {len(rank)} tokens")
    print(f"  LIB_RANK gate: {LIB_RANK}   COMMON_RANK gate: {COMMON_RANK}")
    print(f"  offensive blocklist: {len(OFFENSIVE_BLOCKLIST)} terms")

    # A DAILY puzzle referencing an offensive word cannot be silently fixed by tier
    # regeneration — fail loud so it is dealt with explicitly (the daily tools handle it).
    daily_offensive = sorted(protected_daily & OFFENSIVE_BLOCKLIST)
    assert not daily_offensive, (
        f"Offensive word in a DAILY curated solution (not auto-regenerated): {daily_offensive}")

    # Offensive words appearing only in TIER puzzles: drop from the protected set; the
    # seeded tier regeneration will author clean replacements from common_words.
    tier_offensive = sorted(protected_tier & OFFENSIVE_BLOCKLIST)
    if tier_offensive:
        print(f"  offensive word(s) in TIER puzzles -> dropped from protected, "
              f"tiers will be regenerated clean: {tier_offensive}")
    protected = protected_all - OFFENSIVE_BLOCKLIST

    # sanity: every (non-offensive) protected word is a valid ENABLE word
    prot_not_enable = sorted(w for w in protected if w not in ENABLE)
    assert not prot_not_enable, f"PROTECTED words not in ENABLE: {prot_not_enable[:20]}"

    # --- TARGET-STATE library --------------------------------------------
    # Pin ONLY the PERMANENT (original hand-curated daily) protected words. Tier and
    # appended-daily words are guaranteed to live in `common` (⊆ gated ⊆ library) because the
    # generators draw them from `common`, so they need no separate pin — and pinning them
    # would reintroduce the circular dependency that breaks reproducibility.
    protected_permanent = protected_daily - OFFENSIVE_BLOCKLIST
    gated = {w for w in ENABLE
             if MIN_LEN <= len(w) <= MAX_LEN and w in rank and rank[w] < LIB_RANK}
    library = (gated | protected_permanent) - OFFENSIVE_BLOCKLIST

    removed = prev_lib - library
    added   = library - prev_lib
    blocked = sorted((prev_lib | gated) & OFFENSIVE_BLOCKLIST)
    print(f"\nword_library: {len(prev_lib)} -> {len(library)}   (removed {len(removed)}, added {len(added)})")
    print(f"  removed length dist: {dist(removed)}")
    print(f"  added   length dist: {dist(added)}")
    print(f"  before dist: {dist(prev_lib)}")
    print(f"  after  dist: {dist(library)}")
    print(f"  offensive terms excluded (present in ENABLE/old-lib): {blocked if blocked else 'none'}")

    lost_protected = protected - library
    assert not lost_protected, f"BUG: protected words dropped: {sorted(lost_protected)[:20]}"

    # --- TARGET-STATE common ---------------------------------------------
    # PURE function of (ENABLE, rank, library) — deliberately does NOT union `protected`.
    # protected words come from the curated tier/daily puzzles, which are REGENERATED by the
    # downstream tools (puzzleshow_build / daily_expand) AFTER this runs; folding them back in
    # here would couple common_words to the very files it seeds, making the pipeline non-
    # reproducible (common -> tiers -> protected -> common ...). Curated puzzles are validated
    # against `library`, and the generators draw fresh endpoints from `common`, so an existing
    # endpoint never needs to live in `common`. This keeps the whole pipeline a fixed point.
    common = {w for w in library if w in rank and rank[w] < COMMON_RANK}
    common -= OFFENSIVE_BLOCKLIST
    com_removed = prev_common - common
    com_added = common - prev_common
    print(f"\ncommon_words: {len(prev_common)} -> {len(common)}   (removed {len(com_removed)}, added {len(com_added)})")
    print(f"  before dist: {dist(prev_common)}")
    print(f"  after  dist: {dist(common)}")
    assert common <= library, "BUG: common_words must be a subset of word_library"

    # junk gone?
    sample_removed = sorted(removed)[:25]
    print(f"\n  sample removed (obscure): {sample_removed}")

    # --- re-validate curated puzzles: solvability + Hamming-1 ladders -----
    failures = validate_solvable(td, dp, library)
    n_puz = sum(len(t["puzzles"]) for t in td["tiers"]) + len(dp["puzzles"])
    if failures:
        print(f"\n!!! SOLVABILITY FAILURES ({len(failures)} issues across {n_puz} puzzles):")
        for f in failures[:30]:
            print("   ", f)
        sys.exit(1)
    print(f"\nSolvability: ALL {n_puz} curated puzzles (tier + daily) ladders present & Hamming-1.")

    # --- MULTI-ROUTE report (cleaning must not gut the graph) -------------
    multi, single, unsolvable, unsolv_list = multi_route_report(td, dp, library)
    print(f"\nMulti-route (in cleaned graph): multi-path {multi}, single-path {single}, "
          f"unsolvable {unsolvable}  of {n_puz}")
    if unsolvable:
        print("!!! UNSOLVABLE curated puzzles after cleaning:")
        for u in unsolv_list[:30]:
            print("   ", u)
        sys.exit(1)

    if dry:
        print("\n--dry-run: no files written.")
        return
    write_words("word_library.json", sorted(library))
    write_words("common_words.json", sorted(common))
    print("\nWrote word_library.json and common_words.json (shape: {\"words\":[...]}).")


if __name__ == "__main__":
    main()
