using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotApi.Services;

namespace TelegramBotApi.Controllers
{
    [Route("webhook")]
    public class WebHookController : Controller
    {
        protected readonly IUserMessageService _userMessageService;
        protected readonly IAdminMessageService _adminMessageService;
        protected readonly ILogger<WebHookController> _toFileLogger;
        public WebHookController(IUserMessageService messageService, IAdminMessageService adminMessageService, ILogger<WebHookController> toFileLogger) {
            _userMessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _adminMessageService = adminMessageService ?? throw new ArgumentNullException(nameof(adminMessageService));
            _toFileLogger = toFileLogger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Update update)
        {
            try {
                var message = update.Message;
                var chatId = message.Chat.Id;

                switch (message.Type) {
                    case MessageType.TextMessage:
                        await ReceivedtextMessage(message);
                        break;
                    case MessageType.ContactMessage:
                        await _userMessageService.ReceivedContact(chatId, message.From.Username, message.Contact.PhoneNumber);
                        break;
                    default:
                        await _userMessageService.ReceiveUnsupportedMessage(chatId, message.From.Username);
                        break;
                }
                return Ok();
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        private async Task ReceivedtextMessage(Message message)
        {
            var chatId = message.Chat.Id;
            switch (message.Text) {
                case "/start":
                    await _userMessageService.RequestSubscribe(chatId, message.From.Username);
                    break;
                case "/bye":
                    await _userMessageService.Unsubscribe(chatId, message.From.Username);
                    break;
                case "Отправить код ещё раз":
                    await _userMessageService.ResetCode(chatId);
                    break;
                case "/get_users":
                    await _adminMessageService.GetUsers(chatId);
                    break;
                case "/start_sending":
                    await _adminMessageService.StartSending(chatId);
                    break;
                case "/stop_sending":
                    await _adminMessageService.StopSending(chatId);
                    break;
                default:
                    await _userMessageService.ReceiveTextMessage(chatId, message.Text, message.From.Username);
                    break;
            }
        }

        [Route("healthcheck")]
        public string HealthCheck() {
            return "PASSED";
        }
    }
}
