using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sqllite.Logger;

namespace Utils.DbLogger
{
    public class NewBotLogger : INewBotLogger
    {
        protected readonly ILogger<NewBotLogger> _toFileLogger;
        protected readonly LogDbContext _dbContext;

        public NewBotLogger(ILogger<NewBotLogger> toFileLogger, LogDbContext dbContext)
        {
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<LogMessageStatus> LogAuth(string authMessage, string user)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = user,
                    Text = authMessage,
                    Type = MessageTypes.Auth
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }

        public async Task LogByType(MessageTypes type, string text, string person = null)
        {
            var logResult = new LogMessageStatus();
            switch (type)
            {
                case MessageTypes.Auth:
                    logResult = await LogIncoming(text, person);
                    break;
                case MessageTypes.Errors:
                    logResult = await LogError(text);
                    break;
                case MessageTypes.Incoming:
                    logResult = await LogIncoming(text, person);
                    break;
                case MessageTypes.Outgoing:
                    logResult = await LogOutgoing(text, person);
                    break;
                case MessageTypes.System:
                    logResult = await LogSystem(text, person);
                    break;
                case MessageTypes.SystemErrors:
                    logResult = await LogSystemError(text);
                    break;
            }
            if (!logResult.IsSuccess)
                _toFileLogger.LogError($"Error on log newLogger Message: {LogConst.MessageReceived}. From {person}");
        }

        public async Task<LogMessageStatus> LogError(string error)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = null,
                    Text = error,
                    Type = MessageTypes.Errors
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }

        public async Task<LogMessageStatus> LogIncoming(string action, string user)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = user,
                    Text = action,
                    Type = MessageTypes.Incoming
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }

        public async Task<LogMessageStatus> LogOutgoing(string action, string user)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = user,
                    Text = action,
                    Type = MessageTypes.Outgoing
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }

        public async Task<LogMessageStatus> LogSystem(string action, string user)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = user,
                    Text = action,
                    Type = MessageTypes.System
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }

        public async Task<LogMessageStatus> LogSystemError(string error)
        {
            try
            {
                await _dbContext.Logs.AddAsync(new LogMessage()
                {
                    Date = DateTime.Now,
                    PhoneNumber = null,
                    Text = error,
                    Type = MessageTypes.SystemErrors
                });
                await _dbContext.SaveChangesAsync();
                return new LogMessageStatus();
            }
            catch (Exception err)
            {
                return new LogMessageStatus()
                {
                    IsSuccess = false,
                    Error = err.Message
                };
            }
        }
    }
}
