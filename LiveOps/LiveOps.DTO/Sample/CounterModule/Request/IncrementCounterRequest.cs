namespace GameModuleDTO.ModuleRequests
{
    /// <summary>
    /// Sample request initiating a numeric counter step progression correctly.
    /// </summary>
    public class IncrementCounterRequest : ModuleRequest<IncrementCounterResponse>
    {
        /// <summary>Initializes a new instance for deserialization.</summary>
        public IncrementCounterRequest()
        {
        }
    }
}