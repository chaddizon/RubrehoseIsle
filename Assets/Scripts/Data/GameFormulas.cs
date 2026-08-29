using System;

namespace Rubrehose.Data
{
    // Direct port of the locked formulas from GAME_DESIGN.md, retargeted at the 4-cove
    // structure in CORE_PROGRESSION_RESTRUCTURE.md.
    public static class GameFormulas
    {
        // Base coefficients (36000 HP / 15 Armor for Landing Cove) deliberately retuned far
        // above GAME_DESIGN.md's original 50/8 — see the "why is the first serpent so easy"
        // discussion this replaces. With SalvagePower's early curve and a real 20-min
        // cooldown between attempts (GameManager.CanAttemptFight/FightCooldownSeconds), the
        // goal is Landing Cove taking on the order of 50-60 real attempts to clear: a floor of
        // several real days even for someone hitting every cooldown window during all their
        // waking hours, and multiple weeks for a couple-check-ins-a-day casual player — slow,
        // long-term progression is the point (art-production pacing + Obelisk-style long-haul
        // investment), not a bug. Armor still requires a small early tap-level investment
        // before any damage lands (~5 upgrades, well under 200 driftwood) so the fight isn't
        // silently a no-op before that, matching the pre-existing "damage below armor deals
        // zero" rule. This is a first-pass estimate, not playtested over real elapsed days —
        // retune these two coefficients (only) once real clear times are observed; the
        // per-cove growth curve below is unchanged on purpose.
        //
        // Only takes coveIndex now (0-2 — the 3 finite, one-time-defeat coves; the 4th/endless
        // cove uses SerpentHpForLevel/SerpentArmorForLevel below instead). The old
        // biomeIndex parameter and its BiomeMultiplier table were dropped entirely:
        // CORE_PROGRESSION_RESTRUCTURE.md retires "biome" as a near-term concept (repurposed
        // to a future, rare, endgame-only full-island expansion that doesn't exist yet), and
        // every call site only ever passed BiomeMultiplier[0] == 1 anyway, so it was dead
        // weight. Re-add a multiplier dimension here (not on the old signature — that array
        // was 6-long for a biome roster that no longer applies near-term) if/when that
        // expansion actually gets built.
        public static double SerpentHp(int coveIndex) =>
            Math.Round(36000 * Math.Pow(2.5, coveIndex));

        public static double SerpentArmor(int coveIndex) =>
            Math.Round(15 * Math.Pow(2.8, coveIndex));

        // Cove 4's serpent — the permanent, endlessly-scaling "Obelisk" fight
        // (CORE_PROGRESSION_RESTRUCTURE.md "Cove 4's serpent"). serpentLevel is a single
        // persistent counter (PlayerState.serpentLevel) that only ever goes up, replacing the
        // one-time per-cove HP/Armor above once GameManager.IsEndlessCove(coveIndex) is true.
        //
        // Armor mirrors Idle Obelisk Miner's real cited formula exactly, per the doc:
        //   Armor(level) = round(10 x 2.8^(level-1))          for level <= 60
        //                = round(Armor(60) x 9.5^(level-60))   for level > 60
        // The doc's own table writes the breakpoint as "1-61" then "61+", which double-covers
        // level 61 — resolved here as <=60 / >60 (i.e. the x9.5 jump starts AT level 61), the
        // only reading that doesn't define level 61 twice. Flag this to Chad if that's not
        // what was intended.
        private const int ArmorBreakpointLevel = 60;

        public static double SerpentArmorForLevel(int serpentLevel)
        {
            if (serpentLevel <= ArmorBreakpointLevel)
                return Math.Round(10 * Math.Pow(2.8, serpentLevel - 1));

            double armorAtBreakpoint = Math.Round(10 * Math.Pow(2.8, ArmorBreakpointLevel - 1));
            return Math.Round(armorAtBreakpoint * Math.Pow(9.5, serpentLevel - ArmorBreakpointLevel));
        }

        // The doc only asks for HP to be "a comparable exponential shape," not a literal cited
        // Obelisk HP formula (unlike Armor, which it explicitly says to mirror exactly) — no
        // such number was given. First-pass choice here: literally the same shape/breakpoint
        // as Armor, scaled by a flat multiplier chosen to match Landing Cove's existing
        // HP:Armor ratio (36000/15 = 2400x). Entirely unplaytested at endless-fight scale —
        // retune HpToArmorRatio once real level-60+ playtesting exists, same caveat as
        // SerpentHp/SerpentArmor's base coefficients above.
        private const double HpToArmorRatio = 2400;

        public static double SerpentHpForLevel(int serpentLevel) =>
            Math.Round(SerpentArmorForLevel(serpentLevel) * HpToArmorRatio);

        // Driftwood reward for beating the endless cove's serpent at a given level — the doc
        // doesn't specify an economy for this (only that Artifact unlocks map to serpentLevel
        // milestones, which is a separate, still-unbuilt system). First-pass placeholder: a
        // roughly 3x jump per level, off a base of 250 (the old finite-cove SerpentClearReward's
        // value at Landing Cove, inlined here now that helper is removed — see
        // CORE_PROGRESSION_RESTRUCTURE.md's "REMOVED: the construction gate step", which also
        // retired the finite coves' own clear reward entirely, not just this one). Needs real
        // tuning once the finite coves' pacing (and thus how quickly a player even reaches
        // cove 4) is playtested.
        public static double SerpentLevelClearReward(int serpentLevel) =>
            Math.Round(250 * Math.Pow(3, serpentLevel - 1));

        public static double SalvagePower(int tapLevel) =>
            (tapLevel + 1) * (tapLevel + 2) / 2.0;

        public static double TapUpgradeCost(int tapLevel) =>
            Math.Round(20 * Math.Pow(1.15, tapLevel));

        public const float FightDurationSeconds = 30f;

        // "Cooldown = 20 minutes between attempts on same cove's serpent" (GAME_DESIGN.md),
        // matches Obelisk's real model exactly (rubrehose_prototype.html). Also governs the
        // endless cove's timeout/retreat cooldown — only an immediate re-kill (defeat, not
        // timeout) skips it, per GameManager.EndFightAttempt.
        public const float FightCooldownSeconds = 20f * 60f;
    }
}
