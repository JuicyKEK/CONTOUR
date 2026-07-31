using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

namespace Game.Scripts.Story
{
    public interface ICutsceneDirector
    {
        UniTask PlayAsync(PlayableAsset timeline, CancellationToken token);
    }
}

