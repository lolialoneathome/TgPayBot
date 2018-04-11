using Sender.DataSource.Base;
using System;

namespace Sender.DataSource.SenderAgents
{
    public class SenderAgentProvider : ISenderAgentProvider
    {
        protected readonly IServiceProvider _serviceProvider;
        public SenderAgentProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ISenderAgent Resolve(SenderType type)
        {
            switch (type) {
                case SenderType.Telegram:
                    return (ISenderAgent)(_serviceProvider.GetService(typeof(TelegramSenderAgent)));
                case SenderType.Sms:
                    return (ISenderAgent)(_serviceProvider.GetService(typeof(TwilioSmsSenderAgent)));
                default:
                    throw new InvalidOperationException("Unsupported sender type");
            }
        }
    }
}
