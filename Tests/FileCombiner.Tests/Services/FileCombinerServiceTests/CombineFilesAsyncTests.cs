using FileCombiner.Models.Options;
using FluentAssertions;
using System.Text;
using Xunit;
using FileCombiner.Services;
using FileCombiner.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileCombiner.Tests.Services.FileCombinerServiceTests;


public class CombineFilesAsyncTests : IClassFixture<TestFileHelper>
{
	private readonly TestFileHelper _fileHelper;
	private readonly Mock<ILogger<FileCombinerService>> _loggerMock;
	private readonly FileCombinerService _service;

	public CombineFilesAsyncTests(TestFileHelper fileHelper)
	{
		_fileHelper = fileHelper;
		_loggerMock = new Mock<ILogger<FileCombinerService>>();
		_service = new FileCombinerService(_loggerMock.Object);
	}

	[Fact]
	public async Task CombineFilesAsync_WithNullOptions_ThrowsArgumentNullException()
	{
		// Act
		var action = () => _service.CombineFilesAsync(null!);

		// Assert
		await action.Should().ThrowAsync<ArgumentNullException>()
				.WithParameterName("options");
	}

	[Fact]
	public async Task CombineFilesAsync_WithoutOutputPath_ReturnsFailure()
	{
		// Arrange
		var options = new FileCombinerOptions
		{
			DirectoryPath = "test"
		};

		// Act
		var result = await _service.CombineFilesAsync(options);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Contain("OutputPath is empty or null");
	}

	[Fact]
	public async Task CombineFilesAsync_WithoutDirectoryAndExplicitFiles_ReturnsFailure()
	{
		// Arrange
		var options = new FileCombinerOptions
		{
			OutputPath = "output.txt"
		};

		// Act
		var result = await _service.CombineFilesAsync(options);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Contain("Either DirectoryPath or ExplicitFilePaths must be provided");
	}

	[Fact]
	public async Task CombineFilesAsync_WithValidInputs_WritesFile()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string file1 = _fileHelper.CreateTempFile("Content 1", testDir);
		string file2 = _fileHelper.CreateTempFile("Content 2", testDir);
		string outputPath = Path.Combine(testDir, "output.txt");

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			OutputPath = outputPath,
			IncludeFileHeaders = true
		};

		// Act
		var result = await _service.CombineFilesAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.OutputPath.Should().Be(outputPath);
		result.FilesProcessed.Should().Be(2);
		result.TotalBytes.Should().BeGreaterThan(0);

		// Verify file was written
		File.Exists(outputPath).Should().BeTrue();
		string content = File.ReadAllText(outputPath);
		content.Should().Contain("Content 1");
		content.Should().Contain("Content 2");
	}

	[Fact]
	public async Task CombineFilesAsync_WithNestedOutputDirectory_CreatesDirectories()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string file = _fileHelper.CreateTempFile("Content", testDir);
		string outputDir = Path.Combine(testDir, "nested", "output");
		string outputPath = Path.Combine(outputDir, "output.txt");

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			OutputPath = outputPath
		};

		// Act
		var result = await _service.CombineFilesAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		Directory.Exists(outputDir).Should().BeTrue();
		File.Exists(outputPath).Should().BeTrue();
	}

	[Fact]
	public async Task CombineFilesAsync_WithCustomEncoding_WritesWithEncoding()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string filePath = Path.Combine(testDir, "test.txt");
		string outputPath = Path.Combine(testDir, "output.txt");

		// Write with UTF-16
		File.WriteAllText(filePath, "Content with special characters: èéêë", Encoding.Unicode);

		var options = new FileCombinerOptions
		{
			DirectoryPath = testDir,
			OutputPath = outputPath,
			Encoding = Encoding.Unicode
		};

		// Act
		var result = await _service.CombineFilesAsync(options);

		// Assert
		result.IsSuccess.Should().BeTrue();

		// Verify encoding was preserved
		byte[] fileBytes = File.ReadAllBytes(outputPath);
		fileBytes.Length.Should().BeGreaterThan(0);

		// UTF-16 files should start with BOM
		fileBytes[0].Should().Be(0xFF);
		fileBytes[1].Should().Be(0xFE);
	}

}