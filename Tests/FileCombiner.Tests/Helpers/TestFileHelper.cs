namespace FileCombiner.Tests.Helpers;

public class TestFileHelper : IDisposable
{
	private readonly List<string> _tempFiles = new();
	private readonly List<string> _tempDirectories = new();

	public string CreateTempDirectory()
	{
		string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempPath);
		_tempDirectories.Add(tempPath);
		return tempPath;
	}

	public string CreateTempFile(string content = "test content", string? directory = null, string? extension = ".txt", string? fileName = null)
	{
		directory ??= Path.GetTempPath();
		extension = extension?.StartsWith(".") == true ? extension : $".{extension}";
		fileName ??= Guid.NewGuid().ToString("N");

		string filePath = Path.Combine(directory, $"{fileName}{extension}");
		File.WriteAllText(filePath, content);
		_tempFiles.Add(filePath);
		return filePath;
	}

	public string CreateSubdirectory(string parentDirectory, string subdirectoryName)
	{
		string path = Path.Combine(parentDirectory, subdirectoryName);
		Directory.CreateDirectory(path);
		_tempDirectories.Add(path);
		return path;
	}

	public void Dispose()
	{
		foreach (var file in _tempFiles)
		{
			try { if (File.Exists(file)) File.Delete(file); } catch { /* Ignore cleanup errors */ }
		}

		foreach (var dir in _tempDirectories.OrderByDescending(d => d.Length))
		{
			try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { /* Ignore cleanup errors */ }
		}
	}
}