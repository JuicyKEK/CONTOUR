using System;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Сохраняет, что гнездо заражения разрослось (<see cref="IInfectionSpreadObject"/>, InfectionSpreadObject). Вешается
    /// на объект гнезда. После загрузки локации разросшееся гнездо сразу стоит разросшимся, без роста, и второй раз его
    /// не заразить. Заражение зоны сохраняется отдельно (InfectionZoneSaveController) и здесь не меняется.
    /// </summary>
    [JDIMonoController]
    public class InfectionSpreadStateSaver : LocationStateSaver
    {
        [Serializable]
        private class State
        {
            public bool IsSpread;
        }

        private readonly CompositeDisposable m_Disposables = new();
        private IInfectionSpreadObject m_Spread;

        private void Awake()
        {
            if (!TryGetComponent(out m_Spread))
            {
                Debug.LogError($"[LocationState] InfectionSpreadStateSaver '{name}': на объекте нет гнезда заражения " +
                               "(IInfectionSpreadObject: InfectionSpreadObject).", this);
                return;
            }

            m_Spread.IsSpread
                .Where(isSpread => isSpread)
                .Take(1)
                .Subscribe(_ => StoreStateNow())
                .AddTo(m_Disposables);
        }

        protected override void OnDestroy()
        {
            m_Disposables.Dispose();
            base.OnDestroy();
        }

        public override string CaptureState()
        {
            return m_Spread != null && m_Spread.IsSpread.CurrentValue
                ? JsonUtility.ToJson(new State { IsSpread = true })
                : null;
        }

        public override void ApplySavedState(string state)
        {
            if (m_Spread != null && JsonUtility.FromJson<State>(state).IsSpread)
            {
                m_Spread.RestoreSpread();
            }
        }
    }
}
