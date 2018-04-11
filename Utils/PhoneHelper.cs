using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public interface IPhoneHelper
    {
        string Format(string phone);
        bool IsPhone(string phone);
        string GetOnlyNumerics(string phone);
        string GetPhoneNumberForTwilio(string phone);
    }
    public class PhoneHelper : IPhoneHelper
    {
        public string Format(string phone)
        {
            var phoneParser = new Regex(@"(\d{1})(\d{3})(\d{3})(\d{2})(\d{2})");
            var format = "$1 ($2) $3-$4-$5";
            var res = phoneParser.Replace(phone, format);
            res = Regex.Replace(res, @"(\+)", "");
            return res;
        }

        public bool IsPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^((1-)?\d{3}-)?\d{3}-\d{4}$");
        }

        public string GetOnlyNumerics(string phone) {
            return string.Join("", phone.Where(c => Char.IsDigit(c)).ToArray());
        }

        public string GetPhoneNumberForTwilio(string phone) {
            var clearedPhone = GetOnlyNumerics(phone);
            return $"+{clearedPhone}";
        }
    }
}
