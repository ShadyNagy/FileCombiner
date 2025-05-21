using System.Text;

namespace FileCombiner.Models.Options;

/// <summary>
/// Options for file combining operations.
/// </summary>
public class FileCombinerOptions
{
	/// <summary>
	/// Gets or sets the base directory path to scan for files.
	/// Can be null or empty if only ExplicitFilePaths are specified.
	/// </summary>
	public string? DirectoryPath { get; set; }

	/// <summary>
	/// Gets or sets the explicit file paths to include in the combination.
	/// If specified, these files will be included in addition to any files found by directory scanning.
	/// </summary>
	public string[] ExplicitFilePaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the file extensions to include (e.g., ".cs", ".csproj").
	/// Only used when scanning a directory.
	/// </summary>
	public string[] FileExtensions { get; set; } = [];

	/// <summary>
	/// Gets or sets the output file path where combined content will be written.
	/// </summary>
	public string? OutputPath { get; set; }

	/// <summary>
	/// Gets or sets whether to include subdirectories in the scan.
	/// </summary>
	public bool IncludeSubdirectories { get; set; } = false;

	/// <summary>
	/// Gets or sets the folder paths to exclude from the scan (e.g., "/zzz").
	/// Paths are relative to the DirectoryPath.
	/// </summary>
	public string[] ExcludeFolders { get; set; } = [];

	/// <summary>
	/// Gets or sets the file extensions to exclude (e.g., ".dll", ".exe").
	/// </summary>
	public string[] ExcludeFileExtensions { get; set; } = [];

	/// <summary>
	/// Gets or sets the specific files to exclude by name (e.g., "Program.cs").
	/// </summary>
	public string[] ExcludeFiles { get; set; } = [];

	/// <summary>
	/// Gets or sets whether to include file headers with relative paths.
	/// </summary>
	public bool IncludeFileHeaders { get; set; } = true;

	/// <summary>
	/// Gets or sets the header format to use when including file headers.
	/// Available placeholders: {path}, {name}, {ext}, {index}
	/// </summary>
	public string HeaderFormat { get; set; } = "// {path}";

	/// <summary>
	/// Gets or sets the line separator to use between files.
	/// </summary>
	public string FileSeparator { get; set; } = "\n\n";

	/// <summary>
	/// Gets or sets the encoding to use when reading and writing files.
	/// If null, the default encoding is used.
	/// </summary>
	public Encoding? Encoding { get; set; } = null;

	/// <summary>
	/// Gets or sets an optional filter function to apply to files.
	/// </summary>
	public Func<string, bool>? FileFilter { get; set; } = null;
}
