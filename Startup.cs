using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace FileServiceCore;

public class Startup
{
	public Startup(IConfiguration configuration, IWebHostEnvironment env)
	{
		Configuration = configuration;
		this.env = env;
	}

	private readonly IWebHostEnvironment env;

	public IConfiguration Configuration { get; }

	// This method gets called by the runtime. Use this method to add services to the container.
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddCors(options =>
		{
			options.AddPolicy("any", builder =>
			{
				builder
					.AllowAnyMethod().SetIsOriginAllowed(_ => true)
					.AllowAnyHeader()
					.AllowCredentials();
			});
		});

		JsonConvert.DefaultSettings = () =>
		{
			var result = new JsonSerializerSettings()
			{
				ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
				DateFormatString = "yyyy-MM-dd HH:mm:ss",
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
			};
			return result;
		};
		services.AddControllers();

		if (Configuration.GetSection("Bearer").Exists() && !this.env.IsDevelopment())
		{
			services.AddAuthentication("Bearer")
				.AddJwtBearer("Bearer", options =>
				{
					options.Authority = Configuration["Bearer:Authority"];

					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateAudience = Configuration.GetValue<bool>("Bearer:TokenValidationParameters:ValidateAudience")
					};
					options.RequireHttpsMetadata = Configuration.GetValue<bool>("Bearer:RequireHttpsMetadata");
				});

			// adds an authorization policy to make sure the token is for scope 'api1'
			services.AddAuthorization(options =>
			{
				options.AddPolicy("ApiScope", policy =>
				{
					policy.RequireAuthenticatedUser();
					policy.RequireClaim("scope", "");
				});
			});
		}
		else
		{
			services.AddAuthentication(options =>  
			{
				options.AddScheme<MuteAuthenticationHandler>(MuteAuthenticationHandler.SchemeName, MuteAuthenticationHandler.SchemeName);
				options.DefaultScheme = MuteAuthenticationHandler.SchemeName;
			});
		}

		services.AddDbContext<Models.DatabaseContext>(option =>
			option.UseSqlServer(Configuration["connstr"]));

		services.AddSwaggerGen(c =>
		{
			c.CustomSchemaIds(x => x.FullName);
		});

		services.Configure<FormOptions>(options =>
		{
			options.MultipartBodyLengthLimit = 1024 * 1024 * 128;
			options.ValueLengthLimit = 1024 * 1024 * 128;
			options.MemoryBufferThreshold= 1024 * 1024 * 128;
		});

		//services.AddHostedService<DeleteService>();
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public void Configure(IApplicationBuilder app)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		app.UseCors("any");

		app.UseSerilogRequestLogging(options =>
		{
			options.Logger = Serilog.Log.Logger;
			options.IncludeQueryInRequestPath = true;
			options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
			{
				diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
			};
		});

		app.UseRouting();

		app.UseSwagger();
		app.UseSwaggerUI(c =>
		{
			c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
		});

		if (env.IsDevelopment())
		{
			app.Use(async (context, next) =>
			{
				if (!context.Request.Headers.ContainsKey("appid"))
					context.Request.Headers["appid"] = Configuration.GetSection("appids").Get<string[]>().First();
				await next();
			});
		}

		//app.UseStaticFiles();

		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
		});
	}
}