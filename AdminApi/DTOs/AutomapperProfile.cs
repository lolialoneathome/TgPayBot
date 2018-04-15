using AutoMapper;
using Sqllite;
using Sqllite.Logger;
using System;
using Utils;

namespace AdminApi.DTOs
{
    public class MappingProfile : Profile
    {
        protected readonly IPhoneHelper _phoneHelper;
        public MappingProfile(IPhoneHelper phoneHelper)
        {
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            CreateMap<User, UserDto>()
                .ForMember(x => x.PhoneNumber, opts => opts.MapFrom(src => _phoneHelper.Format(src.PhoneNumber)))
                .ForSourceMember(x => x.ChatId, opt => opt.Ignore());


            CreateMap<LogMessage, LogMessageDto>()
                .ForMember(x => x.Date, opts => opts.MapFrom(src => src.Date.ToString("dd.MM.yy HH:mm:ss")))
                .ForMember(x => x.PhoneNumber, opts => opts.MapFrom(src => _phoneHelper.Format(src.PhoneNumber)))
                .ForMember(x => x.Type, opts => opts.MapFrom(src => src.Type.ToString()));
        }
    }
}
