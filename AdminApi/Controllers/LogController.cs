using AdminApi.DTOs;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sqllite.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utils;

namespace AdminApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class LogController : Controller
    {
        protected readonly IReadLogService _logService;
        protected readonly ILogger<LogController> _toFileLogger;
        protected readonly IPhoneHelper _phoneHelper;
        protected readonly IMapper _mapper;
        public LogController(IReadLogService logService, ILogger<LogController> toFileLogger, IPhoneHelper phoneHelper, IMapper mapper) {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuth([FromQuery]int limit, [FromQuery]int offset)
        {
            try
            {
                return Ok(new LogMessagesListDto() {
                    Total = await _logService.GetTotal(MessageTypes.Auth),
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.Auth, limit, offset))
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
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.System, limit, offset))
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
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.Outgoing, limit, offset))
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
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.Incoming, limit, offset))
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
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.Errors, limit, offset))
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
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetLog(MessageTypes.SystemErrors, limit, offset))
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
                if (_phoneHelper.IsPhone(phone))
                    phone = _phoneHelper.Clear(phone);
                if (phone == null)
                    return BadRequest("Bad phone number");
                return Ok(new LogMessagesListDto()
                {
                    Total = await _logService.GetTotalByUserPhone(phone),
                    List = _mapper.Map<IEnumerable<LogMessageDto>>(await _logService.GetByUserPhone(phone, limit, offset))
                });

            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }
    }
}
