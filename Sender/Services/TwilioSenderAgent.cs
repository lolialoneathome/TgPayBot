using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Sender.Services
{
    public class TwilioSenderAgent : ISenderAgent
    {
        protected readonly IConfigService _configService;
        public TwilioSenderAgent(IConfigService configService)
        {
            _configService = configService;
        }
        public async Task SendMessageAsync(string senderId, string text)
        {
            var accountSid = _configService.Config.Twillo.Sid;
            var authToken = _configService.Config.Twillo.Token;

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(senderId),
                from: new PhoneNumber(_configService.Config.Twillo.PhoneNumber),
                body: text);
        }
    }
}
