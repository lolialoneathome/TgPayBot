using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sender.Controllers
{
    [Route("home")]
    public class HomeController : Controller
    {
        [Route("healthcheck")]
        public string HealthCheck()
        {
            return "PASSED";
        }
    }
}
