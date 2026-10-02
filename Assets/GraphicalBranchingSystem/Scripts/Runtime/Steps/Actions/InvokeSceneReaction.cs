using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Вызывает реакции сцены с ключом (UnityEvent в StorySceneReactions на загруженных сценах):
    /// включить объекты, забрать управление, скомандовать NPC/монстрам и т.д.
    /// </summary>
    [Serializable, GBSMenu("Scene/Invoke Scene Reaction")]
    public class InvokeSceneReaction : GBSAction
    {
        [SerializeField, StoryKey(StoryKeyKind.Reaction)] private string m_Key;

        public InvokeSceneReaction()
        {
        }

        public InvokeSceneReaction(string key)
        {
            m_Key = key;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            int invokedCount = 0;

            foreach (var source in context.ResolveAll<IStorySceneReactionSource>())
            {
                invokedCount += source.InvokeReaction(m_Key);
            }

            if (invokedCount == 0)
            {
                Debug.LogWarning($"[GBS] Invoke Scene Reaction: на сцене нет реакций с ключом '{m_Key}' (StorySceneReactions).");
            }

            return UniTask.CompletedTask;
        }
    }
}
