using System.Threading.Tasks;
using FileCombiner.Models.Options;
using FileCombiner.Models.Results;

namespace FileCombiner.Services.Abstractions;

/// <summary>
/// Represents a service for combining multiple files into a single output file.
/// </summary>
public interface IFileCombinerService
{
	/// <summary>
	/// Combines multiple files into a single output file asynchronously.
	/// </summary>
	/// <param name="options">The options for the file combining operation.</param>
	/// <returns>A <see cref="Task"/> representing the result of the file combining operation.</returns>
	Task<FileCombinerResult> CombineFilesAsync(FileCombinerOptions options);

	/// <summary>
	/// Combines multiple files into a string asynchronously without writing to a file.
	/// </summary>
	/// <param name="options">The options for the file combining operation.</param>
	/// <returns>A <see cref="Task{FileCombinerContentResult}"/> containing the combined content.</returns>
	Task<FileCombinerContentResult> CombineFilesToStringAsync(FileCombinerOptions options);

	/// <summary>
	/// Scans a directory for files matching specified criteria.
	/// </summary>
	/// <param name="options">The options for the file scanning operation.</param>
	/// <returns>A <see cref="FileScanResult"/> containing the results of the scan operation.</returns>
	FileScanResult ScanDirectory(FileScanOptions options);
}