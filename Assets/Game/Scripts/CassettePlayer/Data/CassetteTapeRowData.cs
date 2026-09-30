namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Данные одной строки кассеты для построения списка (см. <see cref="CassettePageView.Build"/>).
    /// </summary>
    public readonly struct CassetteTapeRowData
    {
        public readonly string TapeId;
        public readonly string DisplayName;
        public readonly bool IsFound;
        public readonly bool IsListened;
        public readonly bool IsEvil;
        public readonly bool IsCompleted;

        public CassetteTapeRowData(string tapeId, string displayName, bool isFound, bool isListened, bool isEvil,
            bool isCompleted)
        {
            TapeId = tapeId;
            DisplayName = displayName;
            IsFound = isFound;
            IsListened = isListened;
            IsEvil = isEvil;
            IsCompleted = isCompleted;
        }
    }
}
