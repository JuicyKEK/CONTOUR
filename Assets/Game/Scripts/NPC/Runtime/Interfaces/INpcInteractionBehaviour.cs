using Game.Scripts.NPC.Runtime.Controllers;

namespace Game.Scripts.NPC.Runtime.Interfaces
{
    /// <summary>
    /// Стратегия реакции бота на взаимодействие игрока (кнопка E/аналог IInteraction).
    /// Разные боты вешают разные реализации этого интерфейса рядом с
    /// NpcController (или не вешают вообще, если у бота нет взаимодействия) -
    /// сам NpcController ничего не знает про документы/диалоги/торговлю и т.п.,
    /// он только предоставляет "шасси" (PlayAnimationSound, SetAggressive,
    /// ForceChase и т.д.), которым эти поведения могут пользоваться.
    ///
    /// Примеры реализаций: NpcDocumentCheckBehaviour (проверка документов),
    /// диалоговое поведение, "дать предмет", "разбудить" и т.д. - каждое в
    /// своём маленьком классе, без раздувания NpcController и без наследования
    /// от него.
    /// </summary>
    public interface INpcInteractionBehaviour
    {
        void OnPlayerInteracted(NpcController controller);
    }
}


