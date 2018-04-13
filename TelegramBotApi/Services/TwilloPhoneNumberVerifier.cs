using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Utils;

namespace TelegramBotApi.Services
{
    public class TwilloPhoneNumberVerifier : IPhoneNumberVerifier
    {
        private readonly IConfigService _configService;
        protected readonly IPhoneHelper _phoneHelper;
        public TwilloPhoneNumberVerifier(IConfigService configService, IPhoneHelper phoneHelper) {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
        }

        public async Task<int> SendVerifyRequest(string phone)
        {
            var code = GenerateCode();
            await SendSms(_phoneHelper.GetPhoneNumberForTwilio(phone), code.ToString());
            return code;
        }

        private int GenerateCode() {
            Random r = new Random();
            return r.Next(1000, 9999);
        }

        private async Task SendSms(string phone, string code) {
            var accountSid = _configService.Config.Twillo.Sid;
            var authToken = _configService.Config.Twillo.Token;

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(phone),
                from: new PhoneNumber(_configService.Config.Twillo.PhoneNumber),
                body: $"Код подтверждения: {code}");
        }
    }
}
