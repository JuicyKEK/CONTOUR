using System;
using Game.Scripts.InputController;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Дефолтная сборка StoryContext. Сервисы в контексте не запоминаются - он берёт их из JuicyDI
    /// в момент обращения (см. StoryContext), так что любая зависимость может отсутствовать.
    /// Флаги ниже позволяют пометить те из них, без которых сцена реально не работает: их наличие
    /// проверяется на старте, и только для них будет ошибка в консоли. Всё остальное спокойно
    /// остаётся null (например, на тестовой сцене нет ICameraDirector или IScreenFader).
    /// Любые другие сервисы действия получают сами через context.Resolve&lt;T&gt;().
    /// </summary>
    [Serializable]
    public class GBSStoryContextBuilder
    {
        [SerializeField] private bool m_RequireInputActions;
        [SerializeField] private bool m_RequireInputSelectionActions;
        [SerializeField] private bool m_RequirePlayerControlHandles;
        [SerializeField] private bool m_RequireHintView;
        [SerializeField] private bool m_RequireCutsceneDirector;
        [SerializeField] private bool m_RequireCameraDirector;
        [SerializeField] private bool m_RequireScreenFader;

        public virtual StoryContext Build(GBSDependencyResolver resolver, StoryState state)
        {
            resolver ??= new GBSDependencyResolver();

            CheckRequired(resolver);

            return new StoryContext(state, resolver);
        }

        private void CheckRequired(GBSDependencyResolver resolver)
        {
            Require<IInputActions>(resolver, m_RequireInputActions);
            Require<IInputSelectionActions>(resolver, m_RequireInputSelectionActions);
            Require<IStoryHintView>(resolver, m_RequireHintView);
            Require<ICutsceneDirector>(resolver, m_RequireCutsceneDirector);
            Require<ICameraDirector>(resolver, m_RequireCameraDirector);
            Require<IScreenFader>(resolver, m_RequireScreenFader);

            if (m_RequirePlayerControlHandles && resolver.ResolveAll<IPlayerControlHandle>().Count == 0)
            {
                Debug.LogError("[GBS] Required dependency 'IPlayerControlHandle' is not found in the scene.");
            }
        }

        private static void Require<T>(GBSDependencyResolver resolver, bool isRequired) where T : class
        {
            if (isRequired)
            {
                // Резолвер сам пишет ошибку, если бина нет.
                resolver.Resolve<T>(true);
            }
        }
    }
}
