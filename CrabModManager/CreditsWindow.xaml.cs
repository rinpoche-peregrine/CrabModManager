using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CrabModManager;

public partial class CreditsWindow : Window {
	public CreditsWindow() { InitializeComponent(); }

	void OnNavigate(object sender, RequestNavigateEventArgs e) {
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
		e.Handled = true;
	}

	void OnClose(object sender, RoutedEventArgs e) => Close();
}
