using System.Collections.Generic;
using Game.Scripts.InputController;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Набор зависимостей и "доска состояний" (blackboard), которые прокидываются
    /// в каждый StoryCondition/StoryAction при вызове. StoryManager собирает
    /// зависимости через JuicyDI и передаёт этот контекст дальше по SO-графу,
    /// поэтому сами SO не должны и не могут напрямую резолвить бины JDI.
    /// </summary>
    public class StoryContext
    {
        public IInputActions InputActions { get; }
        public IInputSelectionActions InputSelectionActions { get; }
        public IReadOnlyList<IPlayerControlHandle> PlayerControlHandles { get; }
        public IStoryHintView HintView { get; }
        public ICutsceneDirector CutsceneDirector { get; }
        public ICameraDirector CameraDirector { get; }
        public IScreenFader ScreenFader { get; }

        private readonly Dictionary<string, object> m_Blackboard = new();

        public StoryContext(
            IInputActions inputActions,
            IInputSelectionActions inputSelectionActions,
            IReadOnlyList<IPlayerControlHandle> playerControlHandles,
            IStoryHintView hintView,
            ICutsceneDirector cutsceneDirector,
            ICameraDirector cameraDirector,
            IScreenFader screenFader)
        {
            InputActions = inputActions;
            InputSelectionActions = inputSelectionActions;
            PlayerControlHandles = playerControlHandles;
            HintView = hintView;
            CutsceneDirector = cutsceneDirector;
            CameraDirector = cameraDirector;
            ScreenFader = screenFader;
        }

        public void SetControlEnabled(bool isEnabled)
        {
            if (PlayerControlHandles == null)
            {
                return;
            }

            for (int i = 0; i < PlayerControlHandles.Count; i++)
            {
                PlayerControlHandles[i]?.SetControlEnabled(isEnabled);
            }
        }

        public void SetFlag(string key, bool value)
        {
            m_Blackboard[key] = value;
        }

        public bool HasFlag(string key)
        {
            return m_Blackboard.ContainsKey(key);
        }

        public bool TryGetFlag(string key, out bool value)
        {
            if (m_Blackboard.TryGetValue(key, out var raw) && raw is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = false;
            return false;
        }
    }
}

