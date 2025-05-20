namespace FileCombiner.Models.Results;

/// <summary>
/// Represents the result of a file combining operation.
/// </summary>
public class FileCombinerResult
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
	/// Gets the output file path where the combined content was written.
	/// </summary>
	public string? OutputPath { get; private set; }

	/// <summary>
	/// Gets the number of files that were combined.
	/// </summary>
	public int FilesProcessed { get; private set; }

	/// <summary>
	/// Gets the total size of the combined output in bytes.
	/// </summary>
	public long TotalBytes { get; private set; }

	/// <summary>
	/// Creates a success result.
	/// </summary>
	/// <param name="outputPath">The output file path.</param>
	/// <param name="filesProcessed">The number of files processed.</param>
	/// <param name="totalBytes">The total size of the combined output.</param>
	/// <returns>A success result.</returns>
	public static FileCombinerResult Success(string outputPath, int filesProcessed, long totalBytes) =>
		new()
		{
			OutputPath = outputPath,
			FilesProcessed = filesProcessed,
			TotalBytes = totalBytes
		};

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	/// <param name="error">The error message.</param>
	/// <returns>A failure result.</returns>
	public static FileCombinerResult Failure(string error) =>
		new() { Error = error };
}