using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileServiceCore.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
	[HttpGet("/api/health")]
	[AllowAnonymous]
	public IActionResult Get() => Ok("ok");
}