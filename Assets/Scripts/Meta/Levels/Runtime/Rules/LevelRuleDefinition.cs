using UnityEngine;

namespace Madbox.Levels.Rules
{
    public abstract class LevelRuleDefinition : ScriptableObject
    {
        /// <summary>
        /// Optional text for the end-game popup when this rule ends the session. When empty, the UI uses the default win/lose label.
        /// </summary>
        public string EndMessage => endMessage;

        [SerializeField] [TextArea(2, 4)] private string endMessage = "";
    }
}
