using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Scripts.Inventory.Controllers
{
    /// <summary>
    /// Грузит иконки предметов инвентаря из Addressables. Каждая иконка грузится один раз и держится, пока жив загрузчик
    /// (лежит на игроке рядом с InventoryViewController); при его уничтожении всё загруженное отпускается.
    /// </summary>
    [JDIMonoController]
    public class InventoryIconLoader : MonoBehaviour, IInventoryIconLoader
    {
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> m_Handles = new();

        public async UniTask<Sprite> LoadIconAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            if (!m_Handles.TryGetValue(address, out var handle))
            {
                handle = Addressables.LoadAssetAsync<Sprite>(address);
                m_Handles.Add(address, handle);
            }

            try
            {
                return await handle.ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(InventoryIconLoader)}] Иконка '{address}' не загрузилась из Addressables: " +
                                 exception.Message, this);
                Release(address);
                return null;
            }
        }

        private void Release(string address)
        {
            if (m_Handles.Remove(address, out var handle) && handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        private void OnDestroy()
        {
            foreach (var handle in m_Handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            m_Handles.Clear();
        }
    }
}
