using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using Game.Scripts.Utilities.Save;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Чекпоинт: сохраняет игру через общий контроллер сохранений (GameSaveController) - все механики,
    /// включая прогресс сюжета. Прогресс графа запоминается на ноде с этим действием: после загрузки
    /// сюжет продолжится с этой ноды и заново выполнит её действия. Поэтому удобно ставить сохранение
    /// первым действием отдельной ноды-чекпоинта.
    /// </summary>
    [Serializable, GBSMenu("Save/Save Game")]
    public class SaveGame : GBSAction
    {
        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var saveService = context.Resolve<IGameSaveService>();

            if (saveService == null)
            {
                Debug.LogWarning("[GBS] Save Game: на сцене нет GameSaveController - игра не сохранена.");
                return UniTask.CompletedTask;
            }

            saveService.SaveGame();
            return UniTask.CompletedTask;
        }
    }
}
