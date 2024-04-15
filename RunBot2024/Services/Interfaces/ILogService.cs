using RunBot2024.Models;

namespace RunBot2024.Services.Interfaces
{
    public interface ILogService
    {
        Task CreateErrorLogAsync(ErrorLog errorLog);
        Task CreateResultLogAsync(ResultLog resultLog);
        Task CreateReplyLogAsync(ReplyLog replyLog);

        Task DeleteResultLogAsync(long telegramId);

        Task<List<ResultLog>> GetResultLogAsync(long telegramId);
        Task<List<ReplyLog>> GetReplyLogAsync(long telegramId);
    }
}
