using System.IO;
using System.Reflection;

namespace StswTxtJoiner;

public class FilesService
{
	public string AttachmentsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "attachments");
	public string PreviewsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "previews");
	public string ReportsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "reports");
	public string HelpFilePath { get; } = Path.Combine(AppContext.BaseDirectory, @"Resources\tutorial.pdf");

	public FilesService()
	{
		CreateDirectoriesOnEnter();
		App.Current.Exit += (_, _) => DeleteDirectoriesOnExit();
	}

	/// CreateDirectoriesOnEnter
	public void CreateDirectoriesOnEnter()
	{
		if (!Directory.Exists(AttachmentsDirectory))
			Directory.CreateDirectory(AttachmentsDirectory);

		if (!Directory.Exists(PreviewsDirectory))
			Directory.CreateDirectory(PreviewsDirectory);

		if (!Directory.Exists(ReportsDirectory))
			Directory.CreateDirectory(ReportsDirectory);
	}

	/// DeleteDirectoriesOnExit
	public void DeleteDirectoriesOnExit()
	{
		if (Directory.Exists(AttachmentsDirectory))
			Directory.Delete(AttachmentsDirectory, true);

		if (Directory.Exists(PreviewsDirectory))
			Directory.Delete(PreviewsDirectory, true);

		if (Directory.Exists(ReportsDirectory))
			Directory.Delete(ReportsDirectory, true);
	}

	/// OpenHelpFile
	public async Task OpenHelpFile()
	{
		try
		{
			if (File.Exists(HelpFilePath))
				StswFn.OpenPath(HelpFilePath);
		}
		catch (Exception ex)
		{
			await StswMessageDialog.Show(ex, $"Method error: {MethodBase.GetCurrentMethod()?.Name}");
		}
	}
}
