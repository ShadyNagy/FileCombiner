using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileCombiner.Extensions;
using FileCombiner.Models.Options;
using FileCombiner.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileCombiner.CLI;

internal sealed class Program
{
	private static async Task<int> Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder();

		// Configure logging
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.SetMinimumLevel(LogLevel.Information);

		// Configure services
		builder.Services.AddFileCombiner();

		using var host = builder.Build();

		var fileCombinerService = host.Services.GetRequiredService<IFileCombinerService>();
		var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
		var logger = loggerFactory.CreateLogger<Program>();

		var rootCommand = new RootCommand("FileCombiner CLI - Modern .NET 8 tool for combining multiple files")
				{
						CreateCombineCommand(fileCombinerService, loggerFactory),
						CreateCombineToStringCommand(fileCombinerService, loggerFactory),
						CreateScanCommand(fileCombinerService, loggerFactory)
				};

		// Add global options
		var verboseOption = new Option<bool>(
				aliases: ["--verbose", "-v"],
				description: "Enable verbose logging");

		var quietOption = new Option<bool>(
				aliases: ["--quiet", "-q"],
				description: "Suppress all output except errors");

		rootCommand.AddGlobalOption(verboseOption);
		rootCommand.AddGlobalOption(quietOption);

		return await rootCommand.InvokeAsync(args);
	}

	private static Command CreateCombineCommand(IFileCombinerService service, ILoggerFactory loggerFactory)
	{
		var command = new Command("combine", "Combine files into a single output file");

		// Required options
		var outputOption = new Option<FileInfo>(
				aliases: ["--output", "-o"],
				description: "Output file path where combined content will be written")
		{
			IsRequired = true
		};

		// Directory and file options
		var directoryOption = new Option<DirectoryInfo?>(
				aliases: ["--directory", "-d"],
				description: "Base directory path to scan for files");

		var explicitFilesOption = new Option<FileInfo[]>(
				aliases: ["--files", "-f"],
				description: "Explicit file paths to include (can be used multiple times)")
		{
			AllowMultipleArgumentsPerToken = true
		};

		// Filter options
		var extensionsOption = new Option<string[]>(
				aliases: ["--extensions", "-e"],
				description: "File extensions to include (e.g., .cs .txt)")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeExtensionsOption = new Option<string[]>(
				aliases: ["--exclude-extensions", "-xe"],
				description: "File extensions to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFoldersOption = new Option<string[]>(
				aliases: ["--exclude-folders", "-xf"],
				description: "Folder paths to exclude (relative to directory)")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFilesOption = new Option<string[]>(
				aliases: ["--exclude-files", "-xn"],
				description: "Specific file names to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		// Behavior options
		var recursiveOption = new Option<bool>(
				aliases: ["--recursive", "-r"],
				description: "Include subdirectories in the scan");

		var noHeadersOption = new Option<bool>(
				aliases: ["--no-headers"],
				description: "Exclude file headers from output");

		var headerFormatOption = new Option<string>(
		aliases: new[] { "--header-format" },
		description: "Custom header format (placeholders: {path}, {name}, {ext}, {index})"
);
		headerFormatOption.SetDefaultValue("// {path}");

		var separatorOption = new Option<string>(
			aliases: ["--separator"],
			description: "Line separator between files");
		separatorOption.SetDefaultValue("\n\n");

		var encodingOption = new Option<string>(
			aliases: ["--encoding"],
			description: "Text encoding (utf8, ascii, unicode, etc.)");
		encodingOption.SetDefaultValue("utf8");

		var forceOption = new Option<bool>(
				aliases: ["--force"],
				description: "Overwrite output file if it exists");

		var dryRunOption = new Option<bool>(
				aliases: ["--dry-run"],
				description: "Show what would be done without actually combining files");

		// Add all options
		command.AddOption(outputOption);
		command.AddOption(directoryOption);
		command.AddOption(explicitFilesOption);
		command.AddOption(extensionsOption);
		command.AddOption(excludeExtensionsOption);
		command.AddOption(excludeFoldersOption);
		command.AddOption(excludeFilesOption);
		command.AddOption(recursiveOption);
		command.AddOption(noHeadersOption);
		command.AddOption(headerFormatOption);
		command.AddOption(separatorOption);
		command.AddOption(encodingOption);
		command.AddOption(forceOption);
		command.AddOption(dryRunOption);

		command.SetHandler(async (context) =>
		{
			var output = context.ParseResult.GetValueForOption(outputOption)!;
			var directory = context.ParseResult.GetValueForOption(directoryOption);
			var explicitFiles = context.ParseResult.GetValueForOption(explicitFilesOption) ?? [];
			var extensions = context.ParseResult.GetValueForOption(extensionsOption) ?? [];
			var excludeExtensions = context.ParseResult.GetValueForOption(excludeExtensionsOption) ?? [];
			var excludeFolders = context.ParseResult.GetValueForOption(excludeFoldersOption) ?? [];
			var excludeFileNames = context.ParseResult.GetValueForOption(excludeFilesOption) ?? [];
			var recursive = context.ParseResult.GetValueForOption(recursiveOption);
			var noHeaders = context.ParseResult.GetValueForOption(noHeadersOption);
			var headerFormat = context.ParseResult.GetValueForOption(headerFormatOption)!;
			var separator = context.ParseResult.GetValueForOption(separatorOption)!;
			var encodingName = context.ParseResult.GetValueForOption(encodingOption) ?? "utf8";
			var force = context.ParseResult.GetValueForOption(forceOption);
			var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
			var verbose = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--verbose")));
			var quiet = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--quiet")));

			var logger = CreateLogger(loggerFactory, verbose, quiet);

			try
			{
				var encoding = GetEncoding(encodingName);
				if (encoding is null)
				{
					await Console.Error.WriteLineAsync($"Error: Unknown encoding '{encodingName}'");
					context.ExitCode = 1;
					return;
				}

				if (directory is null && explicitFiles.Length == 0)
				{
					await Console.Error.WriteLineAsync("Error: Either --directory or --files must be specified");
					context.ExitCode = 1;
					return;
				}

				if (output.Exists && !force)
				{
					await Console.Error.WriteLineAsync($"Error: Output file '{output.FullName}' already exists. Use --force to overwrite.");
					context.ExitCode = 1;
					return;
				}

				var options = new FileCombinerOptions
				{
					DirectoryPath = directory?.FullName,
					ExplicitFilePaths = explicitFiles.Select(f => f.FullName).ToArray(),
					FileExtensions = extensions,
					ExcludeFileExtensions = excludeExtensions,
					ExcludeFolders = excludeFolders,
					ExcludeFiles = excludeFileNames,
					OutputPath = output.FullName,
					IncludeSubdirectories = recursive,
					IncludeFileHeaders = !noHeaders,
					HeaderFormat = headerFormat,
					FileSeparator = separator,
					Encoding = encoding
				};

				if (dryRun)
				{
					await Console.Out.WriteLineAsync("🔍 Dry run mode - showing what would be processed:");
					var scanOptions = CreateScanOptions(options);
					var scanResult = service.ScanDirectory(scanOptions);

					if (scanResult.IsSuccess)
					{
						await Console.Out.WriteLineAsync($"📁 Would process {scanResult.FilePaths.Count} files");
						await Console.Out.WriteLineAsync($"📝 Would write to: {output.FullName}");

						if (verbose)
						{
							await Console.Out.WriteLineAsync("\nFiles that would be processed:");
							foreach (var filePath in scanResult.FilePaths)
							{
								await Console.Out.WriteLineAsync($"  {filePath}");
							}
						}
					}
					else
					{
						await Console.Error.WriteLineAsync($"❌ Error during scan: {scanResult.Error}");
						context.ExitCode = 1;
					}
					return;
				}

				if (!quiet)
				{
					await Console.Out.WriteLineAsync($"🔄 Combining files to: {output.FullName}");
					if (directory is not null)
						await Console.Out.WriteLineAsync($"📁 Directory: {directory.FullName}");
					if (explicitFiles.Length > 0)
						await Console.Out.WriteLineAsync($"📄 Explicit files: {explicitFiles.Length}");
				}

				var result = await service.CombineFilesAsync(options);

				if (result.IsSuccess)
				{
					if (!quiet)
					{
						await Console.Out.WriteLineAsync($"✅ Successfully combined {result.FilesProcessed} files");
						await Console.Out.WriteLineAsync($"📁 Output: {result.OutputPath}");
						await Console.Out.WriteLineAsync($"📊 Size: {result.TotalBytes:N0} bytes ({GetHumanReadableSize(result.TotalBytes)})");
					}
				}
				else
				{
					await Console.Error.WriteLineAsync($"❌ Error: {result.Error}");
					context.ExitCode = 1;
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error occurred");
				await Console.Error.WriteLineAsync($"❌ Unexpected error: {ex.Message}");
				if (verbose)
					await Console.Error.WriteLineAsync(ex.StackTrace);
				context.ExitCode = 1;
			}
		});

		return command;
	}

	private static Command CreateCombineToStringCommand(IFileCombinerService service, ILoggerFactory loggerFactory)
	{
		var command = new Command("to-string", "Combine files and output to console (no file output)");

		var directoryOption = new Option<DirectoryInfo?>(
				aliases: ["--directory", "-d"],
				description: "Base directory path to scan for files");

		var explicitFilesOption = new Option<FileInfo[]>(
				aliases: ["--files", "-f"],
				description: "Explicit file paths to include")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var extensionsOption = new Option<string[]>(
				aliases: ["--extensions", "-e"],
				description: "File extensions to include")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeExtensionsOption = new Option<string[]>(
				aliases: ["--exclude-extensions", "-xe"],
				description: "File extensions to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFoldersOption = new Option<string[]>(
				aliases: ["--exclude-folders", "-xf"],
				description: "Folder paths to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFilesOption = new Option<string[]>(
				aliases: ["--exclude-files", "-xn"],
				description: "Specific file names to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var recursiveOption = new Option<bool>(
				aliases: ["--recursive", "-r"],
				description: "Include subdirectories");

		var noHeadersOption = new Option<bool>(
				aliases: ["--no-headers"],
				description: "Exclude file headers");

		var headerFormatOption = new Option<string>(
			aliases: ["--header-format"],
			description: "Custom header format");

		headerFormatOption.SetDefaultValue("// {path}");


		var separatorOption = new Option<string>(
			aliases: ["--separator"],
			description: "Line separator between files");
		separatorOption.SetDefaultValue("\n\n");


		var statsOnlyOption = new Option<bool>(
				aliases: ["--stats-only", "-s"],
				description: "Show only statistics, not the content");

		command.AddOption(directoryOption);
		command.AddOption(explicitFilesOption);
		command.AddOption(extensionsOption);
		command.AddOption(excludeExtensionsOption);
		command.AddOption(excludeFoldersOption);
		command.AddOption(excludeFilesOption);
		command.AddOption(recursiveOption);
		command.AddOption(noHeadersOption);
		command.AddOption(headerFormatOption);
		command.AddOption(separatorOption);
		command.AddOption(statsOnlyOption);

		command.SetHandler(async (context) =>
		{
			var directory = context.ParseResult.GetValueForOption(directoryOption);
			var explicitFiles = context.ParseResult.GetValueForOption(explicitFilesOption) ?? [];
			var extensions = context.ParseResult.GetValueForOption(extensionsOption) ?? [];
			var excludeExtensions = context.ParseResult.GetValueForOption(excludeExtensionsOption) ?? [];
			var excludeFolders = context.ParseResult.GetValueForOption(excludeFoldersOption) ?? [];
			var excludeFileNames = context.ParseResult.GetValueForOption(excludeFilesOption) ?? [];
			var recursive = context.ParseResult.GetValueForOption(recursiveOption);
			var noHeaders = context.ParseResult.GetValueForOption(noHeadersOption);
			var headerFormat = context.ParseResult.GetValueForOption(headerFormatOption)!;
			var separator = context.ParseResult.GetValueForOption(separatorOption)!;
			var statsOnly = context.ParseResult.GetValueForOption(statsOnlyOption);
			var verbose = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--verbose")));
			var quiet = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--quiet")));

			var logger = CreateLogger(loggerFactory, verbose, quiet);

			try
			{
				if (directory is null && explicitFiles.Length == 0)
				{
					await Console.Error.WriteLineAsync("Error: Either --directory or --files must be specified");
					context.ExitCode = 1;
					return;
				}

				var options = new FileCombinerOptions
				{
					DirectoryPath = directory?.FullName,
					ExplicitFilePaths = explicitFiles.Select(f => f.FullName).ToArray(),
					FileExtensions = extensions,
					ExcludeFileExtensions = excludeExtensions,
					ExcludeFolders = excludeFolders,
					ExcludeFiles = excludeFileNames,
					IncludeSubdirectories = recursive,
					IncludeFileHeaders = !noHeaders,
					HeaderFormat = headerFormat,
					FileSeparator = separator
				};

				var result = await service.CombineFilesToStringAsync(options);

				if (result.IsSuccess)
				{
					if (statsOnly)
					{
						await Console.Out.WriteLineAsync($"📊 Files processed: {result.FilesProcessed}");
						await Console.Out.WriteLineAsync($"📊 Total size: {result.TotalBytes:N0} characters ({GetHumanReadableSize(result.TotalBytes)})");
					}
					else
					{
						if (!quiet)
						{
							await Console.Out.WriteLineAsync($"📊 Combined {result.FilesProcessed} files ({result.TotalBytes:N0} characters)");
							await Console.Out.WriteLineAsync(new string('=', 50));
						}
						await Console.Out.WriteLineAsync(result.Content);
					}
				}
				else
				{
					await Console.Error.WriteLineAsync($"❌ Error: {result.Error}");
					context.ExitCode = 1;
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error occurred");
				await Console.Error.WriteLineAsync($"❌ Unexpected error: {ex.Message}");
				context.ExitCode = 1;
			}
		});

		return command;
	}

	private static Command CreateScanCommand(IFileCombinerService service, ILoggerFactory loggerFactory)
	{
		var command = new Command("scan", "Scan directory and list matching files");

		var directoryOption = new Option<DirectoryInfo?>(
				aliases: ["--directory", "-d"],
				description: "Directory path to scan");

		var explicitFilesOption = new Option<FileInfo[]>(
				aliases: ["--files", "-f"],
				description: "Explicit file paths to include")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var extensionsOption = new Option<string[]>(
				aliases: ["--extensions", "-e"],
				description: "File extensions to include")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeExtensionsOption = new Option<string[]>(
				aliases: ["--exclude-extensions", "-xe"],
				description: "File extensions to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFoldersOption = new Option<string[]>(
				aliases: ["--exclude-folders", "-xf"],
				description: "Folder paths to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var excludeFilesOption = new Option<string[]>(
				aliases: ["--exclude-files", "-xn"],
				description: "Specific file names to exclude")
		{
			AllowMultipleArgumentsPerToken = true
		};

		var recursiveOption = new Option<bool>(
				aliases: ["--recursive", "-r"],
				description: "Include subdirectories");

		var countOnlyOption = new Option<bool>(
				aliases: ["--count-only", "-c"],
				description: "Show only the count of matching files");

		var jsonOutputOption = new Option<bool>(
				aliases: ["--json"],
				description: "Output results in JSON format");

		command.AddOption(directoryOption);
		command.AddOption(explicitFilesOption);
		command.AddOption(extensionsOption);
		command.AddOption(excludeExtensionsOption);
		command.AddOption(excludeFoldersOption);
		command.AddOption(excludeFilesOption);
		command.AddOption(recursiveOption);
		command.AddOption(countOnlyOption);
		command.AddOption(jsonOutputOption);

		command.SetHandler(async (context) =>
		{
			var directory = context.ParseResult.GetValueForOption(directoryOption);
			var explicitFiles = context.ParseResult.GetValueForOption(explicitFilesOption) ?? [];
			var extensions = context.ParseResult.GetValueForOption(extensionsOption) ?? [];
			var excludeExtensions = context.ParseResult.GetValueForOption(excludeExtensionsOption) ?? [];
			var excludeFolders = context.ParseResult.GetValueForOption(excludeFoldersOption) ?? [];
			var excludeFileNames = context.ParseResult.GetValueForOption(excludeFilesOption) ?? [];
			var recursive = context.ParseResult.GetValueForOption(recursiveOption);
			var countOnly = context.ParseResult.GetValueForOption(countOnlyOption);
			var jsonOutput = context.ParseResult.GetValueForOption(jsonOutputOption);
			var verbose = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--verbose")));
			var quiet = context.ParseResult.GetValueForOption(context.ParseResult.RootCommandResult.Command.Options
					.OfType<Option<bool>>().First(o => o.HasAlias("--quiet")));

			var logger = CreateLogger(loggerFactory, verbose, quiet);

			try
			{
				if (directory is null && explicitFiles.Length == 0)
				{
					await Console.Error.WriteLineAsync("Error: Either --directory or --files must be specified");
					context.ExitCode = 1;
					return;
				}

				var options = new FileScanOptions
				{
					DirectoryPath = directory?.FullName,
					ExplicitFilePaths = explicitFiles.Select(f => f.FullName).ToArray(),
					FileExtensions = extensions,
					ExcludeFileExtensions = excludeExtensions,
					ExcludeFolders = excludeFolders,
					ExcludeFiles = excludeFileNames,
					IncludeSubdirectories = recursive
				};

				var result = service.ScanDirectory(options);

				if (result.IsSuccess)
				{
					if (jsonOutput)
					{
						var jsonResult = new
						{
							success = true,
							basePath = result.BasePath,
							fileCount = result.FilePaths.Count,
							files = result.FilePaths.Select(path => new
							{
								fullPath = path,
								relativePath = !string.IsNullOrEmpty(result.BasePath) && path.StartsWith(result.BasePath)
											? Path.GetRelativePath(result.BasePath, path)
											: path,
								fileName = Path.GetFileName(path),
								extension = Path.GetExtension(path)
							}).ToArray()
						};

						await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(jsonResult, new System.Text.Json.JsonSerializerOptions
						{
							WriteIndented = true
						}));
					}
					else if (countOnly)
					{
						await Console.Out.WriteLineAsync(result.FilePaths.Count.ToString());
					}
					else
					{
						await Console.Out.WriteLineAsync($"📁 Base path: {result.BasePath}");
						await Console.Out.WriteLineAsync($"📊 Found {result.FilePaths.Count} matching file(s):");
						await Console.Out.WriteLineAsync();

						foreach (var filePath in result.FilePaths)
						{
							var relativePath = !string.IsNullOrEmpty(result.BasePath) && filePath.StartsWith(result.BasePath)
									? Path.GetRelativePath(result.BasePath, filePath)
									: filePath;
							await Console.Out.WriteLineAsync($"  {relativePath}");
						}
					}
				}
				else
				{
					if (jsonOutput)
					{
						var jsonError = new { success = false, error = result.Error };
						await Console.Out.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(jsonError));
					}
					else
					{
						await Console.Error.WriteLineAsync($"❌ Error: {result.Error}");
					}
					context.ExitCode = 1;
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error occurred");
				await Console.Error.WriteLineAsync($"❌ Unexpected error: {ex.Message}");
				context.ExitCode = 1;
			}
		});

		return command;
	}

	private static FileScanOptions CreateScanOptions(FileCombinerOptions options) => new()
	{
		DirectoryPath = options.DirectoryPath,
		ExplicitFilePaths = options.ExplicitFilePaths,
		FileExtensions = options.FileExtensions,
		ExcludeFileExtensions = options.ExcludeFileExtensions,
		ExcludeFolders = options.ExcludeFolders,
		ExcludeFiles = options.ExcludeFiles,
		IncludeSubdirectories = options.IncludeSubdirectories,
		FileFilter = options.FileFilter
	};

	private static ILogger CreateLogger(ILoggerFactory loggerFactory, bool verbose, bool quiet)
	{
		var logger = loggerFactory.CreateLogger<Program>();

		// Reconfigure logging based on verbosity
		if (quiet)
		{
			loggerFactory.CreateLogger("Microsoft").BeginScope("Quiet mode");
		}
		else if (verbose)
		{
			// Verbose logging is handled by the logger configuration
		}

		return logger;
	}

	private static Encoding? GetEncoding(string? encodingName) =>
		string.IsNullOrWhiteSpace(encodingName) ? Encoding.UTF8 : encodingName.ToLowerInvariant() switch
		{
		"utf8" or "utf-8" => Encoding.UTF8,
		"ascii" => Encoding.ASCII,
		"unicode" or "utf16" or "utf-16" => Encoding.Unicode,
		"utf32" or "utf-32" => Encoding.UTF32,
		"latin1" or "iso-8859-1" => Encoding.Latin1,
		_ => null
	};

	private static string GetHumanReadableSize(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
		_ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
	};
}