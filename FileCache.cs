using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileServiceCore;

public class FileCache
{
	public FileCache()
	{
		cachePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), cacheDirName);
		if (!Directory.Exists(cachePath))
			Directory.CreateDirectory(cachePath);
	}

	private string cachePath;

	const string cacheDirName = "cache";
	const int cacheFileCap = 2000;

	public bool tryGet(string id, DateTime createTime, out FileStream stream, out string ext)
	{
		stream = null;
		ext = null;
		try
		{
			var filename = Directory.GetFiles(cachePath, $"{id}*").FirstOrDefault();
			if (filename == null)
				return false;

			if (File.GetCreationTime(filename) != createTime)
			{
				File.Delete(filename);
				return false;
			}

			ext = Path.GetExtension(filename);

			File.SetLastAccessTime(filename, DateTime.Now);

			stream = new FileStream(filename, FileMode.Open, FileAccess.Read);

			return true;
		}
		catch (Exception ex)
		{
			return false;
		}
		finally
		{
			var files = Directory.GetFiles(cachePath).Select(e => new KeyValuePair<string, DateTime>(e, File.GetLastAccessTime(e))).ToList();
			if (files.Count() > cacheFileCap)
			{
				files.Sort(new DefaultComparer<KeyValuePair<string, DateTime>>((l, r) =>
				{
					return (int)((l.Value - r.Value).TotalSeconds);
				}));
				foreach (var file in files.Take(cacheFileCap/10))
				{
					File.Delete(file.Key);
				}
			}
		}
	}

	public async Task saveAsync(string id, Stream stream, long length, string ext, DateTime createTime)
	{
		string filename = "";
		try
		{
			filename = Path.Combine(cachePath, id + ext);

			await using (var fs = new FileStream(filename, FileMode.Create))
				await stream.CopyToAsync(fs);

			File.SetCreationTime(filename, createTime);
			File.SetLastAccessTime(filename, DateTime.Now);
		}
		catch (Exception) 
		{
			File.Delete(filename);
		}
	}

	public void remove(string id)
	{
		try
		{
			var file = Directory.GetFiles(cachePath, $"{id}*").FirstOrDefault();
			File.Delete(file);
		}
		catch (Exception)
		{
			// ignored
		}
	}
}

public class DefaultComparer<T> : IComparer<T>
{
	readonly Func<T, T, int> _comparer;

	public DefaultComparer(Func<T, T, int> comparer)
	{
		this._comparer = comparer;
	}
	public int Compare(T x, T y)
	{
		return this._comparer(x, y);
	}
}