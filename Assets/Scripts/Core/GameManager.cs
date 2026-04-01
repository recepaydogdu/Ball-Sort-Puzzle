using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BallSort.Core;
using BallSort.Level;

namespace BallSort
{
    /// <summary>
    /// Oyun akışının merkezi orkestratörü (Singleton + DontDestroyOnLoad).
    /// Sorumluluklar: seviye kurma, hamle doğrulama, undo yönetimi, kazanma koşulu.
    /// UI ve seviye kataloğu bu sınıfın kapsamı dışındadır.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ─── Sabitler ───────────────────────────────────────────────
        private const float TubeSpacing = 1.4f;

        // ─── Inspector Alanları ─────────────────────────────────────
        [Header("Bağımlılıklar")]
        [SerializeField] private GameObject       _tubePrefab;
        [SerializeField] private Transform        _tubeContainer;
        [SerializeField] private BallColorPalette _colorPalette;

        [Header("Başlangıç Seviyesi (opsiyonel)")]
        [SerializeField] private LevelData _startLevel;

        // ─── Runtime State ──────────────────────────────────────────
        private Tube[]            _tubes;
        private int               _selectedIndex = -1;
        private Stack<MoveRecord> _undoStack;
        private int               _moveCount;
        private bool              _levelComplete;

        // ─── Events ─────────────────────────────────────────────────

        /// <summary>Hamle sayısı her değiştiğinde ateşlenir (yeni değer parametre).</summary>
        public event Action<int> OnMoveCountChanged;

        /// <summary>Tüm tüpler çözüldüğünde ateşlenir (toplam hamle sayısı parametre).</summary>
        public event Action<int> OnLevelSolved;

        /// <summary>Yeni seviye yüklenip hazır olduğunda ateşlenir.</summary>
        public event Action OnLevelLoaded;

        // ─── Unity Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_startLevel != null)
                LoadLevel(_startLevel);
        }

        // ─── Seviye Yönetimi ────────────────────────────────────────

        /// <summary>
        /// Verilen LevelData'ya göre sahneyi sıfırlar ve yeni seviyeyi kurar.
        /// LevelManager veya doğrudan çağrılabilir.
        /// </summary>
        public void LoadLevel(LevelData levelData)
        {
            if (levelData == null) throw new ArgumentNullException(nameof(levelData));

            if (!levelData.Validate(out var error))
            {
                Debug.LogError($"[GameManager] Geçersiz level: {error}");
                return;
            }

            ClearExistingTubes();
            ResetState();
            SpawnTubes(levelData);

            OnLevelLoaded?.Invoke();
            OnMoveCountChanged?.Invoke(_moveCount);
        }

        private void ResetState()
        {
            _undoStack     = new Stack<MoveRecord>();
            _moveCount     = 0;
            _selectedIndex = -1;
            _levelComplete = false;
        }

        private void ClearExistingTubes()
        {
            if (_tubes == null) return;
            foreach (var tube in _tubes)
            {
                if (tube == null) continue;
                tube.OnTubeClicked -= HandleTubeClicked;
                Destroy(tube.gameObject);
            }
            _tubes = null;
        }

        private void SpawnTubes(LevelData levelData)
        {
            int total  = levelData.TotalTubeCount;
            _tubes     = new Tube[total];
            int filled = levelData.TubePlacements.Length;

            for (int i = 0; i < filled; i++)
            {
                var setup = levelData.TubePlacements[i];
                var data  = new TubeData(setup.balls ?? Array.Empty<BallColor>());
                _tubes[i] = CreateTubeObject(data, i, total);
            }

            for (int i = filled; i < total; i++)
                _tubes[i] = CreateTubeObject(new TubeData(), i, total);
        }

        private Tube CreateTubeObject(TubeData data, int index, int totalCount)
        {
            var go  = Instantiate(_tubePrefab, _tubeContainer);
            go.name = $"Tube_{index:00}";

            float totalWidth    = (totalCount - 1) * TubeSpacing;
            float xPos          = index * TubeSpacing - totalWidth / 2f;
            go.transform.localPosition = new Vector3(xPos, 0f, 0f);

            var tube = go.GetComponent<Tube>();
            tube.Initialize(data, _colorPalette);
            tube.OnTubeClicked += HandleTubeClicked;
            return tube;
        }

        // ─── Input İşleme ───────────────────────────────────────────

        private void HandleTubeClicked(Tube clickedTube)
        {
            if (_levelComplete) return;

            int clickedIndex = Array.IndexOf(_tubes, clickedTube);
            if (clickedIndex < 0) return;

            if (_selectedIndex < 0)
            {
                // İlk tıklama: boş tüp seçilemez
                if (_tubes[clickedIndex].GetData().IsEmpty()) return;
                SelectTube(clickedIndex);
            }
            else if (_selectedIndex == clickedIndex)
            {
                // Aynı tüpe tekrar tıklama → seçimi iptal et
                DeselectCurrent();
            }
            else
            {
                // İkinci tıklama → hamle dene
                bool moved = TryMove(_selectedIndex, clickedIndex);
                DeselectCurrent();

                if (!moved && !_tubes[clickedIndex].GetData().IsEmpty())
                    SelectTube(clickedIndex);
            }
        }

        private void SelectTube(int index)
        {
            _selectedIndex = index;
            _tubes[index].SetSelected(true);
        }

        private void DeselectCurrent()
        {
            if (_selectedIndex >= 0)
                _tubes[_selectedIndex].SetSelected(false);
            _selectedIndex = -1;
        }

        // ─── Hamle Mantığı ──────────────────────────────────────────

        /// <summary>
        /// from → to hamlesini dener.
        /// Kural ihlalinde tüpler değişmeden false döner.
        /// </summary>
        public bool TryMove(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex)) return false;

            var fromData = _tubes[fromIndex].GetData();
            var toData   = _tubes[toIndex].GetData();

            if (!fromData.TryPop(out var color)) return false;

            if (!toData.TryPush(color))
            {
                fromData.TryPush(color); // atomik geri alma
                return false;
            }

            _undoStack.Push(new MoveRecord(fromIndex, toIndex, color));
            _moveCount++;

            _tubes[fromIndex].RefreshVisuals();
            _tubes[toIndex].RefreshVisuals();

            OnMoveCountChanged?.Invoke(_moveCount);
            CheckWinCondition();
            return true;
        }

        /// <summary>
        /// Son hamleyi geri alır.
        /// Undo stack boşsa veya seviye tamamlandıysa işlem yapmaz.
        /// </summary>
        public void UndoLastMove()
        {
            if (_levelComplete || _undoStack.Count == 0) return;

            var record  = _undoStack.Pop().Reversed();
            var fromData = _tubes[record.FromTubeIndex].GetData();
            var toData   = _tubes[record.ToTubeIndex].GetData();

            // Ters hamle: to'dan al, from'a geri koy
            fromData.TryPop(out _);
            toData.TryPush(record.MovedColor);

            _tubes[record.FromTubeIndex].RefreshVisuals();
            _tubes[record.ToTubeIndex].RefreshVisuals();

            _moveCount = Mathf.Max(0, _moveCount - 1);
            OnMoveCountChanged?.Invoke(_moveCount);
        }

        /// <summary>Geri alınabilecek hamle var mı?</summary>
        public bool CanUndo() => !_levelComplete && _undoStack.Count > 0;

        /// <summary>Mevcut hamle sayısı.</summary>
        public int MoveCount => _moveCount;

        // ─── Kazanma Koşulu ─────────────────────────────────────────

        private void CheckWinCondition()
        {
            if (_tubes == null) return;
            bool allDone = _tubes.All(t => t.IsEmpty() || t.IsSolved());
            if (!allDone) return;

            _levelComplete = true;
            OnLevelSolved?.Invoke(_moveCount);
        }

        // ─── Yardımcılar ────────────────────────────────────────────

        private bool IsValidIndex(int i) => _tubes != null && i >= 0 && i < _tubes.Length;
    }
}
