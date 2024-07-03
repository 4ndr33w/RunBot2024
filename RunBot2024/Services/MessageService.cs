using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

namespace RunBot2024.Services
{
    public class MessageService
    {
        private readonly IConfiguration _configuration;

        private TelegramBotClientOptions _botClientOptions;
        private TelegramBotClient _botClient;

        public MessageService(IConfiguration configuration)
        {
            _configuration = configuration;

            _botClientOptions = new TelegramBotClientOptions(_configuration["token"]);
            _botClient = new TelegramBotClient(_botClientOptions);
        }

        public async Task SendMessageFromTextFile(string filePath, long chatId)
        {
            using (var streamReader = new StreamReader(filePath))
            {
                StringBuilder messageText = new StringBuilder();
                messageText.Append(await streamReader.ReadToEndAsync());
                streamReader.Close();

                await _botClient.SendTextMessageAsync(chatId, messageText.ToString(), ParseMode.Html);
                messageText.Clear();
            }
        }

        public async Task SendImageFromFile(string filePath, long chatId)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var logo = new InputOnlineFile(fileStream);
            await _botClient.SendPhotoAsync(chatId, logo);
            fileStream.Close();
        }
    }
}
