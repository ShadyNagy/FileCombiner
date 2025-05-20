using Microsoft.Extensions.Logging;
using System.Text;
using FileCombiner.Models.Options;
using FileCombiner.Models.Results;
using FileCombiner.Services.Abstractions;

namespace FileCombiner.Services;

/// <summary>
/// Default implementation of the <see cref="IFileCombinerService"/> interface.
/// </summary>
public class FileCombinerService : IFileCombinerService
{
	private readonly ILogger<FileCombinerService> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileCombinerService"/> class.
	/// </summary>
	/// <param name="logger">The logger.</param>
	public FileCombinerService(ILogger<FileCombinerService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc />
	public async Task<FileCombinerResult> CombineFilesAsync(FileCombinerOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		if (string.IsNullOrEmpty(options.OutputPath))
		{
			_logger.LogError("Error during file combining: OutputPath is empty or null");
			return FileCombinerResult.Failure("Error during file combining: OutputPath is empty or null");
		}

		_logger.LogInformation("Starting file combining operation with directory: {Directory}, output: {Output}",
				options.DirectoryPath, options.OutputPath);

		try
		{
			var contentResult = await CombineFilesToStringAsync(options);

			if (!contentResult.IsSuccess)
			{
				return FileCombinerResult.Failure(contentResult.Error!);
			}

			string? outputDir = Path.GetDirectoryName(options.OutputPath);
			if (!string.IsNullOrEmpty(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			var encoding = options.Encoding ?? Encoding.UTF8;
			await File.WriteAllTextAsync(options.OutputPath, contentResult.Content!, encoding);

			long fileSize = new FileInfo(options.OutputPath).Length;
			_logger.LogInformation("Successfully combined {Count} files into {Output} ({Size} bytes)",
					contentResult.FilesProcessed, options.OutputPath, fileSize);

			return FileCombinerResult.Success(options.OutputPath, contentResult.FilesProcessed, fileSize);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during file combining: {Error}", ex.Message);
			return FileCombinerResult.Failure($"Error during file combining: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public async Task<FileCombinerContentResult> CombineFilesToStringAsync(FileCombinerOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		_logger.LogInformation("Starting file combining to string with directory: {Directory}",
				options.DirectoryPath);

		try
		{
			var scanResult = ScanDirectory(new FileScanOptions
			{
				DirectoryPath = options.DirectoryPath,
				FileExtensions = options.FileExtensions,
				IncludeSubdirectories = options.IncludeSubdirectories,
				FileFilter = options.FileFilter,
				ExcludeFolders = options.ExcludeFolders,
				ExcludeFileExtensions = options.ExcludeFileExtensions,
				ExcludeFiles = options.ExcludeFiles
			});

			if (!scanResult.IsSuccess)
			{
				_logger.LogError("Failed to scan directory: {Error}", scanResult.Error);
				return FileCombinerContentResult.Failure(scanResult.Error!);
			}

			if (scanResult.FilePaths.Count == 0)
			{
				_logger.LogWarning("No files found matching the specified criteria");
				return FileCombinerContentResult.Failure("No files found matching the specified criteria");
			}

			_logger.LogInformation("Found {Count} file(s) to combine", scanResult.FilePaths.Count);

			var contentBuilder = new StringBuilder();
			var encoding = options.Encoding ?? Encoding.UTF8;
			int fileCount = 0;

			foreach (var filePath in scanResult.FilePaths)
			{
				try
				{
					var fileContent = await File.ReadAllTextAsync(filePath, encoding);
					var relativePath = GetRelativePath(options.DirectoryPath, filePath);

					if (options.IncludeFileHeaders)
					{
						var header = options.HeaderFormat
								.Replace("{path}", relativePath)
								.Replace("{name}", Path.GetFileName(filePath))
								.Replace("{ext}", Path.GetExtension(filePath))
								.Replace("{index}", fileCount.ToString());

						contentBuilder.AppendLine(header);
					}

					contentBuilder.AppendLine(fileContent);
					contentBuilder.Append(options.FileSeparator);
					fileCount++;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error reading file {FilePath}: {Error}", filePath, ex.Message);
				}
			}

			if (fileCount == 0)
			{
				_logger.LogWarning("No files were successfully processed");
				return FileCombinerContentResult.Failure("No files were successfully processed");
			}

			string content = contentBuilder.ToString();
			_logger.LogInformation("Successfully combined {Count} files into string content ({Size} bytes)",
					fileCount, content.Length);

			return FileCombinerContentResult.Success(content, fileCount);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during file combining to string: {Error}", ex.Message);
			return FileCombinerContentResult.Failure($"Error during file combining to string: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public FileScanResult ScanDirectory(FileScanOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		_logger.LogDebug("Scanning directory {Directory} for files with extensions {Extensions}, recursive: {Recursive}",
				options.DirectoryPath, string.Join(", ", options.FileExtensions), options.IncludeSubdirectories);

		try
		{
			if (!Directory.Exists(options.DirectoryPath))
			{
				_logger.LogWarning("Directory not found: {Directory}", options.DirectoryPath);
				return FileScanResult.Failure($"Directory not found: {options.DirectoryPath}");
			}

			var searchOption = options.IncludeSubdirectories
					? SearchOption.AllDirectories
					: SearchOption.TopDirectoryOnly;

			IEnumerable<string> filePaths;

			if (options.FileExtensions == null || options.FileExtensions.Length == 0)
			{
				filePaths = Directory.GetFiles(options.DirectoryPath, "*.*", searchOption);
			}
			else
			{
				filePaths = options.FileExtensions
						.SelectMany(ext => Directory.GetFiles(
								options.DirectoryPath,
								$"*{(ext.StartsWith(".") ? ext : $".{ext}")}",
								searchOption))
						.Distinct();
			}

			if (options.ExcludeFolders is { Length: > 0 })
			{
				filePaths = filePaths.Where(path =>
				{
					var relativePath = GetRelativePath(options.DirectoryPath, path);
					return !options.ExcludeFolders.Any(excludeFolder =>
					{
						var normalizedExcludeFolder = excludeFolder.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

						return relativePath.StartsWith(normalizedExcludeFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
						       relativePath.Equals(normalizedExcludeFolder, StringComparison.OrdinalIgnoreCase);
					});
				});
			}

			if (options.ExcludeFolders?.Length > 0 ||
			    options.ExcludeFileExtensions?.Length > 0 ||
			    options.ExcludeFiles?.Length > 0)
			{
				filePaths = filePaths.Where(path =>
				{
					var relativePath = GetRelativePath(options.DirectoryPath, path);
					var fileName = Path.GetFileName(path);
					var fileExtension = Path.GetExtension(path);

					if (options.ExcludeFolders?.Length > 0 &&
					    options.ExcludeFolders.Any(excludeFolder =>
					    {
						    var normalizedExcludeFolder = excludeFolder.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

						    return relativePath.StartsWith(normalizedExcludeFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
						           relativePath.Equals(normalizedExcludeFolder, StringComparison.OrdinalIgnoreCase);
					    }))
					{
						return false;
					}

					if (options.ExcludeFileExtensions?.Length > 0 &&
					    options.ExcludeFileExtensions.Any(ext =>
					    {
						    var normalizedExt = ext.StartsWith(".") ? ext : $".{ext}";
						    return fileExtension.Equals(normalizedExt, StringComparison.OrdinalIgnoreCase);
					    }))
					{
						return false;
					}

					if (options.ExcludeFiles?.Length > 0 &&
					    options.ExcludeFiles.Any(excludeFile =>
						    fileName.Equals(excludeFile, StringComparison.OrdinalIgnoreCase)))
					{
						return false;
					}

					return true;
				});
			}

			if (options.FileFilter != null)
			{
				filePaths = filePaths.Where(options.FileFilter);
			}

			var result = filePaths.ToList();
			_logger.LogDebug("Found {Count} file(s) matching the criteria", result.Count);

			return FileScanResult.Success(result, options.DirectoryPath);
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogError(ex, "Access denied scanning directory: {Directory}", options.DirectoryPath);
			return FileScanResult.Failure($"Access denied: {ex.Message}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error scanning directory: {Directory}", options.DirectoryPath);
			return FileScanResult.Failure($"Error scanning directory: {ex.Message}");
		}
	}

	private string GetRelativePath(string basePath, string fullPath)
	{
		basePath = Path.GetFullPath(basePath);
		fullPath = Path.GetFullPath(fullPath);

		if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			basePath += Path.DirectorySeparatorChar;
		}

		if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
		{
			return fullPath[basePath.Length..];
		}

		return fullPath;
	}
}