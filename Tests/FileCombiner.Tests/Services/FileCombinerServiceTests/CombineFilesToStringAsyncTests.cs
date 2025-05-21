using FileCombiner.Models.Options;
using FluentAssertions;
using System.Text;
using Xunit;
using FileCombiner.Services;
using FileCombiner.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileCombiner.Tests.Services.FileCombinerServiceTests;
public class CombineFilesToStringAsyncTests : IClassFixture<TestFileHelper>
{
	private readonly TestFileHelper _fileHelper;
	private readonly Mock<ILogger<FileCombinerService>> _loggerMock;
	private readonly FileCombinerService _service;

	public CombineFilesToStringAsyncTests(TestFileHelper fileHelper)
	{
		_fileHelper = fileHelper;
		_loggerMock = new Mock<ILogger<FileCombinerService>>();
		_service = new FileCombinerService(_loggerMock.Object);
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithNullOptions_ThrowsArgumentNullException()
	{
		// Act
		var action = () => _service.CombineFilesToStringAsync(null!);

		// Assert
		await action.Should().ThrowAsync<ArgumentNullException>()
				.WithParameterName("options");
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithoutDirectoryAndExplicitFiles_ReturnsFailure()
	{
		// Arrange
		var options = new FileCombinerOptions();

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Contain("Either DirectoryPath or ExplicitFilePaths must be provided");
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithValidInputs_ReturnsSuccess()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string file1 = _fileHelper.CreateTempFile("Content 1", testDir);
		string file2 = _fileHelper.CreateTempFile("Content 2", testDir);

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			IncludeFileHeaders = true,
			HeaderFormat = "// {path}",
			FileSeparator = "\n\n"
		};

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilesProcessed.Should().Be(2);
		result.Content.Should().Contain("Content 1");
		result.Content.Should().Contain("Content 2");
		result.Content.Should().Contain(Path.GetFileName(file1));
		result.Content.Should().Contain(Path.GetFileName(file2));
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithExplicitFilePaths_ReturnsSuccess()
	{
		// Arrange
		string file1 = _fileHelper.CreateTempFile("Content 1");
		string file2 = _fileHelper.CreateTempFile("Content 2");

		var options = new FileCombinerOptions
		{
			ExplicitFilePaths = [file1, file2],
			IncludeFileHeaders = true
		};

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilesProcessed.Should().Be(2);
		result.Content.Should().Contain("Content 1");
		result.Content.Should().Contain("Content 2");
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithNoFilesFound_ReturnsFailure()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			FileExtensions = [".nonexistent"]
		};

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Contain("No files found");
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithCustomFormatting_FormatsCorrectly()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string file = _fileHelper.CreateTempFile("Content", testDir, ".cs", "Test");

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			IncludeFileHeaders = true,
			HeaderFormat = "// File: {name} (Index: {index})",
			FileSeparator = "---\n"
		};

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Content.Should().Contain("// File: Test.cs (Index: 0)");
		result.Content.Should().Contain("Content");
		result.Content.Should().Contain("---\n");
	}

	[Fact]
	public async Task CombineFilesToStringAsync_WithCustomEncoding_ReadsCorrectly()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string filePath = Path.Combine(testDir, "test.txt");

		// Write file with UTF-16 encoding
		File.WriteAllText(filePath, "Content with special characters: äöü", Encoding.Unicode);

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			Encoding = Encoding.Unicode
		};

		// Act
		var result = await _service.CombineFilesToStringAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Content.Should().Contain("Content with special characters: äöü");
	}

}
