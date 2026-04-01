namespace BallSort.Core
{
    /// <summary>
    /// Tek bir hamleyi temsil eden değer tipi.
    /// Undo stack'inde readonly olarak saklanır; mutasyon yoktur.
    /// </summary>
    public readonly struct MoveRecord
    {
        public readonly int       FromTubeIndex;
        public readonly int       ToTubeIndex;
        public readonly BallColor MovedColor;

        public MoveRecord(int from, int to, BallColor color)
        {
            FromTubeIndex = from;
            ToTubeIndex   = to;
            MovedColor    = color;
        }

        /// <summary>Bu hamlenin tersini döner (undo için).</summary>
        public MoveRecord Reversed() =>
            new MoveRecord(ToTubeIndex, FromTubeIndex, MovedColor);

        public override string ToString() =>
            $"Move({MovedColor}: tube[{FromTubeIndex}]→tube[{ToTubeIndex}])";
    }
}
