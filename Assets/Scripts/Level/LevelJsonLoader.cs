using UnityEngine;

namespace BallSort.Level
{
    /// <summary>
    /// Resources/Levels/ klasöründeki JSON dosyalarından LevelData yükler.
    /// Unity Editor olmadan da çalışır; ScriptableObject asset oluşturmaz,
    /// runtime-only instance üretir.
    /// </summary>
    public static class LevelJsonLoader
    {
        private const string ResourcesFolder = "Levels";

        /// <summary>
        /// Resources/Levels/level_XX.json dosyasından LevelData yükler.
        /// Dosya bulunamazsa null döner.
        /// </summary>
        public static LevelData LoadLevel(int levelNumber)
        {
            string resourcePath = $"{ResourcesFolder}/{FormatFileName(levelNumber)}";
            var textAsset = Resources.Load<TextAsset>(resourcePath);

            if (textAsset == null)
            {
                Debug.LogWarning($"[LevelJsonLoader] Dosya bulunamadı: {resourcePath}");
                return null;
            }

            return ParseJson(textAsset.text);
        }

        /// <summary>
        /// Resources/Levels/ içindeki tüm level_XX.json dosyalarını yükler.
        /// Geçersiz dosyalar atlanır.
        /// </summary>
        public static LevelData[] LoadAllLevels()
        {
            var textAssets = Resources.LoadAll<TextAsset>(ResourcesFolder);
            var result     = new System.Collections.Generic.List<LevelData>(textAssets.Length);

            foreach (var asset in textAssets)
            {
                var data = ParseJson(asset.text);
                if (data != null) result.Add(data);
            }

            result.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            return result.ToArray();
        }

        // ─── Private ────────────────────────────────────────────────

        private static LevelData ParseJson(string json)
        {
            var model = JsonUtility.FromJson<LevelJsonModel>(json);
            if (model == null)
            {
                Debug.LogError("[LevelJsonLoader] JSON parse başarısız.");
                return null;
            }
            return LevelData.CreateFromJson(model);
        }

        private static string FormatFileName(int levelNumber) =>
            $"level_{levelNumber:D2}";
    }
}
