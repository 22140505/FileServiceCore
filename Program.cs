using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Destructurama;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FileServiceCore;

public class Program
{
	public static int Main(string[] args)
	{
		var appSettings = new ConfigurationBuilder()
			.SetBasePath(System.IO.Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
			.AddEnvironmentVariables()
			.Build();
		Serilog.Log.Logger = new LoggerConfiguration()
			.Destructure.JsonNetTypes()
			//https://github.com/RehanSaeed/Serilog.Exceptions
			.ReadFrom.Configuration(appSettings)
			.CreateLogger();

		try
		{
			CreateHostBuilder(args).Build().Run();
			return 0;
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Host terminated unexpectedly");
			return 1;
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		var assemblyName = typeof(Startup).GetTypeInfo().Assembly.FullName;

		return Host.CreateDefaultBuilder(args)
			.UseSerilog()
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.UseKestrel(options=>options.Limits.MaxRequestBodySize= 1024 * 1024 * 128);
				webBuilder.UseStartup(assemblyName);
			});
	}
}