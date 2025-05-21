using FileCombiner.Models.Options;
using FluentAssertions;
using Xunit;
using FileCombiner.Services;
using FileCombiner.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileCombiner.Tests.Services.FileCombinerServiceTests;

public class ScanDirectoryTests : IClassFixture<TestFileHelper>
{

	private readonly TestFileHelper _fileHelper;
	private readonly Mock<ILogger<FileCombinerService>> _loggerMock;
	private readonly FileCombinerService _service;

	public ScanDirectoryTests(TestFileHelper fileHelper)
	{
		_fileHelper = fileHelper;
		_loggerMock = new Mock<ILogger<FileCombinerService>>();
		_service = new FileCombinerService(_loggerMock.Object);
	}

	[Fact]
	public void ScanDirectory_WithNullOptions_ThrowsArgumentNullException()
	{
		// Act
		var action = () => _service.ScanDirectory(null!);

		// Assert
		action.Should().Throw<ArgumentNullException>()
				.And.ParamName.Should().Be("options");
	}

	[Fact]
	public void ScanDirectory_WithValidDirectory_ReturnsSuccess()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string testFile1 = _fileHelper.CreateTempFile(directory: testDir, extension: ".txt");
		string testFile2 = _fileHelper.CreateTempFile(directory: testDir, extension: ".cs");

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			FileExtensions = [".txt", ".cs"]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().HaveCount(2);
		result.FilePaths.Should().Contain(testFile1);
		result.FilePaths.Should().Contain(testFile2);
		result.BasePath.Should().Be(testDir);
	}

	[Fact]
	public void ScanDirectory_WithInvalidDirectory_ReturnsFailure()
	{
		// Arrange
		string nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

		var options = new FileScanOptions
		{
			DirectoryPath = nonExistentDir
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Contain("Directory not found");
	}

	[Fact]
	public void ScanDirectory_WithExplicitFilePaths_IncludesFiles()
	{
		// Arrange
		string testFile1 = _fileHelper.CreateTempFile();
		string testFile2 = _fileHelper.CreateTempFile();

		var options = new FileScanOptions
		{
			ExplicitFilePaths = [testFile1, testFile2]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().HaveCount(2);
		result.FilePaths.Should().Contain(testFile1);
		result.FilePaths.Should().Contain(testFile2);
	}

	[Fact]
	public void ScanDirectory_WithFileExtensions_FiltersFiles()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string txtFile = _fileHelper.CreateTempFile(directory: testDir, extension: ".txt");
		string csFile = _fileHelper.CreateTempFile(directory: testDir, extension: ".cs");
		string jsonFile = _fileHelper.CreateTempFile(directory: testDir, extension: ".json");

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			FileExtensions = [".txt", ".cs"]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().HaveCount(2);
		result.FilePaths.Should().Contain(txtFile);
		result.FilePaths.Should().Contain(csFile);
		result.FilePaths.Should().NotContain(jsonFile);
	}

	[Fact]
	public void ScanDirectory_WithExcludeFolders_SkipsExcludedFolders()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string includeDir = _fileHelper.CreateSubdirectory(testDir, "include");
		string excludeDir = _fileHelper.CreateSubdirectory(testDir, "exclude");

		string includeFile = _fileHelper.CreateTempFile(directory: includeDir);
		string excludeFile = _fileHelper.CreateTempFile(directory: excludeDir);

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			IncludeSubdirectories = true,
			ExcludeFolders = ["exclude"]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().Contain(includeFile);
		result.FilePaths.Should().NotContain(excludeFile);
	}

	[Fact]
	public void ScanDirectory_WithExcludeFileExtensions_SkipsExcludedExtensions()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string txtFile = _fileHelper.CreateTempFile(directory: testDir, extension: ".txt");
		string csFile = _fileHelper.CreateTempFile(directory: testDir, extension: ".cs");

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			ExcludeFileExtensions = [".cs"]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().Contain(txtFile);
		result.FilePaths.Should().NotContain(csFile);
	}

	[Fact]
	public void ScanDirectory_WithExcludeFiles_SkipsExcludedFiles()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string includeFile = _fileHelper.CreateTempFile(directory: testDir, fileName: "include");
		string excludeFile = _fileHelper.CreateTempFile(directory: testDir, fileName: "exclude");

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			ExcludeFiles = ["exclude.txt"]
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().Contain(includeFile);
		result.FilePaths.Should().NotContain(excludeFile);
	}

	[Fact]
	public void ScanDirectory_WithFileFilter_AppliesToFiles()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string file1 = _fileHelper.CreateTempFile(directory: testDir, fileName: "file1");
		string file2 = _fileHelper.CreateTempFile(directory: testDir, fileName: "file2");

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			FileFilter = filePath => filePath.Contains("file1")
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().HaveCount(1);
		result.FilePaths.Should().Contain(file1);
		result.FilePaths.Should().NotContain(file2);
	}

	[Fact]
	public void ScanDirectory_WithIncludeSubdirectories_IncludesSubdirFiles()
	{
		// Arrange
		string testDir = _fileHelper.CreateTempDirectory();
		string subDir = _fileHelper.CreateSubdirectory(testDir, "subdir");

		string rootFile = _fileHelper.CreateTempFile(directory: testDir);
		string subDirFile = _fileHelper.CreateTempFile(directory: subDir);

		var options = new FileScanOptions
		{
			DirectoryPath = testDir,
			IncludeSubdirectories = true
		};

		// Act
		var result = _service.ScanDirectory(options);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.FilePaths.Should().Contain(rootFile);
		result.FilePaths.Should().Contain(subDirFile);
	}
}
