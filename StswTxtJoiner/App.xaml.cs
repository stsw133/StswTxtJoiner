global using StswExpress.Commons;
global using StswExpress.Wpf;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;

namespace StswTxtJoiner;

public partial class App : StswApp
{
	public static IConfiguration Configuration { get; private set; } = null!;

	/// OnStartup
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		/// Configuration
		var configurationBuilder = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
		Configuration = configurationBuilder.Build();
		
		MainWindow = new MainWindow();
		MainWindow.Show();
	}

	/// Application_DispatcherUnhandledException
	private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		if (SynchronizationContext.Current is not null)
			StswMessageDialog.Show(e.Exception, MethodBase.GetCurrentMethod()?.Name, false);
		else
			StswLog.WriteException(e.Exception);
	}
}
