using Madbox.LiveOps.DTO.Keys;

namespace Madbox.LiveOps.DTO.ModuleRequests
{
    public abstract class ModuleRequest
    {
        public virtual string AuthKey { get; protected set; }

        public virtual string ModuleName => ModuleKeys.DefaultModuleName;

        public virtual int RetryCall { get; protected set; } = 2;

        public virtual int MaxRetries { get; protected set; } = 2;

        public string FunctionName => GetType().Name;

        public bool HasAuth => !string.IsNullOrEmpty(AuthKey);

        public abstract void AssertModule();
    }
}
