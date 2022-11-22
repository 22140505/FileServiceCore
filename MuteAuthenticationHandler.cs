using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FileServiceCore;

public class Identity : ClaimsIdentity
{
	public Identity(string authenticationType) : base(authenticationType)
	{
	}

	public bool isAnonymous { get; set; } = false;

	public string id { get; set; }

	public string loginId { get; set; }

	public string session { get; set; }
	public DateTime? dateSession { get; set; }

	//public string[] role { get; set; }
}

public class MuteAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public const string SchemeName = "mute";

	public MuteAuthenticationHandler(IConfiguration configuration, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
		: base(options, logger, encoder, clock)
	{
		this.configuration= configuration;
	}
	IConfiguration configuration;

	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		//avoid 401
	}
	protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
	{
		//avoid 403
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (this.Context.GetEndpoint().Metadata.GetMetadata<IAllowAnonymous>() != null)
		{
			var anonymousUser = new Identity(MuteAuthenticationHandler.SchemeName);
			anonymousUser.isAnonymous = true;
			return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(anonymousUser), SchemeName));
		}
		else
		{
			var appid = Request.Headers["appid"].ToString();
			var appids = this.configuration.GetSection("appids").Get<string[]>();
			if (appids.All(e => e != appid))
				return HandleRequestResult.Fail("Fail");

			var claims = new[] { new Claim("token", "a") };
			var identity = new ClaimsIdentity(claims, nameof(MuteAuthenticationHandler));
			var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
			return await Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}