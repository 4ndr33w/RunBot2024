using Deployf.Botf;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using System.Xml.Linq;

namespace RunBot2024.Controllers
{
    public class MessageController : BotController
    {
        private readonly IConfiguration _configuration;

        private TelegramBotClientOptions _botClientOptions;// = new TelegramBotClientOptions();
        private TelegramBotClient _botClient;// = new TelegramBotClient();

        public MessageController(IConfiguration configuration)
        {
            _configuration = configuration;

            _botClientOptions = new TelegramBotClientOptions(_configuration["token"]);
            _botClient = new TelegramBotClient(_botClientOptions);
        }


        [Action]
        public async Task SendFileMessage(string filePath, long chatId)
        {
            string fileExtension = filePath.Substring(filePath.Length - 3, 3).ToLower();
            bool isTextFile = fileExtension == "txt" ? true : false;

            if (isTextFile)
            {
                using (var streamReader = new StreamReader(filePath))
                {
                    StringBuilder messageText = new StringBuilder();
                    messageText.Append(await streamReader.ReadToEndAsync());
                    streamReader.Close();

                    await _botClient.SendTextMessageAsync(chatId, messageText.ToString(), ParseMode.Html, default, default, default, default, 0, true, null);

                    //await Client.SendTextMessageAsync(chatId, "messageText", ParseMode.Html, default, default, default, default, 0, true, null);

                    //await SendTextMessageMethodCover(messageText.ToString(), chatId);
                    messageText.Clear();
                }
            }

            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await _botClient.SendPhotoAsync(chatId: FromId, photo: new InputOnlineFile(fileStream));
                    fileStream.Close();
                }
            }
        }
    }
}
