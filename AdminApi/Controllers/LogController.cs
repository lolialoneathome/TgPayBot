using AdminApi.DTOs;
using AdminApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sqllite.Logger;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AdminApi.Controllers
{
    [Route("api/[controller]")]
    public class LogController : Controller
    {
        protected readonly IReadLogService _logService;
        protected readonly ILogger<LogController> _toFileLogger;
        public LogController(IReadLogService logService, ILogger<LogController> toFileLogger) {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuth([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto() {
                    Total = await _logService.GetTotal(MessageTypes.Auth),
                    List = await _logService.GetLog(MessageTypes.Auth, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("system")]
        public async Task<IActionResult> GetSystem([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotal(MessageTypes.System),
                    List = await _logService.GetLog(MessageTypes.System, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("outgoing")]
        public async Task<IActionResult> GetOutgoung([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotal(MessageTypes.Outgoing),
                    List = await _logService.GetLog(MessageTypes.Outgoing, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("incoming")]
        public async Task<IActionResult> GetIncoming([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotal(MessageTypes.Incoming),
                    List = await _logService.GetLog(MessageTypes.Incoming, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("error")]
        public async Task<IActionResult> GetErrors([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotal(MessageTypes.Errors),
                    List = await _logService.GetLog(MessageTypes.Errors, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("syserror")]
        public async Task<IActionResult> GetSystemErrors([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotal(MessageTypes.SystemErrors),
                    List = await _logService.GetLog(MessageTypes.SystemErrors, limit, offset)
                });
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err.Message);
            };
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("user/{phone}")]
        public async Task<IActionResult> GetByUser(string phone, [FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotalByUserPhone(phone),
                    List = await _logService.GetByUserPhone(phone, limit, offset)
                });

            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }
    }
}
