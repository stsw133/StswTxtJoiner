using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace StswTxtJoiner;

public partial class MainContext : StswObservableObject
{
	[StswObservableProperty] ObservableCollection<FileInfoModel> _fileList = [];
	[StswObservableProperty] FileInfoModel? _selectedFileInfo;
	[StswObservableProperty] string _separatorText = string.Empty;
	[StswObservableProperty] string _outputText = string.Empty;
	[StswObservableProperty] bool _isOutputReadOnly = true;
	[StswObservableProperty] string _onlyFilterExtensions = AppSettings.DefaultOnlyFilterExtensions;
	[StswObservableProperty] string _excludedFilterExtensions = string.Empty;
	[StswObservableProperty] FileFilterMode _filterMode = FileFilterMode.Only;
	bool _isLoadingSettings;

	public MainContext()
	{
		_isLoadingSettings = true;
		var settings = AppSettings.Load();
		OnlyFilterExtensions = string.IsNullOrWhiteSpace(settings.OnlyFilterExtensions) ? AppSettings.DefaultOnlyFilterExtensions : settings.OnlyFilterExtensions;
		ExcludedFilterExtensions = settings.ExcludedFilterExtensions ?? string.Empty;
		FilterMode = settings.FilterMode;
		_isLoadingSettings = false;
	}

	public bool IsOnlyFilterMode
	{
		get => FilterMode == FileFilterMode.Only;
		set
		{
			if (value)
				FilterMode = FileFilterMode.Only;
		}
	}

	public bool IsExcludedFilterMode
	{
		get => FilterMode == FileFilterMode.Excluded;
		set
		{
			if (value)
				FilterMode = FileFilterMode.Excluded;
		}
	}

	partial void OnOnlyFilterExtensionsChanged(string oldValue, string newValue) => SaveSettings();

	partial void OnExcludedFilterExtensionsChanged(string oldValue, string newValue) => SaveSettings();

	partial void OnFilterModeChanged(FileFilterMode oldValue, FileFilterMode newValue)
	{
		OnPropertyChanged(nameof(IsOnlyFilterMode));
		OnPropertyChanged(nameof(IsExcludedFilterMode));
		SaveSettings();
	}

	[StswCommand]
	void AddFiles()
	{
		var dialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "All files (*.*)|*.*"
		};
		if (dialog.ShowDialog() != true)
			return;

		AddFilesToList(dialog.FileNames);
	}

	[StswCommand]
	void DragFilesOver(DragEventArgs eventArgs)
	{
		var containsFiles = eventArgs.Data.GetDataPresent(DataFormats.FileDrop);
		eventArgs.Effects = containsFiles ? DragDropEffects.Copy : DragDropEffects.None;
		eventArgs.Handled = true;
	}

	[StswCommand]
	void DropFiles(DragEventArgs eventArgs)
	{
		if (!eventArgs.Data.GetDataPresent(DataFormats.FileDrop))
			return;

		if (eventArgs.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
			AddFilesToList(ResolveDroppedFiles(fileNames));

		eventArgs.Handled = true;
	}

	void AddFilesToList(IEnumerable<string> fileNames)
	{
		foreach (var fileName in fileNames.Where(File.Exists).Where(IsFileAcceptedByFilter))
		{
			var fileInfo = new FileInfoModel
			{
				FileName = Path.GetFileName(fileName),
				FilePath = fileName,
			};
			if (!FileList.Any(f => f.FilePath == fileInfo.FilePath))
				FileList.Add(fileInfo);
		}
	}

	static IEnumerable<string> ResolveDroppedFiles(IEnumerable<string> paths)
	{
		foreach (var path in paths)
		{
			if (File.Exists(path))
				yield return path;
			else if (Directory.Exists(path))
			{
				foreach (var fileName in EnumerateFilesSafely(path))
					yield return fileName;
			}
		}
	}

	static IEnumerable<string> EnumerateFilesSafely(string directory)
	{
		IEnumerable<string> files = [];
		IEnumerable<string> directories = [];

		try
		{
			files = Directory.EnumerateFiles(directory).ToArray();
		}
		catch (Exception) when (IsFileSystemEnumerationException())
		{
		}

		foreach (var file in files)
			yield return file;

		try
		{
			directories = Directory.EnumerateDirectories(directory).ToArray();
		}
		catch (Exception) when (IsFileSystemEnumerationException())
		{
		}

		foreach (var subdirectory in directories)
		foreach (var file in EnumerateFilesSafely(subdirectory))
			yield return file;
	}

	static bool IsFileSystemEnumerationException() => true;

	bool IsFileAcceptedByFilter(string fileName)
	{
		var extensions = ParseExtensions(FilterMode == FileFilterMode.Only ? OnlyFilterExtensions : ExcludedFilterExtensions).ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (extensions.Count == 0)
			return true;

		var extension = Path.GetExtension(fileName);
		var containsExtension = extensions.Contains(extension);
		return FilterMode == FileFilterMode.Only ? containsExtension : !containsExtension;
	}

	static IEnumerable<string> ParseExtensions(string extensionsText)
	{
		return extensionsText
			.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(x => x.Trim())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.StartsWith('.') ? x : $".{x}");
	}

	void SaveSettings()
	{
		if (_isLoadingSettings)
			return;

		new AppSettings
		{
			OnlyFilterExtensions = OnlyFilterExtensions,
			ExcludedFilterExtensions = ExcludedFilterExtensions,
			FilterMode = FilterMode,
		}.Save();
	}

	[StswCommand]
	void ClearFiles()
	{
		FileList.Clear();
	}

	[StswCommand]
	void ClearFile(FileInfoModel fileInfoModel)
	{
		FileList.Remove(fileInfoModel);
	}

	[StswCommand]
	void ClearSeparator()
	{
		SeparatorText = string.Empty;
	}

	[StswCommand]
	void ConvertFiles()
	{
		var fileContents = FileList.Select(f => File.ReadAllText(f.FilePath));
		OutputText = string.Join(SeparatorText, fileContents);
	}

	[StswCommand]
	void CopyOutput()
	{
		Clipboard.SetText(OutputText);
	}

	[StswCommand]
	void SaveOutput()
	{
		var dialog = new SaveFileDialog
		{
			DefaultExt = ".txt",
			Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
		};
		if (dialog.ShowDialog() != true)
			return;

		File.WriteAllText(dialog.FileName, OutputText);
	}
}
