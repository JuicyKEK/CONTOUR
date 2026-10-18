using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GBS.Steps
{
    /// <summary>
    /// Выполнено сразу. В переходе то же самое даёт пустое условие; нужно в ноде Condition
    /// (пустая нода Condition не срабатывает никогда) и внутри All Of / Any Of.
    /// </summary>
    [Serializable, GBSMenu("Flow/Always True")]
    public class AlwaysTrue : GBSCondition
    {
        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
    }
}
