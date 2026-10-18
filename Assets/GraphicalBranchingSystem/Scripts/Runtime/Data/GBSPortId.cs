namespace GBS.Data
{
    /// <summary>
    /// Идентификаторы портов нод. Связи (GBSEdgeData) сохраняются как
    /// пара (NodeId, PortId), поэтому идентификаторы обязаны быть стабильными.
    /// </summary>
    public static class GBSPortId
    {
        public const string In = "in";
        public const string Out = "out";
        public const string Gate = "gate";

        /// <summary>Flow-выход ноды, которая умеет стоять внутри потока сюжета (Graph Completed).</summary>
        public const string FlowOut = "flowOut";

        public const string ConditionSuffix = "#cond";
        public const string LogicInputPrefix = "in";

        public static string BranchCondition(string branchId)
        {
            return branchId + ConditionSuffix;
        }

        public static string LogicInput(int index)
        {
            return LogicInputPrefix + index;
        }
    }

    /// <summary>
    /// Маркерный тип для flow-портов. Используется только как "тип порта"
    /// в GraphView, чтобы flow нельзя было соединить с булевым портом.
    /// </summary>
    public sealed class GBSFlow
    {
    }
}


