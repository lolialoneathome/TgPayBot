using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TelegramBotApi.Services
{
    public class TwilloPhoneNumberVerifier : IPhoneNumberVerifier
    {
        private readonly Config _config;
        public TwilloPhoneNumberVerifier(Config config) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<int> SendVerifyRequest(string phone)
        {
            var code = GenerateCode();
            await SendSms($"+{phone}", code.ToString());
            return code;
        }

        private int GenerateCode() {
            Random r = new Random();
            return r.Next(1000, 9999);
        }

        private async Task SendSms(string phone, string code) {
            var accountSid = _config.Twillo.Sid;
            var authToken = _config.Twillo.Token;

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(phone),
                from: new PhoneNumber(_config.Twillo.PhoneNumber),
                body: $"Код подтверждения: {code}");
        }
    }
}
