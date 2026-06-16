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

	[StswCommand]
	void AddFiles()
	{
		var dialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
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
			AddFilesToList(fileNames);

		eventArgs.Handled = true;
	}

	void AddFilesToList(IEnumerable<string> fileNames)
	{
		foreach (var fileName in fileNames.Where(File.Exists))

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
	void ConvertFiles()
	{
		var fileContents = FileList.Select(f => File.ReadAllText(f.FilePath));
		OutputText = string.Join(SeparatorText, fileContents);
	}
}
