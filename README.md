# StswTxtJoiner

**StswTxtJoiner** is a small Windows app for quickly joining multiple text files into one combined output.

![StswTxtJoiner screenshot](docs/screenshot.webp)

It is useful when you want to merge Markdown files, JSON snippets, logs, notes, source-code fragments, configuration files, or any other text-based files without manually copying everything one by one.

## What it does

* Joins many text files into one output.
* Lets you add files manually or by drag & drop.
* Supports dropping whole folders and loading files recursively.
* Can filter files by extensions.
* Allows custom separators between joined files.
* Supports simple separator placeholders such as file name, extension, number, and path.
* Shows a preview of the generated output.
* Can copy the result to the clipboard.
* Can save the result to a file.

## Example use case

You have several files:

```text
intro.md
chapter-1.md
chapter-2.md
summary.md
```

You add them to StswTxtJoiner, set a separator, and generate one combined text file.

Example separator:

```text
--- {fileNo}. {fileName}.{fileExt} ---
```

Result:

```text
--- 1. intro.md ---
Content of intro.md

--- 2. chapter-1.md ---
Content of chapter-1.md

--- 3. chapter-2.md ---
Content of chapter-2.md

--- 4. summary.md ---
Content of summary.md
```

## Separator placeholders

You can use these placeholders in the separator text:

| Placeholder       | Meaning                     |
| ----------------- | --------------------------- |
| `{fileName}`      | File name without extension |
| `{fileExt}`       | File extension              |
| `{fileNo}`        | File number on the list     |
| `{filePath}`      | Full file path              |
| `{filePathShort}` | Shorter relative file path  |

## File filters

The app can work in two filter modes:

| Mode       | Description                               |
| ---------- | ----------------------------------------- |
| `Only`     | Adds only files with selected extensions  |
| `Excluded` | Adds all files except selected extensions |

Default allowed extensions:

```text
.md, .json, .txt
```

You can change them in the app.

## Download / installation

Download the latest release from the **Releases** section and run the application on Windows.

> Add release link here after publishing the first GitHub release.

## Build from source

Requirements:

* Windows
* .NET 8 SDK

Build:

```bash
dotnet build -c Release
```

Run:

```bash
dotnet run -c Release
```

Publish:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## Tech stack

* C#
* WPF
* .NET 8
