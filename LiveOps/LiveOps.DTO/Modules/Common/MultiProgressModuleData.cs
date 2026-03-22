using System.Collections.Generic;
using System.Linq;
using Madbox.LiveOps.DTO.GameModule;
using Newtonsoft.Json;

namespace Madbox.LiveOps.DTO.Modules.Common
{
    public abstract class MultiProgressModuleData : IGameModuleData, IIsActive
    {
        public abstract string Key { get; }

        [JsonProperty]
        private bool _isActive;

        [JsonProperty]
        private List<ModuleProgress> _progress = new List<ModuleProgress>();

        [JsonIgnore]
        public bool IsActive => _isActive;

        [JsonIgnore]
        public List<ModuleProgress> Progress => _progress;

        public void SetActive(bool value)
        {
            _isActive = value;
        }

        public ModuleProgress GetProgress(string id)
        {
            return _progress.FirstOrDefault(p => p.Id == id);
        }

        public bool IsCompleted(string id)
        {
            ModuleProgress progress = GetProgress(id);
            return progress != null && progress.Status == ModuleStatus.Completed;
        }

        public virtual void SetProgress(string id, ModuleStatus status, ModuleProgressState state = ModuleProgressState.Default)
        {
            ModuleProgress progress = _progress.FirstOrDefault(p => p.Id == id);
            if (progress == null)
            {
                progress = new ModuleProgress { Id = id };
                _progress.Add(progress);
            }
            progress.Status = status;
            progress.State = state;
        }
    }
}
