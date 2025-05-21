namespace FileCombiner.Models.Options;

/// <summary>
/// Options for file scanning operations.
/// </summary>
public class FileScanOptions
{
	/// <summary>
	/// Gets or sets the directory path to scan.
	/// Can be null or empty if only ExplicitFilePaths are specified.
	/// </summary>
	public string? DirectoryPath { get; set; }

	/// <summary>
	/// Gets or sets the file extensions to include (e.g., ".cs", ".txt").
	/// Only used when scanning a directory.
	/// </summary>
	public string[] FileExtensions { get; set; } = [];

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
	/// Gets or sets the explicit file paths to include in the scan results.
	/// These files will be included regardless of other filtering criteria.
	/// </summary>
	public string[] ExplicitFilePaths { get; set; } = [];

	/// <summary>
	/// Gets or sets an optional filter function to apply to files.
	/// </summary>
	public Func<string, bool>? FileFilter { get; set; } = null;
}