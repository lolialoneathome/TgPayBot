using Sqllite;
using System;
using System.Linq;
using Telegram.Bot;

namespace EventApp
{
    public class EventsTelegramBotClient : TelegramBotClient
    {
        protected readonly string _dbPath;
        public EventsTelegramBotClient(string token, string dbPath) : base(token)
        {
            OnUpdate += EventsTelegramBotClient_OnUpdate;
            _dbPath = dbPath;
        }

        private void EventsTelegramBotClient_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            if (e.Update.Message.Text == "/start")
            { 
                using (var db = new UserContext(_dbPath))
                {
                    if (db.Users.Any(x => x.Username == e.Update.Message.From.Username))
                        return;

                    //db.Users.Add(new User {
                    //    ChatId = e.Update.Message.Chat.Id.ToString(),
                    //    Username = e.Update.Message.From.Username
                    //});
                    //var count = db.SaveChanges();
                }

                Console.WriteLine("Added user {0}", e.Update.Message.From.Username);
            }
            
        }        
    }
}
