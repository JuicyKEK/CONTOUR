using GBS.Data;

namespace GBS.Runtime
{
    /// <summary>
    /// Источник информации о завершённости графов (реализует GBSStarter).
    /// Нужен ноде "Graph Completed", чтобы стартовать один граф после другого.
    /// </summary>
    public interface IGBSGraphProgressProvider
    {
        bool IsGraphCompleted(GBSGraphSO graph);
    }
}

