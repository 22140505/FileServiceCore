using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FileServiceCore.Models;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Data;

namespace FileServiceCore.Controllers;

[Route("v1/[action]")]
[Produces("application/json")]
[ApiController]
[Authorize]
public class V1Controller : ControllerBase
{
	public V1Controller(IConfiguration configuration, DatabaseContext db)
	{
		this.config = configuration;
		this.db = db;
	}

	protected IConfiguration config { get; private set; }
	protected DatabaseContext db { get; set; }

	protected string appid { get { return this.Request.Headers["appid"].ToString(); } }

	[HttpPost]
	public async Task<IActionResult> upload([Required] IFormFile file, [FromForm] string index = "", [FromForm] string claimsJsonArray = null, CancellationToken cancellationToken = default)
	{
		if (file == null)
			return BadRequest();

		var found = await db.files.AsNoTracking().AnyAsync(e => e.appid == appid && e.index == index && e.name == file.FileName, cancellationToken);
		if (found)
			return Ok(new { status = "287115dac7b449d4a0733bfe715e0fad", phrase = "repeat" });

		var f = new File();
		f.name = file.FileName;
		f.appid = appid;
		f.length = file.Length;
		f.index = index ?? "";
		if (!string.IsNullOrWhiteSpace(claimsJsonArray))
		{
			var claims = JsonConvert.DeserializeObject<List<ClaimParam>>(claimsJsonArray);
			f.claims = new List<Claim>();
			foreach (var c in claims)
			{
				f.claims.Add(new Claim
				{
					fileId = f.id,
					name = c.name,
					value = c.value
				});
			}
		}

		//content
		await using var stream = file.OpenReadStream();
		f.content = new byte[stream.Length];
		await stream.ReadAsync(f.content, 0, f.content.Length, cancellationToken);
		var md5 = MD5.Create();
		//hash
		stream.Position = 0;
		f.hash = await md5.ComputeHashAsync(stream, cancellationToken);

		db.files.Add(f);
		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 2601)
		{
			return Ok(new { status = "287115dac7b449d4a0733bfe715e0fad", phrase = "repeat" });
		}

		return Ok(new { status = 0, f.id });
	}

	[HttpPost]
	public async Task<IActionResult> copy(
		[FromForm] string filename,
		[FromForm, Required] string fileId,
		[FromForm] string index = "",
		[FromForm] string claimsJsonArray = null,
		CancellationToken cancellationToken = default)
	{
		var fileFrom = await db.files.AsNoTracking().Where(e => e.id == fileId).SingleOrDefaultAsync(cancellationToken);
		if (fileFrom == null)
			return Ok(new { status = "287115dac7b449d4a0733bfe715e0fad", phrase = "repeat" });

		if (string.IsNullOrWhiteSpace(filename))
			filename = fileFrom.name;

		var found = await db.files.AsNoTracking().AnyAsync(e => e.appid == appid && e.index == index && e.name == filename, cancellationToken);
		if (found)
			return Ok(new { status = "287115dac7b449d4a0733bfe715e0fad", phrase = "repeat" });

		var f = new File();
		f.name = filename;
		f.appid = appid;
		f.length = fileFrom.length;
		f.index = index ?? "";
		if (!string.IsNullOrWhiteSpace(claimsJsonArray))
		{
			var claims = JsonConvert.DeserializeObject<List<ClaimParam>>(claimsJsonArray);
			f.claims = new List<Claim>();
			foreach (var c in claims)
			{
				f.claims.Add(new Claim
				{
					fileId = f.id,
					name = c.name,
					value = c.value
				});
			}
		}

		f.content = fileFrom.content;
		f.hash = fileFrom.hash;
		db.files.Add(f);
		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 2601)
		{
			return Ok(new { status = "287115dac7b449d4a0733bfe715e0fad", phrase = "repeat" });
		}

		return Ok(new { f.id });
	}

	[HttpGet, AllowAnonymous]
	public async Task<IActionResult> download([Required] string id, CancellationToken cancellationToken)
	{
		if (id?.Length != 32)
			return BadRequest();
		id = id.ToLower();

		var f = await db.files.AsNoTrackingWithIdentityResolution().Where(e => e.id == id && e.isDeleted == false).Select(e => new { e.name, e.date, e.length }).SingleOrDefaultAsync(cancellationToken);
		if (f == null)
			return NotFound();

		var ext = System.IO.Path.GetExtension(f.name).ToLower();
		var contenttype=MimeMapping.MimeUtility.GetMimeMapping(ext);

		//try loading from cache first
		var cache = new FileCache();
		if (cache.tryGet(id, f.date, out var stream, out var _))
			return File(stream, contenttype, f.name);

		var connstr = config["connstr"];
		await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connstr);
		HttpContext.Response.RegisterForDispose(conn);
		await conn.OpenAsync(cancellationToken);
		await using var cmd = conn.CreateCommand();
		HttpContext.Response.RegisterForDispose(cmd);
		cmd.CommandText = $"select [content] from [file] where id='{id}'";
		cmd.CommandTimeout = 5 * 60;
		await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken);
		await reader.ReadAsync(cancellationToken);
		HttpContext.Response.RegisterForDispose(reader);
		await using var streamSequential = reader.GetStream(0);
		HttpContext.Response.RegisterForDispose(streamSequential);

		try
		{
			//save to cache
			await cache.saveAsync(id, streamSequential, f.length, ext, f.date);

			//load from cache again
			if (cache.tryGet(id, f.date, out stream, out var _))
				return File(stream, contenttype, f.name);
			else
				throw new InvalidOperationException();
		}
		catch (Exception ex)
		{
			streamSequential.Position = 0;
			return File(streamSequential, contenttype, f.name);
		}
	}

	[HttpPost]
	public async Task<IActionResult> delete([FromForm] string id, [FromForm] string index)
	{
		int effected;
		if (!string.IsNullOrWhiteSpace(id))
		{
			if (!await db.files.AnyAsync(e => e.id == id && e.isDeleted == false))
				return NotFound();
			db.files.RemoveRange(db.files.Where(e => e.id == id));
			effected = 1;
		}
		else if (!string.IsNullOrWhiteSpace(index))
		{
			var files = db.files.Where(e => e.index == index && e.isDeleted == false);
			effected = await files.CountAsync();
			await files.ForEachAsync(e =>
			{
				e.isDeleted = true;
				e.dateDeleted = DateTime.Now;
			});
			//db.files.RemoveRange(files);
		}
		else throw new InvalidOperationException();
		await db.SaveChangesAsync();
		return Ok(new { effected });
	}

	public class ClaimParam
	{
		public string name { get; set; }
		public string value { get; set; }
	}

	/// <summary>
	/// retrieve file list
	/// </summary>
	/// <param name="appid"></param>
	/// <param name="id"></param>
	/// <param name="index"></param>
	/// <param name="name">file name. when this parameter exists, it means to query by file name and support the wildcard character of sqlserver</param>
	/// <param name="page">fixed 20 per page</param>
	/// <param name="orderby">the value returned after sorting is date and name. indicates the date and file name uploaded according to the file</param>
	/// <param name="order">asc or desc</param>
	/// <param name="claimsJsonArray"></param>
	/// <returns></returns>
	[HttpGet]
	public async Task<IActionResult> query(string id, string index, string name, [Range(1, int.MaxValue)] int page = 1, string orderby = "date", string order = "asc", string claimsJsonArray = null, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(id))
			if (string.IsNullOrWhiteSpace(index) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(claimsJsonArray))
				return Ok(new { status = "14152677ed294fdd88e31a0fa6a7734e", phrase = "at least one query condition should be included" });

		var query = db.files.AsNoTracking().Include(e => e.claims).Where(e => e.appid == appid && e.isDeleted == false);

		if (string.IsNullOrWhiteSpace(id))
		{
			if (!string.IsNullOrWhiteSpace(index))
				query = query.Where(e => e.index == index);
			if (!string.IsNullOrWhiteSpace(name))
				query = query.Where(e => EF.Functions.Like(e.name, name));
			if (!string.IsNullOrWhiteSpace(claimsJsonArray))
			{
				query = query.Include(e => e.claims);
				var claims = JsonConvert.DeserializeObject<List<ClaimParam>>(claimsJsonArray);
				foreach (var c in claims.GroupBy(e => e.name))
				{
					query = query.Where(e => e.claims.Any(a => a.name == c.Key && c.Select(x => x.value).Contains(a.value)));
				}
			}
		}
		else
			query = query.Where(e => e.id == id);

		if (order == "asc")
			query = orderby switch
			{
				"date" => query.OrderBy(e => e.date),
				"name" => query.OrderBy(e => name),
			};
		else
			query = orderby switch
			{
				"date" => query.OrderByDescending(e => e.date),
				"name" => query.OrderByDescending(e => name),
			};
		const int PageSize = 20;
		query = query.Skip(PageSize * (page - 1)).Take(PageSize);


		var list = await query.Select(e => new
		{
			e.id,
			e.name,
			e.length,
			e.date,
			e.hash,
			claims = e.claims.Select(c => new { c.name, c.value }).ToArray()
		}).ToArrayAsync(cancellationToken);
		return Ok(new { list });
	}
}