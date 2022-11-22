using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FileServiceCore.Models;

public class DatabaseContext : DbContext
{
	public DatabaseContext(DbContextOptions<DatabaseContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
	}

	public DbSet<File> files { get; set; }
	public DbSet<Claim> claims { get; set; }
}

[Table(name: nameof(File)), Index(nameof(id))]
public class File
{
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int autoid { get; set; }
	[Key, StringLength(32)]
	public string id { get; set; } = Guid.NewGuid().ToString("N");

	[StringLength(32)]
	public string appid { get; set; }

	[MaxLength(500)]
	public string index { get; set; }

	[MaxLength(128)]
	public string name { get; set; }
	/// <summary>
	/// contents of the file
	/// </summary>
	public byte[] content { get; set; }
	/// <summary>
	/// file length
	/// </summary>
	public long length { get; set; }

	public byte[] hash { get; set; }

	/// <summary>
	/// date uploaded
	/// </summary>
	public DateTime date { get; set; } = DateTime.Now;

	public ICollection<Claim> claims { get; set; }

	public bool isDeleted { get; set; }=false;
	public DateTime? dateDeleted { get; set; } = null;
}

[Table(name: nameof(Claim))]
public class Claim
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int autoid { get; set; }

	[StringLength(32)]
	public string fileId{ get; set; }
	[ForeignKey(nameof(fileId))]
	public virtual File file{ get; set; }

	[StringLength(50)]
	public string name{ get; set; }
	[StringLength(50)]
	public string value { get; set; }
}