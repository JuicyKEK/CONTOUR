using UnityEngine;

namespace Game.Scripts.Rendering
{
    /// <summary>
    /// Компонент на том же объекте, что и <see cref="PickupHighlight"/>, который сам решает цвета подсветки по
    /// данным предмета (например, AudioTapeHighlightColor: обычная кассета - жёлтым, злая - красным).
    /// </summary>
    public interface IPickupHighlightColorSource
    {
        /// <summary>
        /// Цвета свечения и обводки (уже с яркостью). false - пусть PickupHighlight берёт свои цвета.
        /// </summary>
        bool TryGetHighlightColors(out Color glow, out Color outline);
    }
}
