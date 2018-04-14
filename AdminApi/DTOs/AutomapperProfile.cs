using AutoMapper;
using Sqllite;

namespace AdminApi.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForSourceMember(x => x.ChatId, opt => opt.Ignore());
        }
    }
}
