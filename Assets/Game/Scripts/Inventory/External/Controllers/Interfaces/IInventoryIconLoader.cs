using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Inventory.External.Controllers.Interfaces
{
    /// <summary>
    /// Иконки предметов инвентаря по ключу в Addressables.
    /// </summary>
    public interface IInventoryIconLoader
    {
        /// <summary>
        /// Иконка по ключу (адресу). Пустой ключ или иконки с таким ключом нет - null.
        /// </summary>
        UniTask<Sprite> LoadIconAsync(string address, CancellationToken cancellationToken);
    }
}
