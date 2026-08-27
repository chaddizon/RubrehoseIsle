using System;
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
    // Several OnEnable() methods (ConstructionGate, MenuDrawerController,
    // PersistentHUDController) read GameManager.Instance and NullReferenceException
    // without this.
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerState State { get; private set; }
        public double LastOfflineEarnings { get; private set; }

        public event Action OnStateChanged;

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

        public double TapPower => GameFormulas.SalvagePower(State.tapLevel);

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

        public double CoveHp => GameFormulas.SerpentHp(WreckBeachData.BiomeIndex, State.coveIndex);
        public double CoveArmor => GameFormulas.SerpentArmor(WreckBeachData.BiomeIndex, State.coveIndex);
        public bool CoveMinibossDefeated => State.coveMinibossDefeated[State.coveIndex];

        // HP persists in State.bossHpRemaining across attempts; falls back to full HP
        // before the first-ever attempt on this cove (sentinel -1).
        public double BossHpRemaining => State.bossHpRemaining < 0 ? CoveHp : State.bossHpRemaining;

        // No clears-needed counter, no attempt limit — the only gate is this cooldown
        // (started on retreat/timeout, never on defeat) and whether the boss is already dead.
        public bool CanAttemptFight => !CoveMinibossDefeated && State.fightCooldownSeconds <= 0f;

        public void BeginFightAttempt()
        {
            if (State.bossHpRemaining < 0) State.bossHpRemaining = CoveHp; // first-ever encounter this cove
        }

        // Damage always applies (Attack() itself already refuses to call this when
        // tap power can't overcome armor). Defeat is detected here so HP and the
        // defeated flag can never drift apart.
        public void ApplyFightDamage(double dmg)
        {
            State.bossHpRemaining = Math.Max(0, State.bossHpRemaining - dmg);
            if (State.bossHpRemaining <= 0) RegisterMiniBossDefeat();
            else RaiseChanged();
        }

        // Called on retreat or fight-timer timeout — never on defeat. Starts the real
        // 20-minute cooldown; HP already persisted via ApplyFightDamage, so nothing here.
        public void EndFightAttempt()
        {
            if (!CoveMinibossDefeated) State.fightCooldownSeconds = GameFormulas.FightCooldownSeconds;
            RaiseChanged();
        }

        // Winning just flips the current cove's flag and reveals its construction gate —
        // no clears-needed counter. Re-beating an already-defeated mini-boss (shouldn't
        // happen once CanAttemptFight gates it, but kept safe) pays no second reward.
        public void RegisterMiniBossDefeat()
        {
            int cove = State.coveIndex;
            if (State.coveMinibossDefeated[cove])
            {
                RaiseChanged();
                return;
            }

            State.coveMinibossDefeated[cove] = true;
            State.coveConstructionRevealed[cove] = true; // reveal happens the same beat as defeat in this slice
            State.totalClears++;

            double reward = GameFormulas.SerpentClearReward(WreckBeachData.BiomeIndex);
            State.driftwood += reward;
            State.totalEarnedAllTime += reward;
            RaiseChanged();
        }

        // --- Construction gate (revealed per-cove once its mini-boss falls) -------

        private bool IsFinalCove(int cove) => cove >= WreckBeachData.CoveNames.Length - 1;

        // For a past cove, "complete" is just having moved beyond it; the biome's FINAL
        // cove instead uses constructionComplete, since its crossing unlocks the next
        // BIOME rather than advancing coveIndex further.
        public bool IsCoveConstructionComplete(int cove) =>
            IsFinalCove(cove) ? State.constructionComplete : State.coveIndex > cove;

        public bool ConstructionUnlocked => State.coveConstructionRevealed[State.coveIndex];

        public double ConstructionCost => GameFormulas.ConstructionCost(WreckBeachData.BiomeIndex);

        public bool CanBuildConstruction =>
            ConstructionUnlocked && !IsCoveConstructionComplete(State.coveIndex) && State.driftwood >= ConstructionCost;

        public void BuildConstruction()
        {
            if (!CanBuildConstruction) return;
            State.driftwood -= ConstructionCost;

            if (IsFinalCove(State.coveIndex))
            {
                State.constructionComplete = true;
                State.biomeUnlocked = Math.Max(State.biomeUnlocked, WreckBeachData.BiomeIndex + 1);
            }
            else
            {
                State.coveIndex++;
                State.bossHpRemaining = -1; // fresh boss encounter for the new cove
                State.fightCooldownSeconds = 0f;
            }
            RaiseChanged();
        }

        private void RaiseChanged() => OnStateChanged?.Invoke();
    }
}
