namespace FileCombiner.Models.Results;

/// <summary>
/// Represents the result of a file scanning operation.
/// </summary>
public class FileScanResult
{
	/// <summary>
	/// Gets whether the operation was successful.
	/// </summary>
	public bool IsSuccess => Error is null;

	/// <summary>
	/// Gets the error that occurred during the operation, if any.
	/// </summary>
	public string? Error { get; private set; }

	/// <summary>
	/// Gets the list of file paths that matched the scan criteria.
	/// </summary>
	public IReadOnlyList<string> FilePaths { get; private set; } = Array.Empty<string>();

	/// <summary>
	/// Gets the base directory path that was scanned.
	/// </summary>
	public string BasePath { get; private set; } = string.Empty;

	/// <summary>
	/// Creates a success result.
	/// </summary>
	/// <param name="filePaths">The list of file paths found.</param>
	/// <param name="basePath">The base directory that was scanned.</param>
	/// <returns>A success result.</returns>
	public static FileScanResult Success(IReadOnlyList<string> filePaths, string basePath) =>
		new()
		{
			FilePaths = filePaths,
			BasePath = basePath
		};

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	/// <param name="error">The error message.</param>
	/// <returns>A failure result.</returns>
	public static FileScanResult Failure(string error) =>
		new() { Error = error };
}