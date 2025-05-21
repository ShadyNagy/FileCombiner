using FileCombiner.Extensions;
using FileCombiner.Models.Options;
using FileCombiner.Services;
using FileCombiner.Services.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FileCombiner.Tests.Extensions;

public class FileCombinerServiceCollectionExtensionsTests
{
	[Fact]
	public void AddFileCombiner_WithoutOptions_RegistersService()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		// Act
		services.AddFileCombiner();

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var service = serviceProvider.GetService<IFileCombinerService>();

		service.Should().NotBeNull();
		service.Should().BeOfType<FileCombinerService>();
	}

	[Fact]
	public void AddFileCombiner_WithOptions_RegistersServiceAndOptions()
	{
		// Arrange
		var services = new ServiceCollection();

		services.AddLogging();

		// Act
		services.AddFileCombiner(options =>
		{
			options.DirectoryPath = "test-path";
			options.IncludeSubdirectories = true;
		});

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var service = serviceProvider.GetService<IFileCombinerService>();

		service.Should().NotBeNull();
		service.Should().BeOfType<FileCombinerService>();
	}

	[Fact]
	public void AddFileCombiner_WithNullServices_ThrowsArgumentNullException()
	{
		// Arrange
		IServiceCollection services = null!;

		// Act
		var action = () => services.AddFileCombiner();

		// Assert
		action.Should().Throw<ArgumentNullException>()
				.And.ParamName.Should().Be("services");
	}

	[Fact]
	public void AddFileCombiner_WithNullConfigureOptions_ThrowsArgumentNullException()
	{
		// Arrange
		var services = new ServiceCollection();
		Action<FileCombinerOptions> configureOptions = null!;

		// Act
		var action = () => services.AddFileCombiner(configureOptions);

		// Assert
		action.Should().Throw<ArgumentNullException>()
				.And.ParamName.Should().Be("configureOptions");
	}
}