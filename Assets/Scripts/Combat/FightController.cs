using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rubrehose.Core;
using Rubrehose.Data;

namespace Rubrehose.Combat
{
    // Drives the 30s serpent fight modal (GAME_DESIGN.md "Fight Duration = 30 seconds base").
    // Attach to the fight modal root and wire the fields below in the Inspector;
    // hook Attack()/Retreat() up to the modal's buttons.
    public class FightController : MonoBehaviour
    {
        [Header("UI refs (wire in Inspector)")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TMP_Text serpentNameText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text logText;

        private double _hp;
        private double _hpMax;
        private double _armor;
        private float _timeLeft;
        private bool _active;

        public void OpenFight()
        {
            var gm = GameManager.Instance;
            _hpMax = gm.CoveHp;
            _hp = _hpMax;
            _armor = gm.CoveArmor;
            _timeLeft = GameFormulas.FightDurationSeconds;
            _active = true;

            serpentNameText.text = WreckBeachData.SerpentNames[gm.State.coveIndex];
            statsText.text = $"HP {Format.Number(_hpMax)} · Armor {Format.Number(_armor)}";
            hpSlider.value = 1f;
            timerText.text = $"{_timeLeft:F1}s";
            logText.text = string.Empty;
            modalRoot.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;

            _timeLeft -= Time.deltaTime;
            timerText.text = $"{Mathf.Max(0f, _timeLeft):F1}s";

            if (_timeLeft <= 0f)
            {
                _active = false;
                logText.text = "Out of time — try again!";
                Invoke(nameof(CloseFight), 1.2f);
            }
        }

        public void Attack()
        {
            if (!_active) return;

            double dmg = GameManager.Instance.TapPower - _armor;
            if (dmg <= 0)
            {
                logText.text = "Your damage can't overcome its armor yet!";
                return;
            }

            _hp -= dmg;
            hpSlider.value = Mathf.Clamp01((float)(_hp / _hpMax));

            if (_hp <= 0)
            {
                _active = false;
                logText.text = "Defeated! Clear recorded.";
                GameManager.Instance.RegisterSerpentClear();
                Invoke(nameof(CloseFight), 1.0f);
            }
        }

        public void Retreat()
        {
            _active = false;
            CancelInvoke(nameof(CloseFight));
            CloseFight();
        }

        private void CloseFight() => modalRoot.SetActive(false);
    }
}
