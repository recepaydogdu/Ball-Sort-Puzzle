using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BallSort.Level;

namespace BallSort.UI
{
    /// <summary>
    /// Oyun içi HUD ve panel yönetimi.
    /// GameManager / LevelManager event'lerini UI elemanlarına yansıtır.
    /// Layout: Canvas → CanvasScaler (Scale with Screen Size) zorunludur.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // ─── Format Sabitleri ────────────────────────────────────────
        private const string FormatMoves    = "Hamleler: {0}";
        private const string FormatWinMoves = "{0} Hamlede Tamamlandı!";
        private const string FormatBest     = "En İyi: {0} Hamle";
        private const string TextNewRecord  = "Yeni Rekor!";
        private const string TextNoRecord   = "İlk Tamamlama";

        // ─── Runtime ────────────────────────────────────────────────
        // Level yüklendiğinde snapshot'lanan en iyi hamle.
        // HandleLevelSolved, LevelManager güncellemesinden önce veya sonra
        // tetiklenebileceğinden, karşılaştırmayı bu değer üzerinden yapıyoruz.
        private int _bestMovesAtLevelStart = -1;

        // ─── Inspector — HUD ────────────────────────────────────────
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI _levelNameText;
        [SerializeField] private TextMeshProUGUI _movesText;
        [SerializeField] private Button          _undoButton;
        [SerializeField] private Button          _restartButton;

        [Header("Kazanma Paneli")]
        [SerializeField] private GameObject      _winPanel;
        [SerializeField] private TextMeshProUGUI _winMovesText;
        [SerializeField] private TextMeshProUGUI _winBestText;
        [SerializeField] private Button          _nextLevelButton;
        [SerializeField] private Button          _winRestartButton;

        // ─── Unity Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            RegisterButtonListeners();
            SetWinPanelVisible(false);
        }

        private void OnEnable()  => SubscribeToGameEvents();
        private void OnDisable() => UnsubscribeFromGameEvents();

        // ─── Event Bağlantıları ─────────────────────────────────────

        private void SubscribeToGameEvents()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnMoveCountChanged += HandleMoveCountChanged;
            gm.OnLevelSolved      += HandleLevelSolved;
            gm.OnLevelLoaded      += HandleLevelLoaded;
        }

        private void UnsubscribeFromGameEvents()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnMoveCountChanged -= HandleMoveCountChanged;
            gm.OnLevelSolved      -= HandleLevelSolved;
            gm.OnLevelLoaded      -= HandleLevelLoaded;
        }

        private void RegisterButtonListeners()
        {
            _undoButton?.onClick.AddListener(OnUndoClicked);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            _nextLevelButton?.onClick.AddListener(OnNextLevelClicked);
            _winRestartButton?.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            // AddListener ile eklenen referanslar UnityEvent tarafından tutulur.
            // GameUI yıkılırken butonlar hayatta kalırsa çökme olur; temizle.
            _undoButton?.onClick.RemoveListener(OnUndoClicked);
            _restartButton?.onClick.RemoveListener(OnRestartClicked);
            _nextLevelButton?.onClick.RemoveListener(OnNextLevelClicked);
            _winRestartButton?.onClick.RemoveListener(OnRestartClicked);
        }

        // ─── Olay İşleyiciler ───────────────────────────────────────

        private void HandleLevelLoaded()
        {
            SetWinPanelVisible(false);
            UpdateMoves(GameManager.Instance?.MoveCount ?? 0);
            RefreshUndoButton();

            var lm = LevelManager.Instance;
            if (_levelNameText != null)
                _levelNameText.text = lm?.CurrentLevel?.LevelName ?? string.Empty;

            // LevelManager güncelleme yapmadan ÖNCE en iyi değeri sakla.
            // HandleLevelSolved'daki "Yeni Rekor" karşılaştırması buna dayanır.
            _bestMovesAtLevelStart = lm?.GetBestMoves(lm.CurrentIndex) ?? -1;
        }

        private void HandleMoveCountChanged(int moves)
        {
            UpdateMoves(moves);
            RefreshUndoButton();
        }

        private void HandleLevelSolved(int moves)
        {
            SetWinPanelVisible(true);

            if (_winMovesText != null)
                _winMovesText.text = string.Format(FormatWinMoves, moves);

            // _bestMovesAtLevelStart: level yüklendiğinde snapshot'lanan değer.
            // LevelManager bu event'te PlayerPrefs'i güncellemiş olabilir;
            // o yüzden GetBestMoves() yerine snapshot'ı kullanıyoruz.
            bool isFirstCompletion = _bestMovesAtLevelStart < 0;
            bool isNewRecord       = !isFirstCompletion && moves < _bestMovesAtLevelStart;

            if (_winBestText != null)
            {
                if (isFirstCompletion)
                    _winBestText.text = TextNoRecord;
                else if (isNewRecord)
                    _winBestText.text = TextNewRecord;
                else
                    _winBestText.text = string.Format(FormatBest, _bestMovesAtLevelStart);
            }

            // Son seviyedeyse "Sonraki" gizle
            var lm = LevelManager.Instance;
            bool hasNext = lm != null && lm.CurrentIndex + 1 < lm.LevelCount;
            _nextLevelButton?.gameObject.SetActive(hasNext);
        }

        // ─── Buton İşleyicileri ─────────────────────────────────────

        private void OnUndoClicked()   => GameManager.Instance?.UndoLastMove();

        private void OnRestartClicked() => LevelManager.Instance?.RestartCurrentLevel();

        private void OnNextLevelClicked()
        {
            if (LevelManager.Instance?.LoadNextLevel() == false)
                Debug.Log("[GameUI] Son seviyedesiniz.");
        }

        // ─── Yardımcılar ────────────────────────────────────────────

        private void UpdateMoves(int moves)
        {
            if (_movesText != null)
                _movesText.text = string.Format(FormatMoves, moves);
        }

        private void RefreshUndoButton()
        {
            if (_undoButton != null)
                _undoButton.interactable = GameManager.Instance?.CanUndo() ?? false;
        }

        private void SetWinPanelVisible(bool visible)
        {
            _winPanel?.SetActive(visible);
        }
    }
}
