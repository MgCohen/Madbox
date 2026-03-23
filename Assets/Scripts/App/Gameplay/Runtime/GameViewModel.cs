using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Madbox.Levels;
using Madbox.Players;
using Scaffold.MVVM;
using UnityEngine;
using VContainer;

namespace Madbox.App.Gameplay
{
    public sealed class GameViewModel : ViewModel
    {
        public GameViewModel(LevelDefinition selectedLevel)
        {
            this.selectedLevel = selectedLevel ?? throw new System.ArgumentNullException(nameof(selectedLevel));
        }

        private readonly LevelDefinition selectedLevel;

        [Inject]
        private GameSessionCoordinator sessionCoordinator;

        [Inject]
        private PlayerFactory playerFactory;

        [Inject]
        private IMainMenuLauncher mainMenuLauncher;

        public void BeginSessionLoad(MonoBehaviour coroutineHost)
        {
            if (coroutineHost == null || sessionCoordinator == null || playerFactory == null)
            {
                return;
            }

            coroutineHost.StartCoroutine(LoadSessionRoutine(coroutineHost));
        }

        private IEnumerator LoadSessionRoutine(MonoBehaviour host)
        {
            Task sessionTask = sessionCoordinator.RunSessionAsync(selectedLevel, playerFactory, CancellationToken.None);
            yield return new WaitUntil(() => sessionTask.IsCompleted);
            if (sessionTask.IsFaulted && sessionTask.Exception != null)
            {
                Debug.LogException(sessionTask.Exception.GetBaseException(), host);
            }
        }

        public void ExitToMenu(MonoBehaviour coroutineHost)
        {
            if (coroutineHost == null || navigation == null)
            {
                return;
            }

            coroutineHost.StartCoroutine(ExitRoutine());
        }

        private IEnumerator ExitRoutine()
        {
            if (sessionCoordinator != null)
            {
                Task teardown = sessionCoordinator.TeardownSessionAsync(CancellationToken.None);
                yield return new WaitUntil(() => teardown.IsCompleted);
            }

            mainMenuLauncher?.OpenMainMenu(navigation);
        }
    }
}
