namespace Madbox.LiveOps.DTO.Modules.Common
{
    public class ModuleProgress
    {
        public string Id { get; set; } = string.Empty;
        public ModuleStatus Status { get; set; }
        public ModuleProgressState State { get; set; }
    }
}
