using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Madbox.Battle
{
    public readonly struct BattleBootstrapResult
    {
        public BattleBootstrapResult(BattleGame game, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            Game = game;
            SceneLoadHandle = sceneLoadHandle;
        }

        public BattleGame Game { get; }

        public AsyncOperationHandle<SceneInstance> SceneLoadHandle { get; }
    }
}
