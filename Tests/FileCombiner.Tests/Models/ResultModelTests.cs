using FileCombiner.Models.Results;
using FluentAssertions;
using Xunit;

namespace FileCombiner.Tests.Models;

public class ResultModelTests
{
	[Fact]
	public void FileCombinerResult_Success_SetsProperties()
	{
		// Arrange & Act
		var result = FileCombinerResult.Success("testpath", 10, 1024);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Error.Should().BeNull();
		result.OutputPath.Should().Be("testpath");
		result.FilesProcessed.Should().Be(10);
		result.TotalBytes.Should().Be(1024);
	}

	[Fact]
	public void FileCombinerResult_Failure_SetsError()
	{
		// Arrange & Act
		var result = FileCombinerResult.Failure("test error");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be("test error");
		result.OutputPath.Should().BeNull();
		result.FilesProcessed.Should().Be(0);
		result.TotalBytes.Should().Be(0);
	}

	[Fact]
	public void FileCombinerContentResult_Success_SetsProperties()
	{
		// Arrange & Act
		var result = FileCombinerContentResult.Success("test content", 5);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Error.Should().BeNull();
		result.Content.Should().Be("test content");
		result.FilesProcessed.Should().Be(5);
		result.TotalBytes.Should().Be("test content".Length);
	}

	[Fact]
	public void FileCombinerContentResult_Failure_SetsError()
	{
		// Arrange & Act
		var result = FileCombinerContentResult.Failure("test error");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be("test error");
		result.Content.Should().BeNull();
		result.FilesProcessed.Should().Be(0);
		result.TotalBytes.Should().Be(0);
	}

	[Fact]
	public void FileScanResult_Success_SetsProperties()
	{
		// Arrange
		var filePaths = new List<string> { "path1", "path2" };

		// Act
		var result = FileScanResult.Success(filePaths, "basepath");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Error.Should().BeNull();
		result.FilePaths.Should().BeEquivalentTo(filePaths);
		result.BasePath.Should().Be("basepath");
	}

	[Fact]
	public void FileScanResult_Failure_SetsError()
	{
		// Arrange & Act
		var result = FileScanResult.Failure("test error");

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be("test error");
		result.FilePaths.Should().BeEmpty();
		result.BasePath.Should().BeEmpty();
	}
}