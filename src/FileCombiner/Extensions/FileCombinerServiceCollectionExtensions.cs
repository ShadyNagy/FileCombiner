using System;
using FileCombiner.Models.Options;
using FileCombiner.Services;
using FileCombiner.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FileCombiner.Extensions;

/// <summary>
/// Extension methods for configuring FileCombiner services.
/// </summary>
public static class FileCombinerServiceCollectionExtensions
{
	/// <summary>
	/// Adds FileCombiner services to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
	public static IServiceCollection AddFileCombiner(this IServiceCollection services)
	{
		if (services == null)
		{
			throw new ArgumentNullException(nameof(services));
		}

		services.AddSingleton<IFileCombinerService, FileCombinerService>();

		return services;
	}

	/// <summary>
	/// Adds FileCombiner services to the specified <see cref="IServiceCollection"/> with custom configuration.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="configureOptions">A delegate to configure the <see cref="FileCombinerOptions"/>.</param>
	/// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
	public static IServiceCollection AddFileCombiner(
		this IServiceCollection services,
		Action<FileCombinerOptions> configureOptions)
	{
		if (services == null)
		{
			throw new ArgumentNullException(nameof(services));
		}

		if (configureOptions == null)
		{
			throw new ArgumentNullException(nameof(configureOptions));
		}

		services.AddFileCombiner();
		services.Configure(configureOptions);

		return services;
	}
}