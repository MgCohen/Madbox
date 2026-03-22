using GameModuleDTO.ModuleRequests;

namespace GameModuleDTO.Sample.ReactiveModule
{
    /// <summary>
    /// Sample implementation managing reactive parameter commands execution blocks.
    /// </summary>
    public class ReactiveCounterRequest : ModuleRequest<ReactiveCounterResponse>
    {
        /// <summary>Gets or sets the tracking token internally securely mapping parameters.</summary>
        public int Value { get; set; }
    }
}
