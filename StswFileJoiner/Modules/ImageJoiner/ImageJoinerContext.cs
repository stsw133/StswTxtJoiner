using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StswFileJoiner;

public partial class ImageJoinerContext : StswObservableObject
{
	const int MaxBitmapDimension = 32767;

	[StswObservableProperty] ObservableCollection<FileInfoModel> _fileList = [];
	[StswObservableProperty] FileInfoModel? _selectedFileInfo;
	[StswObservableProperty] ImageJoinMode _joinMode = ImageJoinMode.Horizontal;
	[StswObservableProperty] ImageGridDefinitionMode _gridDefinitionMode = ImageGridDefinitionMode.Columns;
	[StswObservableProperty] int _gridSize = 2;
	[StswObservableProperty] int _spacing;
	[StswObservableProperty] BitmapSource? _outputImage;
	[StswObservableProperty] int _outputWidth;
	[StswObservableProperty] int _outputHeight;
	[StswObservableProperty] string _outputMessage = "Add images and click Join.";
	[StswObservableProperty] int _outputPreviewPixelLimit = AppSettings.DefaultImageOutputPreviewPixelLimit;
	[StswObservableProperty] string _onlyFilterExtensions = AppSettings.DefaultImageOnlyFilterExtensions;
	[StswObservableProperty] string _excludedFilterExtensions = string.Empty;
	[StswObservableProperty] FileFilterMode _filterMode = FileFilterMode.Only;
	bool _isLoadingSettings;

	public ImageJoinerContext()
	{
		_isLoadingSettings = true;
		var settings = AppSettings.Load();
		OnlyFilterExtensions = string.IsNullOrWhiteSpace(settings.ImageOnlyFilterExtensions)
			? AppSettings.DefaultImageOnlyFilterExtensions
			: settings.ImageOnlyFilterExtensions;
		ExcludedFilterExtensions = settings.ImageExcludedFilterExtensions ?? string.Empty;
		FilterMode = settings.ImageFilterMode;
		JoinMode = settings.ImageJoinMode;
		GridDefinitionMode = settings.ImageGridDefinitionMode;
		GridSize = Math.Max(1, settings.ImageGridSize);
		Spacing = Math.Max(0, settings.ImageSpacing);
		OutputPreviewPixelLimit = Math.Max(1, settings.ImageOutputPreviewPixelLimit);
		_isLoadingSettings = false;

		FileList.CollectionChanged += FileList_CollectionChanged;
	}

	public string FileListHeader => $"File list ({FileList.Count})";

	public long OutputPixelCount => (long)OutputWidth * OutputHeight;

	public string OutputHeader => OutputImage is null
		? "Output"
		: $"Output ({OutputWidth} × {OutputHeight}, {OutputPixelCount:N0} px)";

	public bool IsGridMode => JoinMode == ImageJoinMode.Grid;

	public bool IsOutputAvailable => OutputImage is not null;

	public bool IsOutputTooLarge => OutputImage is not null && OutputPixelCount > Math.Max(1, OutputPreviewPixelLimit);

	public bool IsOutputPreviewVisible => OutputImage is not null && !IsOutputTooLarge;

	public bool IsOutputMessageVisible => !IsOutputPreviewVisible;

	public BitmapSource? OutputPreviewImage => IsOutputPreviewVisible ? OutputImage : null;

	void FileList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
	{
		OnPropertyChanged(nameof(FileListHeader));
		InvalidateOutput();
	}

	partial void OnOnlyFilterExtensionsChanged(string oldValue, string newValue) => SaveSettings();

	partial void OnExcludedFilterExtensionsChanged(string oldValue, string newValue) => SaveSettings();

	partial void OnFilterModeChanged(FileFilterMode oldValue, FileFilterMode newValue) => SaveSettings();

	partial void OnJoinModeChanged(ImageJoinMode oldValue, ImageJoinMode newValue)
	{
		OnPropertyChanged(nameof(IsGridMode));
		InvalidateOutput();
		SaveSettings();
	}

	partial void OnGridDefinitionModeChanged(ImageGridDefinitionMode oldValue, ImageGridDefinitionMode newValue)
	{
		InvalidateOutput();
		SaveSettings();
	}

	partial void OnGridSizeChanged(int oldValue, int newValue)
	{
		if (newValue < 1)
		{
			GridSize = 1;
			return;
		}

		InvalidateOutput();
		SaveSettings();
	}

	partial void OnSpacingChanged(int oldValue, int newValue)
	{
		if (newValue < 0)
		{
			Spacing = 0;
			return;
		}

		InvalidateOutput();
		SaveSettings();
	}

	partial void OnOutputImageChanged(BitmapSource? oldValue, BitmapSource? newValue) => RefreshOutputProperties();

	partial void OnOutputWidthChanged(int oldValue, int newValue) => RefreshOutputProperties();

	partial void OnOutputHeightChanged(int oldValue, int newValue) => RefreshOutputProperties();

	partial void OnOutputPreviewPixelLimitChanged(int oldValue, int newValue)
	{
		if (newValue < 1)
		{
			OutputPreviewPixelLimit = 1;
			return;
		}

		RefreshOutputProperties();
		SaveSettings();
	}

	void RefreshOutputProperties()
	{
		OnPropertyChanged(nameof(OutputPixelCount));
		OnPropertyChanged(nameof(OutputHeader));
		OnPropertyChanged(nameof(IsOutputAvailable));
		OnPropertyChanged(nameof(IsOutputTooLarge));
		OnPropertyChanged(nameof(IsOutputPreviewVisible));
		OnPropertyChanged(nameof(IsOutputMessageVisible));
		OnPropertyChanged(nameof(OutputPreviewImage));

		if (OutputImage is not null)
			OutputMessage = IsOutputTooLarge
				? $"Output is too large to preview ({OutputWidth} × {OutputHeight}, {OutputPixelCount:N0} pixels). The current preview limit is {Math.Max(1, OutputPreviewPixelLimit):N0} pixels. You can still save the image."
				: string.Empty;
	}

	void InvalidateOutput()
	{
		OutputImage = null;
		OutputWidth = 0;
		OutputHeight = 0;
		OutputMessage = FileList.Count == 0
			? "Add images and click Join."
			: "The output is not up to date. Click Join to generate it.";
	}

	[StswCommand]
	void AddFiles()
	{
		var dialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.wdp;*.jxr)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.wdp;*.jxr|All files (*.*)|*.*"
		};
		if (dialog.ShowDialog() != true)
			return;

		AddFilesToList(dialog.FileNames);
	}

	[StswCommand]
	void DragFilesOver(DragEventArgs eventArgs)
	{
		var containsFiles = eventArgs.Data.GetDataPresent(DataFormats.FileDrop);
		if (!containsFiles)
			return;

		eventArgs.Effects = DragDropEffects.Copy;
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
			if (!FileList.Any(f => string.Equals(f.FilePath, fileInfo.FilePath, StringComparison.OrdinalIgnoreCase)))
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
				foreach (var fileName in EnumerateFilesSafely(path))
					yield return fileName;
		}
	}

	static IEnumerable<string> EnumerateFilesSafely(string directory)
	{
		IEnumerable<string> files = [];
		IEnumerable<string> directories = [];

		try
		{
			files = [.. Directory.EnumerateFiles(directory)];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}

		foreach (var file in files)
			yield return file;

		try
		{
			directories = [.. Directory.EnumerateDirectories(directory)];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}

		foreach (var subdirectory in directories)
			foreach (var file in EnumerateFilesSafely(subdirectory))
				yield return file;
	}

	bool IsFileAcceptedByFilter(string fileName)
	{
		var extensions = ParseExtensions(FilterMode == FileFilterMode.Only ? OnlyFilterExtensions : ExcludedFilterExtensions)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

		var settings = AppSettings.Load();
		settings.ImageOnlyFilterExtensions = OnlyFilterExtensions;
		settings.ImageExcludedFilterExtensions = ExcludedFilterExtensions;
		settings.ImageFilterMode = FilterMode;
		settings.ImageJoinMode = JoinMode;
		settings.ImageGridDefinitionMode = GridDefinitionMode;
		settings.ImageGridSize = Math.Max(1, GridSize);
		settings.ImageSpacing = Math.Max(0, Spacing);
		settings.ImageOutputPreviewPixelLimit = Math.Max(1, OutputPreviewPixelLimit);
		settings.Save();
	}

	[StswCommand]
	void ClearFiles() => FileList.Clear();

	[StswCommand]
	void ClearFile(FileInfoModel? fileInfoModel)
	{
		if (fileInfoModel is not null)
			FileList.Remove(fileInfoModel);
	}

	[StswCommand]
	void JoinImages()
	{
		if (FileList.Count == 0)
		{
			InvalidateOutput();
			return;
		}

		try
		{
			var images = FileList.Select(x => LoadBitmap(x.FilePath)).ToArray();
			var layout = CalculateLayout(images);
			ValidateOutputSize(layout.Width, layout.Height);

			var output = RenderImages(images, layout);
			output.Freeze();

			OutputWidth = output.PixelWidth;
			OutputHeight = output.PixelHeight;
			OutputImage = output;
		}
		catch (Exception ex) when (ex is IOException
			or UnauthorizedAccessException
			or NotSupportedException
			or FileFormatException
			or InvalidOperationException
			or OverflowException
			or ArgumentException
			or OutOfMemoryException)
		{
			OutputImage = null;
			OutputWidth = 0;
			OutputHeight = 0;
			OutputMessage = $"Unable to join images: {ex.Message}";
		}
	}

	static BitmapSource LoadBitmap(string filePath)
	{
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
		var source = decoder.Frames[0];

		BitmapSource result = source.Format == PixelFormats.Pbgra32
			? source
			: new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
		result.Freeze();
		return result;
	}

	ImageLayout CalculateLayout(IReadOnlyList<BitmapSource> images)
	{
		var spacing = Math.Max(0, Spacing);
		return JoinMode switch
		{
			ImageJoinMode.Horizontal => CalculateHorizontalLayout(images, spacing),
			ImageJoinMode.Vertical => CalculateVerticalLayout(images, spacing),
			ImageJoinMode.Grid => CalculateGridLayout(images, spacing),
			ImageJoinMode.Overlay => CalculateOverlayLayout(images),
			_ => throw new InvalidOperationException($"Unsupported join mode: {JoinMode}."),
		};
	}

	static ImageLayout CalculateHorizontalLayout(IReadOnlyList<BitmapSource> images, int spacing)
	{
		var width = checked(images.Sum(x => x.PixelWidth) + spacing * Math.Max(0, images.Count - 1));
		var height = images.Max(x => x.PixelHeight);
		var positions = new Point[images.Count];
		var x = 0;

		for (var index = 0; index < images.Count; index++)
		{
			positions[index] = new Point(x, (height - images[index].PixelHeight) / 2d);
			x = checked(x + images[index].PixelWidth + spacing);
		}

		return new ImageLayout(width, height, positions);
	}

	static ImageLayout CalculateVerticalLayout(IReadOnlyList<BitmapSource> images, int spacing)
	{
		var width = images.Max(x => x.PixelWidth);
		var height = checked(images.Sum(x => x.PixelHeight) + spacing * Math.Max(0, images.Count - 1));
		var positions = new Point[images.Count];
		var y = 0;

		for (var index = 0; index < images.Count; index++)
		{
			positions[index] = new Point((width - images[index].PixelWidth) / 2d, y);
			y = checked(y + images[index].PixelHeight + spacing);
		}

		return new ImageLayout(width, height, positions);
	}

	ImageLayout CalculateGridLayout(IReadOnlyList<BitmapSource> images, int spacing)
	{
		var requestedSize = Math.Max(1, GridSize);
		var isDefinedByColumns = GridDefinitionMode == ImageGridDefinitionMode.Columns;
		var columns = isDefinedByColumns
			? Math.Min(requestedSize, images.Count)
			: (int)Math.Ceiling(images.Count / (double)Math.Min(requestedSize, images.Count));
		var rows = isDefinedByColumns
			? (int)Math.Ceiling(images.Count / (double)columns)
			: Math.Min(requestedSize, images.Count);

		var cellWidth = images.Max(x => x.PixelWidth);
		var cellHeight = images.Max(x => x.PixelHeight);
		var width = checked(columns * cellWidth + spacing * Math.Max(0, columns - 1));
		var height = checked(rows * cellHeight + spacing * Math.Max(0, rows - 1));
		var positions = new Point[images.Count];

		for (var index = 0; index < images.Count; index++)
		{
			var column = isDefinedByColumns ? index % columns : index / rows;
			var row = isDefinedByColumns ? index / columns : index % rows;
			var cellX = column * (cellWidth + spacing);
			var cellY = row * (cellHeight + spacing);
			positions[index] = new Point(
				cellX + (cellWidth - images[index].PixelWidth) / 2d,
				cellY + (cellHeight - images[index].PixelHeight) / 2d);
		}

		return new ImageLayout(width, height, positions);
	}

	static ImageLayout CalculateOverlayLayout(IReadOnlyList<BitmapSource> images)
	{
		var width = images.Max(x => x.PixelWidth);
		var height = images.Max(x => x.PixelHeight);
		var positions = images
			.Select(x => new Point((width - x.PixelWidth) / 2d, (height - x.PixelHeight) / 2d))
			.ToArray();
		return new ImageLayout(width, height, positions);
	}

	static void ValidateOutputSize(int width, int height)
	{
		if (width <= 0 || height <= 0)
			throw new InvalidOperationException("The calculated output size is invalid.");
		if (width > MaxBitmapDimension || height > MaxBitmapDimension)
			throw new InvalidOperationException($"The output dimensions ({width} × {height}) exceed the supported limit of {MaxBitmapDimension} pixels per side.");

		_ = checked((long)width * height);
	}

	static RenderTargetBitmap RenderImages(IReadOnlyList<BitmapSource> images, ImageLayout layout)
	{
		var visual = new DrawingVisual();
		using (var context = visual.RenderOpen())
		{
			for (var index = 0; index < images.Count; index++)
			{
				var image = images[index];
				context.DrawImage(image, new Rect(layout.Positions[index], new Size(image.PixelWidth, image.PixelHeight)));
			}
		}

		var output = new RenderTargetBitmap(layout.Width, layout.Height, 96, 96, PixelFormats.Pbgra32);
		output.Render(visual);
		return output;
	}

	[StswCommand]
	void SaveOutput()
	{
		if (OutputImage is null)
		{
			OutputMessage = "Generate an output image before saving it.";
			return;
		}

		var dialog = new SaveFileDialog
		{
			DefaultExt = ".png",
			AddExtension = true,
			Filter = "PNG image (*.png)|*.png"
		};
		if (dialog.ShowDialog() != true)
			return;

		try
		{
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(OutputImage));
			using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
			encoder.Save(stream);
			OutputMessage = IsOutputTooLarge
				? $"Output saved to {dialog.FileName}. The image remains hidden because it exceeds the preview limit."
				: string.Empty;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
		{
			OutputMessage = $"Unable to save the image: {ex.Message}";
		}
	}

	readonly record struct ImageLayout(int Width, int Height, Point[] Positions);
}
