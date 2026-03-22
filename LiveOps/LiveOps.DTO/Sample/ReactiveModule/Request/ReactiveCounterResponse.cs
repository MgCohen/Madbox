using GameModuleDTO.ModuleRequests;

namespace GameModuleDTO.Sample.ReactiveModule
{
    /// <summary>
    /// Sample payload returning active state changes for reactive components.
    /// </summary>
    public class ReactiveCounterResponse : ModuleResponse
    {
        /// <summary>
        /// Initializes the returned response cleanly parameterizing the internal value.
        /// </summary>
        /// <param name="value">The resulting numeric execution argument.</param>
        public ReactiveCounterResponse(int value)
        {
            ValueB = value;
        }

        /// <summary>Gets the bound execution parameter integer internally.</summary>
        public int ValueB { get; protected set; }
    }
}