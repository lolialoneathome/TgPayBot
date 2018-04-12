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
        protected readonly IMessageRoutingService _routeMessageService;
        protected readonly ILogger<WebHookController> _toFileLogger;
        public WebHookController(IMessageRoutingService routeService, ILogger<WebHookController> toFileLogger) {
            _routeMessageService = routeService ?? throw new ArgumentNullException(nameof(_routeMessageService));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Update update)
        {
            try {
                if (update.Type == UpdateType.MessageUpdate) {
                    var message = update.Message;
                    var chatId = message.Chat.Id;

                    switch (message.Type)
                    {
                        case MessageType.TextMessage:
                            await _routeMessageService.RouteTextMessage(message);
                            break;
                        case MessageType.ContactMessage:
                            await _routeMessageService.RouteContactMessage(message);
                            break;
                        default:
                            await _routeMessageService.RouteUnsopportedMessage(message);
                            break;
                    }
                    return Ok();
                }
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            };
        }

        [Route("healthcheck")]
        public string HealthCheck() {
            return "PASSED";
        }
    }
}
