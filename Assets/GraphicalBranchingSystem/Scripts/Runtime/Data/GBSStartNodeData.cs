using System;

namespace GBS.Data
{
    /// <summary>
    /// Точка входа графа. Если к булеву порту "gate" ничего не подключено -
    /// граф стартует сразу (через GBSStarter). Если подключено - граф ждёт,
    /// пока выражение станет истинным (например, нода "Graph Completed").
    /// </summary>
    [Serializable]
    public class GBSStartNodeData : GBSNodeData
    {
        public override GBSNodeType NodeType => GBSNodeType.Start;
    }
}
