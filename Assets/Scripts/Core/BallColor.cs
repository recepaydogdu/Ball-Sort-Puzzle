namespace BallSort.Core
{
    /// <summary>
    /// Oyundaki top renklerini tanımlayan enum.
    /// Değerler explicit int ile sabitleniştir; böylece save/load verisi
    /// enum sırası değişse bile bozulmaz.
    /// </summary>
    public enum BallColor
    {
        None   = 0,
        Red    = 1,
        Blue   = 2,
        Green  = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6,
        Teal   = 7,
        Pink   = 8
    }
}
