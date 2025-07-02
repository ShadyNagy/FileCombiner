# FileCombiner CLI (.NET 8)

A modern, high-performance .NET 8 command-line tool for combining multiple files into a single output file with advanced filtering and customization options.

## 🚀 .NET 8 Features

- **High Performance**: Built on .NET 8 runtime with optimized file I/O and memory usage
- **Cross-Platform**: Native support for Windows, macOS, and Linux (including ARM64)
- **Single-File Deployment**: Self-contained executable with compression
- **Modern C# 12**: Leverages latest language features for better performance and readability
- **Advanced Async Patterns**: Full async/await support throughout
- **JSON Output**: Machine-readable output for integration with other tools

## Installation

### As a Global .NET Tool (Recommended)
```bash
# Requires .NET 8 SDK
dotnet tool install -g FileCombiner.CLI
```

### Verify Installation
```bash
dotnet --version  # Should show 8.0.x or higher
filecombiner --version
```

### From Source
```bash
git clone <repository-url>
cd FileCombiner.CLI
dotnet build -c Release
dotnet run -- [commands]
```

## Commands

### `combine` - Combine files into an output file

Combines multiple files into a single output file with customizable formatting.

**Basic Usage:**
```bash
filecombiner combine --directory ./src --extensions .cs .csproj --output combined.txt
```

**New .NET 8 Features:**
- `--dry-run`: Preview what would be processed without actually combining files
- `--force`: Overwrite existing output files
- Enhanced error handling with detailed stack traces (use `--verbose`)
- Improved file path validation using `FileInfo` and `DirectoryInfo`
- Human-readable file sizes in output

**Options:**
- `--output, -o` (required): Output file path
- `--directory, -d`: Base directory to scan for files
- `--files, -f`: Explicit file paths to include (can specify multiple)
- `--extensions, -e`: File extensions to include (e.g., `.cs`, `.txt`)
- `--exclude-extensions, -xe`: File extensions to exclude
- `--exclude-folders, -xf`: Folder paths to exclude (relative to directory)
- `--exclude-files, -xn`: Specific file names to exclude
- `--recursive, -r`: Include subdirectories in scan
- `--no-headers`: Exclude file headers from output
- `--header-format`: Custom header format (default: `// {path}`)
- `--separator`: Line separator between files (default: `\n\n`)
- `--encoding`: Text encoding (utf8, ascii, unicode, etc.)
- `--verbose, -v`: Enable verbose logging

### `to-string` - Combine files and output to console

Same as `combine` but outputs the result to the console instead of a file.

**Usage:**
```bash
filecombiner to-string --directory ./src --extensions .cs
```

**Additional Options:**
- `--stats-only, -s`: Show only statistics, not the content

### `scan` - Scan and list matching files

Lists all files that would be processed by the combine operation.

**Usage:**
```bash
filecombiner scan --directory ./src --extensions .cs --recursive
```

**New .NET 8 Features:**
- `--json`: Output results in JSON format for integration with other tools
- Improved relative path handling
- Better performance with large directory structures

**Additional Options:**
- `--count-only, -c`: Show only the count of matching files

## .NET 8 Enhanced Examples

### Dry Run with Detailed Preview
```bash
# Preview what would be processed without actually combining
filecombiner combine \
  --directory ./src \
  --extensions .cs \
  --recursive \
  --output combined.cs \
  --dry-run \
  --verbose
```

### JSON Output for Automation
```bash
# Get scan results in JSON format for scripting
filecombiner scan \
  --directory ./src \
  --extensions .cs .csproj \
  --recursive \
  --json > scan-results.json
```

### Force Overwrite with Global Options
```bash
# Use global verbose option and force overwrite
filecombiner --verbose combine \
  --directory ./MyProject \
  --extensions .cs \
  --recursive \
  --output MyProject-Source.cs \
  --force
```

### Quiet Mode for CI/CD
```bash
# Suppress all output except errors (perfect for automation)
filecombiner --quiet combine \
  --directory ./src \
  --extensions .cs \
  --recursive \
  --output release-source.cs
```

## Examples

### Combine all C# files in a project
```bash
filecombiner combine \
  --directory ./MyProject \
  --extensions .cs \
  --recursive \
  --exclude-folders bin obj \
  --output MyProject-Combined.cs
```

### Combine specific files with custom headers
```bash
filecombiner combine \
  --files Program.cs Models/User.cs Services/UserService.cs \
  --output combined.txt \
  --header-format "=== {name} ===" \
  --separator "\n\n---\n\n"
```

### Combine all text files excluding certain patterns
```bash
filecombiner combine \
  --directory ./docs \
  --extensions .md .txt \
  --exclude-files README.md CHANGELOG.md \
  --exclude-folders .git node_modules \
  --recursive \
  --output documentation.md
```

### Preview what files would be combined
```bash
filecombiner scan \
  --directory ./src \
  --extensions .cs .csproj \
  --recursive \
  --exclude-folders bin obj
```

### Get just the count of matching files
```bash
filecombiner scan \
  --directory ./src \
  --extensions .cs \
  --recursive \
  --count-only
```

### Output combined content to terminal with stats
```bash
filecombiner to-string \
  --directory ./config \
  --extensions .json .xml \
  --stats-only
```

### Combine with different encoding
```bash
filecombiner combine \
  --directory ./legacy \
  --extensions .txt \
  --encoding ascii \
  --output legacy-combined.txt
```

### Mix directory scanning with explicit files
```bash
filecombiner combine \
  --directory ./src \
  --extensions .cs \
  --files ../shared/Utils.cs ../shared/Constants.cs \
  --exclude-folders Tests \
  --output full-project.cs
```

## Header Format Placeholders

When using `--header-format`, you can use these placeholders:
- `{path}`: Relative file path
- `{name}`: File name with extension
- `{ext}`: File extension
- `{index}`: Zero-based index of the file in processing order

**Examples:**
```bash
# Default format
--header-format "// {path}"

# Custom format with multiple placeholders
--header-format "/* File: {name} | Path: {path} | Index: {index} */"

# Simple separator
--header-format "=== {name} ==="
```

## Encoding Options

Supported encoding values:
- `utf8` or `utf-8` (default)
- `ascii`
- `unicode`, `utf16`, or `utf-16`
- `utf32` or `utf-32`
- `latin1` or `iso-8859-1`

## Error Handling

The tool provides clear error messages and exits with appropriate codes:
- `0`: Success
- `1`: Error occurred

Use `--verbose` for detailed logging information during processing.

## System Requirements

- **.NET 8 Runtime or SDK**: Required for execution
- **Cross-Platform**: Windows, macOS, Linux (x64 and ARM64)
- **Memory**: Optimized for low memory usage with streaming file operations
- **Performance**: Enhanced I/O operations using .NET 8's improved file handling

### Runtime Installation
If you don't have .NET 8:
```bash
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Or use package managers:

# Windows (winget)
winget install Microsoft.DotNet.Runtime.8

# macOS (homebrew)
brew install --cask dotnet

# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0
```

## Advanced Usage

### Combining with shell scripts

```bash
#!/bin/bash
# Combine all source files for a release
filecombiner combine \
  --directory ./src \
  --extensions .cs .csproj .json \
  --recursive \
  --exclude-folders bin obj .vs \
  --exclude-files *.designer.cs \
  --output "release-$(date +%Y%m%d).cs" \
  --header-format "// {path} (processed on $(date))"
```

### Using in CI/CD pipelines

```yaml
# GitHub Actions example
- name: Combine source files for documentation
  run: |
    filecombiner combine \
      --directory ./src \
      --extensions .cs \
      --recursive \
      --exclude-folders Tests \
      --output artifacts/source-combined.cs
```

### Filtering with complex patterns

```bash
# Combine all API-related files
filecombiner combine \
  --directory ./src \
  --extensions .cs \
  --recursive \
  --files ./src/Controllers/*.cs ./src/Models/Api/*.cs \
  --exclude-folders Tests bin obj \
  --exclude-files *Controller.Designer.cs \
  --output api-combined.cs
```

## Tips

1. **Preview first**: Use the `scan` command to preview which files will be processed
2. **Use --stats-only**: When working with large projects, use `to-string --stats-only` to see file counts and sizes
3. **Exclude build artifacts**: Always exclude `bin`, `obj`, `.git`, and other generated folders
4. **Custom separators**: Use meaningful separators like `\n\n//==================\n\n` for better readability
5. **Encoding awareness**: Specify encoding explicitly when working with legacy files or specific character sets