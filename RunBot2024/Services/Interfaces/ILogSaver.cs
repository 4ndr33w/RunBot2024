namespace RunBot2024.Services.Interfaces
{
    public interface ILogSaver
    {

        Task SaveErrorLog(Exception e, long fromId, string adminName, string selectedRivalName = null);
    }
}
