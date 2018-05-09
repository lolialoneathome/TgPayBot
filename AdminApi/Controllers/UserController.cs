using AdminApi.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminApi.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> GetList([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                var list = _mapper.Map<IEnumerable<UserDto>>(await _dbContext.Users.Skip(offset).Take(limit).ToListAsync());
                var result = new UserListDto() {
                    Total = await _dbContext.Users.CountAsync(),
                    List = list
                };
                return Ok(result);
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
