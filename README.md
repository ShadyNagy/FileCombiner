# FileCombiner

[![NuGet](https://img.shields.io/nuget/v/FileCombiner.svg)](https://www.nuget.org/packages/FileCombiner/)
[![Build Status](https://github.com/ShadyNagy/FileCombiner/workflows/build/badge.svg)](https://github.com/ShadyNagy/FileCombiner/actions)
[![License](https://img.shields.io/github/license/ShadyNagy/FileCombiner.svg)](https://github.com/ShadyNagy/FileCombiner/blob/main/LICENSE.md)

A robust .NET library for combining multiple files into a single output file with comprehensive configuration options.

## Installation

### NuGet Package

```bash
dotnet add package FileCombiner
```

## Usage

### Basic Usage

```csharp
using FileCombiner;
using FileCombiner.Models.Options;
using FileCombiner.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Setup dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddFileCombiner();
    })
    .Build();

// Get the service
var fileCombinerService = host.Services.GetRequiredService<IFileCombinerService>();

// Configure options
var options = new FileCombinerOptions
{
    DirectoryPath = "/path/to/source",
    OutputPath = "/path/to/output.txt",
    FileExtensions = new[] { ".cs", ".txt" },
    IncludeSubdirectories = true
};

// Execute the operation
var result = await fileCombinerService.CombineFilesAsync(options);

// Check the result
if (result.IsSuccess)
{
    Console.WriteLine($"Combined {result.FilesProcessed} files into {result.OutputPath}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### Advanced Configuration

```csharp
// Advanced configuration example
var options = new FileCombinerOptions
{
    DirectoryPath = "/path/to/source",
    OutputPath = "/path/to/output.txt",
    FileExtensions = new[] { ".cs", ".txt" },
    IncludeSubdirectories = true,
    IncludeFileHeaders = true,
    HeaderFormat = "// File: {path} (Last Modified: {index})",
    FileSeparator = "\n\n// --------------------\n\n",
    Encoding = System.Text.Encoding.UTF8,
    FileFilter = filePath => !filePath.Contains("bin") && !filePath.Contains("obj")
};
```

## API Reference

### FileCombinerOptions

| Property | Type | Description |
|----------|------|-------------|
| DirectoryPath | string | The base directory path to scan for files |
| FileExtensions | string[] | The file extensions to include (e.g., ".cs", ".txt") |
| OutputPath | string | The output file path where combined content will be written |
| IncludeSubdirectories | bool | Whether to include subdirectories in the scan |
| IncludeFileHeaders | bool | Whether to include file headers with relative paths |
| HeaderFormat | string | The header format to use when including file headers |
| FileSeparator | string | The line separator to use between files |
| Encoding | Encoding? | The encoding to use when reading and writing files |
| FileFilter | Func<string, bool>? | An optional filter function to apply to files |

### FileCombinerResult

| Property | Type | Description |
|----------|------|-------------|
| IsSuccess | bool | Whether the operation was successful |
| Error | string? | The error that occurred during the operation, if any |
| OutputPath | string? | The output file path where combined content was written |
| FilesProcessed | int | The number of files that were combined |
| TotalBytes | long | The total size of the combined output in bytes |

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
