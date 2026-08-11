using System;
using UnityEngine;
using Rubrehose.Data;

namespace Rubrehose.Core
{
    // Central game-state owner for the Wreck Beach vertical slice.
    // UI scripts subscribe to OnStateChanged and call the public methods below;
    // nothing here reaches into UI directly, so scene wiring is entirely Chad's side.
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

        private void AddDriftwood(double amount)
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

        // --- Cove / serpent ------------------------------------------------

        public double CoveHp => GameFormulas.SerpentHp(WreckBeachData.BiomeIndex, State.coveIndex);
        public double CoveArmor => GameFormulas.SerpentArmor(WreckBeachData.BiomeIndex, State.coveIndex);
        public int ClearsNeeded => GameFormulas.ClearsNeeded(WreckBeachData.BiomeIndex, State.coveIndex);

        public void RegisterSerpentClear()
        {
            State.coveClears++;
            State.totalClears++;

            double reward = GameFormulas.SerpentClearReward(WreckBeachData.BiomeIndex);
            State.driftwood += reward;
            State.totalEarnedAllTime += reward;

            bool isLastCove = State.coveIndex >= WreckBeachData.CoveNames.Length - 1;
            if (State.coveClears >= ClearsNeeded && !isLastCove)
            {
                State.coveIndex++;
                State.coveClears = 0;
            }
            RaiseChanged();
        }

        // --- Construction gate ---------------------------------------------

        public bool ConstructionUnlocked =>
            State.coveIndex == WreckBeachData.CoveNames.Length - 1 && State.coveClears >= ClearsNeeded;

        public double ConstructionCost => GameFormulas.ConstructionCost(WreckBeachData.BiomeIndex);

        public bool CanBuildConstruction =>
            ConstructionUnlocked && !State.constructionComplete && State.driftwood >= ConstructionCost;

        public void BuildConstruction()
        {
            if (!CanBuildConstruction) return;
            State.driftwood -= ConstructionCost;
            State.constructionComplete = true;
            RaiseChanged();
        }

        private void RaiseChanged() => OnStateChanged?.Invoke();
    }
}
