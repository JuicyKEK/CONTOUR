using System;
using Game.Scripts.InputController;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Дефолтная сборка StoryContext: каждая зависимость резолвится опционально.
    /// Флаги ниже позволяют пометить те из них, без которых сцена реально не работает -
    /// только для них будет ошибка в консоли. Всё остальное спокойно остаётся null
    /// (например, на тестовой сцене нет ICameraDirector или IScreenFader).
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
        [SerializeField] private bool m_RequireAudioTapes;

        public virtual StoryContext Build(GBSDependencyResolver resolver)
        {
            resolver ??= new GBSDependencyResolver();

            var playerControlHandles = resolver.ResolveAll<IPlayerControlHandle>();

            if (m_RequirePlayerControlHandles && playerControlHandles.Count == 0)
            {
                Debug.LogError("[GBS] Required dependency 'IPlayerControlHandle' is not found in the scene.");
            }

            return new StoryContext(
                resolver.Resolve<IInputActions>(m_RequireInputActions),
                resolver.Resolve<IInputSelectionActions>(m_RequireInputSelectionActions),
                playerControlHandles,
                resolver.Resolve<IStoryHintView>(m_RequireHintView),
                resolver.Resolve<ICutsceneDirector>(m_RequireCutsceneDirector),
                resolver.Resolve<ICameraDirector>(m_RequireCameraDirector),
                resolver.Resolve<IScreenFader>(m_RequireScreenFader),
                resolver.Resolve<IAudioTapeFoundRegistry>(m_RequireAudioTapes));
        }
    }
}

