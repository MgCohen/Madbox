namespace Madbox.LiveOps.DTO.Modules.Common
{
    public interface IIsActive
    {
        bool IsActive { get; }

        void SetActive(bool value);
    }
}
