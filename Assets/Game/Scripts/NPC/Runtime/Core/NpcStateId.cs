namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Идентификаторы состояний FSM бота. Сюжет и триггеры переключают состояния
    /// именно по этому enum (через публичные no-op методы NpcController -
    /// см. NpcController.ForceIdle()/ForcePatrol()/... для привязки из UnityEvent).
    /// </summary>
    public enum NpcStateId
    {
        Idle = 0,
        Observe = 1,
        Patrol = 2,
        Chase = 3,
        Panic = 4,

        /// <summary>
        /// Случайное блуждание по окрестностям без цели - в частности, бот
        /// попадает сюда сам, "сдавшись" перед запертой дверью во время
        /// преследования (см. NpcChaseState/NpcTypeDefinitionSO.LockedDoorGiveUpDelay).
        /// </summary>
        Wander = 5
    }
}

