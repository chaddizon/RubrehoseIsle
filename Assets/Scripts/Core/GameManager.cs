using System;
using System.Collections.Generic;
using UnityEngine;
using Rubrehose.Data;

namespace Rubrehose.Core
{
    // Central game-state owner for the Wreck Beach vertical slice.
    // UI scripts subscribe to OnStateChanged and call the public methods below;
    // nothing here reaches into UI directly, so scene wiring is entirely Chad's side.
    //
    // Forced to initialize before every other script: Unity does NOT guarantee this
    // object's Awake() (which sets Instance) runs before another GameObject's OnEnable()
    // — that ordering depends on scene hierarchy position, not load/dependency order.
    // Several OnEnable() methods (CoveBuildingVisual, MenuDrawerController,
    // PersistentHUDController) read GameManager.Instance and NullReferenceException
    // without this.
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerState State { get; private set; }
        public double LastOfflineEarnings { get; private set; }

        public event Action OnStateChanged;

        // Fires the instant a cove's mini-boss defeat unlocks the next cove (previousCoveIndex,
        // newCoveIndex) — CoveViewCamera subscribes to auto-pan-reveal the new cove, and
        // OnboardingController subscribes to fire popup #12 (CORE_PROGRESSION_RESTRUCTURE.md
        // "New unlock moment" / the onboarding table's row 12). Never fires for the endless
        // cove's serpent (RegisterSerpentLevelUp doesn't advance coveIndex further, nothing to
        // unlock past it).
        public event Action<int, int> OnCoveUnlocked;

        // Fires once a Cove Building stage is actually paid for (id, new stage 1-3) —
        // OnboardingController subscribes to fire popup #14.
        public event Action<string, int> OnBuildingStageCompleted;

        // Fires once a Compass Shard is actually found (tier) — OnboardingController
        // subscribes to fire the previously-deferred Artifacts onboarding row now that this
        // system exists.
        public event Action<string> OnCompassShardFound;

        [SerializeField] private float passiveTickInterval = 0.5f;
        private float _tickTimer;

        private const float AutoSaveIntervalSeconds = 60f;
        private const long MaxOfflineSeconds = 8 * 3600;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadOrCreateState();
            InvokeRepeating(nameof(AutoSave), AutoSaveIntervalSeconds, AutoSaveIntervalSeconds);
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < passiveTickInterval) return;
            float elapsed = _tickTimer;
            _tickTimer = 0f;

            // Real-time fight cooldown — ticks whether or not the fight modal is open.
            if (State.fightCooldownSeconds > 0f)
            {
                State.fightCooldownSeconds = Mathf.Max(0f, State.fightCooldownSeconds - elapsed);
                RaiseChanged();
            }

            double total = 0;
            foreach (var c in State.crew) total += CrewRate(c);
            if (total > 0) AddDriftwood(total * elapsed);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        private void OnApplicationQuit() => Save();

        // --- Save / load ---------------------------------------------------

        private void LoadOrCreateState()
        {
            var loaded = SaveSystem.Load();
            if (loaded == null)
            {
                State = CreateNewState();
                return;
            }
            State = loaded;
            ApplyOfflineEarnings();
        }

        private PlayerState CreateNewState()
        {
            var state = new PlayerState { lastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            foreach (var def in CrewCatalog.WreckBeachCrew)
                state.crew.Add(new CrewState { id = def.id, level = 0, currentCost = def.baseCost });
            return state;
        }

        private void ApplyOfflineEarnings()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = Math.Clamp(now - State.lastSaveUnixSeconds, 0, MaxOfflineSeconds);
            if (elapsed <= 0) return;

            double totalRate = 0;
            foreach (var c in State.crew) totalRate += CrewRate(c);
            LastOfflineEarnings = totalRate * elapsed;
            if (LastOfflineEarnings > 0) AddDriftwood(LastOfflineEarnings);
        }

        private void AutoSave() => Save();

        public void Save()
        {
            State.lastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveSystem.Save(State);
        }

        // --- Tap / driftwood -------------------------------------------------

        // Cove Buildings and Artifacts are separate trees (EXPANDED_UPGRADES_AND_BALANCE.md's
        // additive-within/multiplicative-across rule) so each multiplies against the base tap
        // formula independently, rather than summing into one shared bonus pool. See
        // BuildingTapPowerBonusSum's and ArtifactTapPowerBonusSum's own comments for the
        // rebalancing caveat both introduce.
        public double TapPower => GameFormulas.SalvagePower(State.tapLevel)
            * (1 + BuildingTapPowerBonusSum)
            * (1 + ArtifactTapPowerBonusSum);

        public void Tap() => AddDriftwood(TapPower);

        // Public so world-space side-loops (e.g. BottleCastPoint) can pay out rewards
        // through the same single entry point as Tap/crew/mini-boss income.
        public void AddDriftwood(double amount)
        {
            State.driftwood += amount;
            State.totalEarnedAllTime += amount;
            RaiseChanged();
        }

        public double TapUpgradeCost => GameFormulas.TapUpgradeCost(State.tapLevel);
        public bool CanUpgradeTap => State.driftwood >= TapUpgradeCost;

        public void UpgradeTap()
        {
            if (!CanUpgradeTap) return;
            State.driftwood -= TapUpgradeCost;
            State.tapLevel++;
            RaiseChanged();
        }

        // --- Crew --------------------------------------------------------

        public CrewDefinition GetCrewDefinition(string id) => CrewCatalog.Find(id);
        public CrewState GetCrewState(string id) => State.crew.Find(c => c.id == id);

        public double CrewRate(CrewState c)
        {
            var def = GetCrewDefinition(c.id);
            return def == null ? 0 : c.level * def.ratePerSecond;
        }

        public bool CanRecruit(string id)
        {
            var c = GetCrewState(id);
            return c != null && State.driftwood >= c.currentCost;
        }

        public void RecruitCrew(string id)
        {
            var c = GetCrewState(id);
            if (c == null || State.driftwood < c.currentCost) return;
            State.driftwood -= c.currentCost;
            c.level++;
            c.currentCost = Math.Round(c.currentCost * 1.35);
            RaiseChanged();
        }

        // --- Cove / mini-boss (CAMERA_AND_UI_SPEC.md "Cove-gate structure",
        // rubrehose_prototype.html — matches Obelisk's exact fight model) -------------

        // Index 3 (the last of WreckBeachData.CoveNames) is the permanent, endlessly-scaling
        // serpent fight (CORE_PROGRESSION_RESTRUCTURE.md "Cove 4's serpent") — our Obelisk
        // equivalent. Indices 0-2 keep the exact one-time mini-boss model already built.
        public bool IsEndlessCove(int cove) => cove == WreckBeachData.CoveNames.Length - 1;

        // 0 is a valid save value (never reached the endless cove yet) but not a valid fight
        // level — GameFormulas' level formulas start at 1, and RegisterMiniBossDefeat sets
        // this to 1 explicitly on first arrival anyway. This getter just makes every other read of
        // "the current level" (CoveHp/CoveArmor/FightController's display) agree even before
        // that first-arrival write has happened.
        private int EffectiveSerpentLevel => Math.Max(1, State.serpentLevel);
        public int SerpentLevel => EffectiveSerpentLevel;

        public double CoveHp => IsEndlessCove(State.coveIndex)
            ? GameFormulas.SerpentHpForLevel(EffectiveSerpentLevel)
            : GameFormulas.SerpentHp(State.coveIndex);

        public double CoveArmor => IsEndlessCove(State.coveIndex)
            ? GameFormulas.SerpentArmorForLevel(EffectiveSerpentLevel)
            : GameFormulas.SerpentArmor(State.coveIndex);

        // Always false for the endless cove — it's never "permanently" defeated, only ever
        // leveled up (RegisterSerpentLevelUp). Guards the coveMinibossDefeated array access,
        // which is sized only for the 3 finite coves.
        public bool CoveMinibossDefeated => !IsEndlessCove(State.coveIndex) && State.coveMinibossDefeated[State.coveIndex];

        // HP persists in State.bossHpRemaining across attempts; falls back to full HP
        // before the first-ever attempt on this cove (sentinel -1). Also what re-arms the
        // endless cove's next level after a kill (RegisterSerpentLevelUp resets the sentinel).
        public double BossHpRemaining => State.bossHpRemaining < 0 ? CoveHp : State.bossHpRemaining;

        // No clears-needed counter, no attempt limit — the only gate is this cooldown
        // (started on retreat/timeout, never on defeat) and whether the boss is already dead.
        // For the endless cove CoveMinibossDefeated is always false, so this only ever gates
        // on cooldown there — matching "unlimited attempts" for an unkillable-for-good boss.
        public bool CanAttemptFight => !CoveMinibossDefeated && State.fightCooldownSeconds <= 0f;

        public void BeginFightAttempt()
        {
            if (State.bossHpRemaining < 0) State.bossHpRemaining = CoveHp; // first-ever encounter this cove/level
        }

        // Damage always applies (Attack() itself already refuses to call this when tap power
        // can't overcome armor). Defeat is detected here so HP and the defeated/level-up state
        // can never drift apart. Returns whether this hit finished the current HP pool, so
        // FightController can react (defeat visual, ending the attempt) without needing to
        // separately re-derive it from CoveMinibossDefeated — which stays permanently false
        // for the endless cove and so can't signal "defeated this hit" there.
        public bool ApplyFightDamage(double dmg)
        {
            State.bossHpRemaining = Math.Max(0, State.bossHpRemaining - dmg);
            if (State.bossHpRemaining <= 0)
            {
                if (IsEndlessCove(State.coveIndex)) RegisterSerpentLevelUp();
                else RegisterMiniBossDefeat();
                return true;
            }
            RaiseChanged();
            return false;
        }

        // Called on retreat or fight-timer timeout — never on defeat. Starts the real
        // 20-minute cooldown; HP already persisted via ApplyFightDamage, so nothing here.
        public void EndFightAttempt()
        {
            if (!CoveMinibossDefeated) State.fightCooldownSeconds = GameFormulas.FightCooldownSeconds;
            RaiseChanged();
        }

        // Winning immediately unlocks the next cove — no construction-gate payment step
        // anymore (CORE_PROGRESSION_RESTRUCTURE.md "REMOVED: the construction gate step":
        // Wreck Beach is one continuous scrollable island now, not disconnected landmasses
        // needing a bridge built between them). No clears-needed counter, no driftwood reward
        // either (the old SerpentClearReward was explicitly retired alongside the construction
        // gate, not retuned — the cove unlock itself is the reward). Re-beating an
        // already-defeated mini-boss (shouldn't happen once CanAttemptFight gates it, but kept
        // safe) does nothing further. Finite coves (0-2) only — the endless cove uses
        // RegisterSerpentLevelUp instead.
        public void RegisterMiniBossDefeat()
        {
            int cove = State.coveIndex;
            if (State.coveMinibossDefeated[cove])
            {
                RaiseChanged();
                return;
            }

            State.coveMinibossDefeated[cove] = true;
            State.totalClears++;

            int previousCove = State.coveIndex;
            State.coveIndex++;
            State.bossHpRemaining = -1; // fresh boss encounter for the new cove
            State.fightCooldownSeconds = 0f;

            if (IsEndlessCove(State.coveIndex))
            {
                State.serpentLevel = 1; // first-ever arrival at the endless cove
                State.reachedEndlessCove = true; // reveals Captain's Log (MenuDrawerController)
            }

            RaiseChanged();
            OnCoveUnlocked?.Invoke(previousCove, State.coveIndex);
        }

        // Cove 4's serpent never stays "defeated" (CORE_PROGRESSION_RESTRUCTURE.md "Cove 4's
        // serpent") — beating it at the current level pays a reward, bumps the persistent
        // serpentLevel counter, and re-arms the HP sentinel so the next attempt (no cooldown
        // required — see EndFightAttempt) starts fresh against the next, tougher level.
        public void RegisterSerpentLevelUp()
        {
            double reward = GameFormulas.SerpentLevelClearReward(EffectiveSerpentLevel);
            State.driftwood += reward;
            State.totalEarnedAllTime += reward;
            State.totalClears++;

            State.serpentLevel = EffectiveSerpentLevel + 1;
            State.bossHpRemaining = -1; // sentinel: next BeginFightAttempt arms the new level's full HP
            RaiseChanged();
        }

        // --- Cove Buildings (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings" — a separate,
        // optional wealth sink with NO bearing on cove-unlock progression, unlike the removed
        // construction gate above) -------------------------------------------------------

        public int GetBuildingStage(string id) => State.buildings.Find(b => b.id == id)?.stage ?? 0;

        private BuildingState GetOrCreateBuildingState(string id)
        {
            var b = State.buildings.Find(x => x.id == id);
            if (b == null)
            {
                b = new BuildingState { id = id, stage = 0 };
                State.buildings.Add(b);
            }
            return b;
        }

        // Sum of tap-power bonuses from every stage already paid, across every building —
        // additive-within-the-tree per EXPANDED_UPGRADES_AND_BALANCE.md's rule, folded into
        // TapPower above. Only the Hut exists today (CoveBuildingCatalog), so this is
        // effectively just the Hut's own paid stages for now.
        public double BuildingTapPowerBonusSum
        {
            get
            {
                double sum = 0;
                foreach (var b in State.buildings)
                {
                    var def = CoveBuildingCatalog.Find(b.id);
                    if (def == null) continue;
                    for (int i = 0; i < b.stage && i < def.stages.Length; i++)
                        sum += def.stages[i].tapPowerBonusPercent;
                }
                return sum;
            }
        }

        // A building's cove must actually be reached before it's payable at all — "the world
        // itself stays silent until something is actually earned" doesn't apply to a cove the
        // player hasn't even arrived at yet.
        public bool IsBuildingCoveReached(string id)
        {
            var def = CoveBuildingCatalog.Find(id);
            return def != null && State.coveIndex >= def.coveIndex;
        }

        public bool CanAffordNextBuildingStage(string id)
        {
            var def = CoveBuildingCatalog.Find(id);
            if (def == null || !IsBuildingCoveReached(id)) return false;
            int stage = GetBuildingStage(id);
            if (stage >= def.stages.Length) return false; // already at max stage
            return State.driftwood >= def.stages[stage].cost;
        }

        public void PayNextBuildingStage(string id)
        {
            if (!CanAffordNextBuildingStage(id)) return;
            var def = CoveBuildingCatalog.Find(id);
            int stageIndex = GetBuildingStage(id); // 0-based index into def.stages for the stage about to be paid

            State.driftwood -= def.stages[stageIndex].cost;
            var b = GetOrCreateBuildingState(id);
            b.stage++;
            RaiseChanged();
            OnBuildingStageCompleted?.Invoke(id, b.stage);
        }

        // --- Artifacts (NEXT_CLAUDE_CODE_PUSH.md §1 — account-wide, NOT per-cove; the
        // deliberate split from Cove Buildings above, which are local/per-cove) -----------

        private ShardStack GetOrCreateShardStack(string tier)
        {
            var s = State.shardStacks.Find(x => x.tier == tier);
            if (s == null)
            {
                s = new ShardStack { tier = tier };
                State.shardStacks.Add(s);
            }
            return s;
        }

        public int GetUnappraisedShardCount(string tier) => State.shardStacks.Find(s => s.tier == tier)?.unappraisedCount ?? 0;
        public int GetAppraisedShardCount(string tier) => State.shardStacks.Find(s => s.tier == tier)?.appraisedCount ?? 0;

        // Menu -> Artifacts entry stays hidden entirely until this is true (§1b's
        // zero-presence-until-earned rule, same spirit as Cove Buildings' world objects).
        public bool HasFoundAnyShard => State.shardStacks.Exists(s => s.unappraisedCount > 0 || s.appraisedCount > 0);

        // Called by TellSpot.Collect() — the acquisition-side entry point.
        public void AddCompassShard(string tier)
        {
            GetOrCreateShardStack(tier).unappraisedCount++;
            RaiseChanged();
            OnCompassShardFound?.Invoke(tier);
        }

        // Free/instant — the doc doesn't specify an appraisal cost, only that it's a manual
        // browse-and-appraise step (Menu -> Artifacts panel).
        public void AppraiseShard(string tier)
        {
            var s = GetOrCreateShardStack(tier);
            if (s.unappraisedCount <= 0) return;
            s.unappraisedCount--;
            s.appraisedCount++;
            RaiseChanged();
        }

        public bool IsArtifactNodeUnlocked(string id)
        {
            var def = ArtifactNodeCatalog.Find(id);
            return def != null && SerpentLevel >= def.requiredSerpentLevel;
        }

        public bool IsArtifactNodePurchased(string id) => State.artifactNodes.Find(n => n.id == id)?.purchased ?? false;

        public bool CanPurchaseArtifactNode(string id)
        {
            var def = ArtifactNodeCatalog.Find(id);
            if (def == null || IsArtifactNodePurchased(id) || !IsArtifactNodeUnlocked(id)) return false;
            return GetAppraisedShardCount(def.shardTier) >= def.shardCost;
        }

        public void PurchaseArtifactNode(string id)
        {
            if (!CanPurchaseArtifactNode(id)) return;
            var def = ArtifactNodeCatalog.Find(id);

            GetOrCreateShardStack(def.shardTier).appraisedCount -= def.shardCost;
            var n = State.artifactNodes.Find(x => x.id == id);
            if (n == null)
            {
                n = new ArtifactNodeState { id = id };
                State.artifactNodes.Add(n);
            }
            n.purchased = true;
            RaiseChanged();
        }

        // Sum of every purchased node's tap-power bonus — additive-within-the-tree per
        // EXPANDED_UPGRADES_AND_BALANCE.md's rule, folded into TapPower above as its own
        // separate multiplicative factor from Cove Buildings'.
        public double ArtifactTapPowerBonusSum
        {
            get
            {
                double sum = 0;
                foreach (var n in State.artifactNodes)
                {
                    if (!n.purchased) continue;
                    var def = ArtifactNodeCatalog.Find(n.id);
                    if (def != null) sum += def.tapPowerBonusPercent;
                }
                return sum;
            }
        }

        // Runtime-only registry of which coves currently have a live Artifacts tell — NOT
        // persisted (see TellSpawner.cs's class comment for why), purely for the fast-travel
        // ribbon badge (NEXT_CLAUDE_CODE_PUSH.md §1a). TellSpot/TellSpawner are the only
        // callers of the setter.
        private readonly HashSet<int> _covesWithLiveTell = new HashSet<int>();
        public bool CoveHasLiveTell(int coveIndex) => _covesWithLiveTell.Contains(coveIndex);
        public void SetCoveTellLive(int coveIndex, bool live)
        {
            bool changed = live ? _covesWithLiveTell.Add(coveIndex) : _covesWithLiveTell.Remove(coveIndex);
            if (changed) RaiseChanged();
        }

        // --- Onboarding (CORE_PROGRESSION_RESTRUCTURE.md "Onboarding / tutorial system") --

        public bool HasSeenOnboarding(string id) => State.seenOnboardingIds.Contains(id);

        public void MarkOnboardingSeen(string id)
        {
            if (State.seenOnboardingIds.Contains(id)) return;
            State.seenOnboardingIds.Add(id);
            RaiseChanged();
        }

        private void RaiseChanged() => OnStateChanged?.Invoke();
    }
}
