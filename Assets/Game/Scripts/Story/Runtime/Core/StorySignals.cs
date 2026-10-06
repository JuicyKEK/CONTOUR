using JuicyDI;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Доступ к состоянию сюжета (<see cref="IStoryState"/>) из геймплейного кода, которому сюжет
    /// не обязателен: на сцене может не быть GBSStarter (тестовые сцены, старый StoryManager) -
    /// тогда вызовы просто ничего не делают. Компоненты, которые без сюжета не имеют смысла,
    /// получают IStoryState через [Inject].
    /// </summary>
    public static class StorySignals
    {
        /// <summary>
        /// Префикс ключей, которые старые SO-каналы/действия получают автоматически
        /// (например "Legacy/StoryEvent_1.3") - мост на время переезда со SO на ключи.
        /// </summary>
        public const string LegacyPrefix = "Legacy/";

        public static IStoryState Current => BinController.GetContext()?.GetBean<IStoryState>();

        public static void Raise(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Current?.RaiseSignal(key);
            }
        }

        public static void SetFlag(string key, bool value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Current?.SetFlag(key, value);
            }
        }

        public static string LegacyKey(UnityEngine.Object asset)
        {
            return asset != null ? LegacyPrefix + asset.name : null;
        }
    }
}
