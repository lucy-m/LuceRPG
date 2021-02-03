using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuceRPG.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CredentialsController : ControllerBase
    {
        private readonly ICredentialService _credentialService;
        private readonly ILogger<CredentialsController> _logger;

        public CredentialsController(
            ICredentialService credentialService,
            ILogger<CredentialsController> logger)
        {
            _credentialService = credentialService;
            _logger = logger;
        }

        [HttpPut("addOrReset")]
        public string AddOrReset(string username)
        {
            _logger.LogInformation($"Adding or resetting credentials for {username}");
            return _credentialService.AddOrResetUser(username);
        }
    }
}
