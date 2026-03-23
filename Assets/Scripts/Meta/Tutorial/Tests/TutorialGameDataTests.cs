using System.Collections.Generic;
using GameModuleDTO.Modules.Tutorial;
using NUnit.Framework;

namespace Madbox.Tutorial.Tests
{
    public sealed class TutorialGameDataTests
    {
        [Test]
        public void Constructor_DerivesSequentialStates_LikeLevels()
        {
            TutorialConfig config = new TutorialConfig();
            config.SetTutorials(new List<int> { 1, 2, 3 });

            TutorialPersistence persistence = new TutorialPersistence();
            persistence.AddCompletedTutorial(1);

            TutorialGameData sut = new TutorialGameData(persistence, config);

            Assert.That(sut.Steps.Count, Is.EqualTo(3));
            Assert.That(sut.Steps[0].State, Is.EqualTo(TutorialStepState.Complete));
            Assert.That(sut.Steps[1].State, Is.EqualTo(TutorialStepState.Unlocked));
            Assert.That(sut.Steps[2].State, Is.EqualTo(TutorialStepState.Blocked));
            Assert.That(sut.RewardPerStep, Is.EqualTo(config.Reward));
        }
    }
}
