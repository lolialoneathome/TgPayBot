using System.Collections.Generic;

namespace AdminApi.DTOs
{
    public class UserListDto
    {
        public int Total { get; set; }
        public IEnumerable<UserDto> List { get; set; }
    }
}
