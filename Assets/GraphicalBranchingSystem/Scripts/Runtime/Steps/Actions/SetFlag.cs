using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Записывает флаг в состояние сюжета (сохраняется). Типовое применение - запомнить выбор игрока
    /// ("TookNpcTask") и дальше ветвиться по нему условием "Flag Is" в этом или другом графе.
    /// </summary>
    [Serializable, GBSMenu("Story State/Set Flag")]
    public class SetFlag : GBSAction
    {
        [SerializeField, StoryKey(StoryKeyKind.Flag)] private string m_Key;
        [SerializeField] private bool m_Value = true;

        public SetFlag()
        {
        }

        public SetFlag(string key, bool value)
        {
            m_Key = key;
            m_Value = value;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            context.State.SetFlag(m_Key, m_Value);
            return UniTask.CompletedTask;
        }
    }
}
