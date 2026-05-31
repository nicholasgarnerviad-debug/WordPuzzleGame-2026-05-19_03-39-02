using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Modes;

namespace WordPuzzle.UI
{
    /// <summary>
    /// §5.3 — Time Attack setup screen. 2x2 button grid lets the player pick
    /// a (baseTimeSeconds, subMode) tuple before starting a Time Attack run.
    /// Fires OnConfigConfirmed with the resulting TimeAttackConfig.
    /// </summary>
    public class TimeAttackSetupScreen : MonoBehaviour
    {
        [SerializeField] private Button btn60Timed;
        [SerializeField] private Button btn60Survival;
        [SerializeField] private Button btn120Timed;
        [SerializeField] private Button btn120Survival;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;

        public event Action<TimeAttackConfig> OnConfigConfirmed;
        public event Action OnBackToMenu;

        private void OnEnable()
        {
            if (titleText != null) titleText.text = "TIME ATTACK";

            if (btn60Timed != null)
                btn60Timed.onClick.AddListener(() => Confirm(60f, TimeAttackSubMode.Timed));
            if (btn60Survival != null)
                btn60Survival.onClick.AddListener(() => Confirm(60f, TimeAttackSubMode.Survival));
            if (btn120Timed != null)
                btn120Timed.onClick.AddListener(() => Confirm(120f, TimeAttackSubMode.Timed));
            if (btn120Survival != null)
                btn120Survival.onClick.AddListener(() => Confirm(120f, TimeAttackSubMode.Survival));

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
                var lbl = backButton.GetComponentInChildren<TMP_Text>(true);
                if (lbl != null)
                {
                    lbl.text = "HOME";
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.fontSize = 28f;
                    lbl.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
                    lbl.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void OnDisable()
        {
            if (btn60Timed != null) btn60Timed.onClick.RemoveAllListeners();
            if (btn60Survival != null) btn60Survival.onClick.RemoveAllListeners();
            if (btn120Timed != null) btn120Timed.onClick.RemoveAllListeners();
            if (btn120Survival != null) btn120Survival.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// §5.3 — Build a TimeAttackConfig from the canonical factory then override
        /// the (baseTimeSeconds, subMode) tuple chosen by the player. Uses the live
        /// TimeAttackConfig API exposed by Assets/Scripts/Game/Modes/TimeAttackMode.cs.
        /// </summary>
        private void Confirm(float baseSeconds, TimeAttackSubMode sub)
        {
            // Start from the closest matching factory so addTime/survival defaults stay sane.
            TimeAttackConfig cfg;
            if (sub == TimeAttackSubMode.Survival)
            {
                cfg = TimeAttackConfig.DefaultSurvival();
            }
            else
            {
                cfg = Mathf.Approximately(baseSeconds, 120f)
                    ? TimeAttackConfig.Default120()
                    : TimeAttackConfig.Default60();
            }

            cfg.baseTimeSeconds = baseSeconds;
            cfg.subMode = sub;

            OnConfigConfirmed?.Invoke(cfg);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
