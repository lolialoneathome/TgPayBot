using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace BotWebApi
{
    public class EventsTelegramBotClient : TelegramBotClient
    {
        public EventsTelegramBotClient(string token) : base(token)
        {
            OnCallbackQuery += EventsTelegramBotClient_CallbackQueryReceived;
            OnInlineQuery += EventsTelegramBotClient_OnInlineQuery;
            OnInlineResultChosen += EventsTelegramBotClient_OnInlineResultChosen;
            OnMessage += EventsTelegramBotClient_OnMessage;
            OnMessageEdited += EventsTelegramBotClient_OnMessageEdited;
            OnReceiveError += EventsTelegramBotClient_OnReceiveError;
            OnReceiveGeneralError += EventsTelegramBotClient_OnReceiveGeneralError;
            OnUpdate += EventsTelegramBotClient_OnUpdate;



            StartReceiving();

        }

        private void EventsTelegramBotClient_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {

        }

        private void EventsTelegramBotClient_OnReceiveGeneralError(object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
        {
        }

        private void EventsTelegramBotClient_OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
        }

        private async void EventsTelegramBotClient_OnMessageEdited(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
        }

        private void EventsTelegramBotClient_OnInlineResultChosen(object sender, Telegram.Bot.Args.ChosenInlineResultEventArgs e)
        {
        }

        private void EventsTelegramBotClient_OnInlineQuery(object sender, Telegram.Bot.Args.InlineQueryEventArgs e)
        {
        }

        private void EventsTelegramBotClient_CallbackQueryReceived(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
        }

        private async void EventsTelegramBotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
        }
    }
}
