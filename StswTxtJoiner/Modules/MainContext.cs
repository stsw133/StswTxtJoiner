using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

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

		foreach (var fileName in dialog.FileNames)
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
