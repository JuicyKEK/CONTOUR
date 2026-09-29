namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Данные одной строки кассеты для построения списка (см. <see cref="CassettePlayerView.BuildSections"/>).
    /// </summary>
    public readonly struct CassetteTapeRowData
    {
        public readonly string TapeId;
        public readonly string DisplayName;
        public readonly bool IsFound;
        public readonly bool IsEvil;

        public CassetteTapeRowData(string tapeId, string displayName, bool isFound, bool isEvil)
        {
            TapeId = tapeId;
            DisplayName = displayName;
            IsFound = isFound;
            IsEvil = isEvil;
        }
    }
}