namespace FileCombiner.Models.Results;

/// <summary>
/// Represents the result of a file combining operation that returns content as a string.
/// </summary>
public class FileCombinerContentResult
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
	/// Gets the combined content.
	/// </summary>
	public string? Content { get; private set; }

	/// <summary>
	/// Gets the number of files that were combined.
	/// </summary>
	public int FilesProcessed { get; private set; }

	/// <summary>
	/// Gets the total size of the combined content in bytes.
	/// </summary>
	public long TotalBytes => Content?.Length ?? 0;

	/// <summary>
	/// Creates a success result.
	/// </summary>
	/// <param name="content">The combined content.</param>
	/// <param name="filesProcessed">The number of files processed.</param>
	/// <returns>A success result.</returns>
	public static FileCombinerContentResult Success(string content, int filesProcessed) =>
		new()
		{
			Content = content,
			FilesProcessed = filesProcessed
		};

	/// <summary>
	/// Creates a failure result.
	/// </summary>
	/// <param name="error">The error message.</param>
	/// <returns>A failure result.</returns>
	public static FileCombinerContentResult Failure(string error) =>
		new() { Error = error };
}