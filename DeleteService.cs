using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileServiceCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileServiceCore;

public class DeleteService : BackgroundService
{
	public DeleteService(IServiceProvider services)
	{
		this.services = services;
	}

	public IServiceProvider services { get; }

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (true)
		{
			var threshold = DateTime.Now.AddDays(7);
			await using var scope = services.CreateAsyncScope();
			await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
			var filesDelete = db.files.Where(e => e.isDeleted && e.dateDeleted >= threshold).AsAsyncEnumerable();
			await foreach (var file in filesDelete)
			{
				db.Remove(file);
			}
			await db.SaveChangesAsync();

			await Task.Delay(new TimeSpan(1, 0, 0));
		}
	}
}
