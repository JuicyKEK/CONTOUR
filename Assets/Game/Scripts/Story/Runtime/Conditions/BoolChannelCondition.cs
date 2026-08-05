using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Условие перехода, проверяющее ЖИВОЕ bool-состояние произвольного
    /// источника через StoryBoolChannelSO - подходит для чего угодно (двери,
    /// рубильники, шкалы, любые флаги любых скриптов), а не только для дверей.
    /// Само условие ничего не знает о конкретном типе источника - оно только
    /// читает канал; кто и как заполняет канал реальным значением - см.
    /// StoryBoolChannelListener / DoorOpenStateToBoolChannelBridge.
    ///
    /// ВАЖНО: это именно условие (StoryCondition-ассет), которое кладётся в
    /// поле StoryBranch.Condition. StoryBoolChannelSO сам по себе туда класть
    /// нельзя - он не наследует StoryCondition, это просто "почтовый ящик"
    /// со значением. Правильная схема:
    ///   1) Создать ассет StoryBoolChannelSO (Create -> Story/Events/Bool State Channel).
    ///   2) Создать ассет BoolChannelCondition (Create -> Story/Conditions/Bool Channel State),
    ///      указать в нём этот же StoryBoolChannelSO и нужное ExpectedValue.
    ///   3) В StoryBranch.Condition указать именно этот BoolChannelCondition,
    ///      а не сам канал.
    ///
    /// Мгновенная проверка при входе в ноду + дальнейшее ожидание изменения -
    /// это одно и то же действие: StoryBoolChannelSO.Subscribe() сразу
    /// синхронно отдаёт текущее значение, а затем - каждое следующее изменение.
    /// Если оно уже совпадает с ExpectedValue - условие завершается мгновенно;
    /// если нет - продолжает ждать следующего SetValue с нужным значением.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Bool Channel State", fileName = "BoolChannelCondition")]
    public class BoolChannelCondition : StoryCondition
    {
        [SerializeField] private StoryBoolChannelSO m_Channel;
        [SerializeField] private bool m_ExpectedValue = true;

        public override async UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            if (m_Channel == null)
            {
                Debug.LogWarning($"{nameof(BoolChannelCondition)} '{name}': канал не назначен.");
                return;
            }

            var completionSource = new UniTaskCompletionSource();

            void Handler(bool value)
            {
                if (value == m_ExpectedValue)
                {
                    completionSource.TrySetResult();
                }
            }

            using var subscription = m_Channel.Subscribe(Handler);

            using (token.Register(() => completionSource.TrySetCanceled(token)))
            {
                await completionSource.Task;
            }
        }
    }
}

