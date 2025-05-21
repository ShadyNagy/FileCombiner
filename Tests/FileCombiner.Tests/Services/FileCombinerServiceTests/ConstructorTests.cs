using FileCombiner.Services;
using FileCombiner.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace FileCombiner.Tests.Services.FileCombinerServiceTests;

public class ConstructorTests : IClassFixture<TestFileHelper>
{
	[Fact]
	public void Constructor_WithNullLogger_ThrowsArgumentNullException()
	{
		// Act
		var action = () => new FileCombinerService(null!);

		// Assert
		action.Should().Throw<ArgumentNullException>()
				.And.ParamName.Should().Be("logger");
	}
}