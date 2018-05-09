using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayBot.Configuration;
using System;
using System.Threading.Tasks;

namespace AdminApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ConfigController : Controller
    {
        protected readonly IConfigService _configService;

        public ConfigController(IConfigService configService) {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        [HttpGet]
        public async Task<IActionResult> Get() {
            try
            {
                return Ok(_configService.Config);
            }
            catch (Exception err) {
                return BadRequest(err.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Config config) {
            try
            {
                await _configService.UpdateConfig(config);
                return NoContent();
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }

        [Route("healthcheck")]
        public string HealthCheck()
        {
            return "PASSED";
        }
    }
}
