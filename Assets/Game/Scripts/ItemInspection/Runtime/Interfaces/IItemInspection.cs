using System;
using UnityEngine;

namespace Game.Scripts.ItemInspection
{
    /// <summary>
    /// Осмотр предмета (по примеру Resident Evil): предмет подлетает к камере, всё за ним размыто, снизу - название;
    /// зажав ЛКМ, его крутят; по Esc или кнопке "Принять" он уменьшается и улетает. Делает ItemInspectionController
    /// в префабе игрока.
    ///
    /// Модуль ничего не знает о том, что за предмет и что с ним будет дальше: его можно звать откуда угодно.
    /// При подборе его зовёт InspectOnFirstPickup (только первый предмет каждого типа).
    /// </summary>
    public interface IItemInspection
    {
        /// <summary>
        /// Идёт осмотр.
        /// </summary>
        bool IsInspecting { get; }

        /// <summary>
        /// Показать предмет на осмотре. Сам объект source на это время прячется (его заменяет копия перед камерой),
        /// по окончании снова показывается, и вызывается onFinished.
        /// false - осмотр сейчас невозможен (идёт другой, открыта панель с курсором, игрок обездвижен, у объекта нет
        /// видимых мешей): onFinished не вызывается, вызывающий сам решает, что делать.
        /// </summary>
        /// <param name="source">Объект, который осматривают (берутся его видимые меши).</param>
        /// <param name="displayName">Подпись под предметом.</param>
        /// <param name="flyTarget">Куда улетает предмет: объект на сцене или элемент интерфейса; null - туда,
        /// куда по умолчанию (панель инвентаря).</param>
        /// <param name="onFinished">Предмет долетел - осмотр закончен.</param>
        bool TryInspect(GameObject source, string displayName, Transform flyTarget, Action onFinished);
    }
}
