using AdminApi.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminApi.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        protected readonly SqlliteDbContext _dbContext;
        protected readonly IMapper _mapper;

        public UserController(SqlliteDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList([FromQuery]int limit, [FromQuery]int offcet)
        {
            try
            {
                var list = _mapper.Map<IEnumerable<UserDto>>(_dbContext.Users.Skip(offcet).Take(limit).ToList());
                var result = new UserListDto() {
                    Total = _dbContext.Users.Count(),
                    List = list
                };
                return Ok(list);
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = _dbContext.Users.SingleOrDefault(x => x.Id == id);
                if (user == null)
                    return NotFound();
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }
    }
}
