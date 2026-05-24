using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CrabModManager.Models;
using CrabModManager.Services;
using Microsoft.Win32;

namespace CrabModManager;

public partial class MainWindow : Window {
	readonly string _gameDir;
	readonly BepInExInstaller _installer;
	readonly PluginManager _plugins;
	readonly ObservableCollection<PluginEntry> _items = new();
	ICollectionView? _view;

	// Sort state. Default: enabled mods first, then by name.
	string _sortKey = "Name";
	ListSortDirection _sortDir = ListSortDirection.Ascending;

	public MainWindow() {
		InitializeComponent();
		var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory;
		_gameDir = Path.GetDirectoryName(exePath)!;
		_installer = new BepInExInstaller(_gameDir);
		_plugins = new PluginManager(_gameDir);

		_view = CollectionViewSource.GetDefaultView(_items);
		PluginList.ItemsSource = _view;
		ApplySort();

		Loaded += (_, _) => Refresh();
	}

	void Log(string msg) => Dispatcher.Invoke(() => LogLine.Text = msg);

	void Refresh() {
		var state = _installer.GetState();
		switch (state) {
			case BepInExState.Installed:
				StatusLabel.Text = $"BepInEx installed ({_installer.GetInstalledVersion()})";
				BtnInstallBepInEx.Content = "Reinstall BepInEx";
				break;
			default:
				StatusLabel.Text = "BepInEx not installed.";
				BtnInstallBepInEx.Content = "Install BepInEx";
				break;
		}

		var gameExe = Path.Combine(_gameDir, "Everything is Crab.exe");
		BtnLaunchGame.IsEnabled = File.Exists(gameExe);

		_items.Clear();
		foreach (var p in _plugins.Scan()) _items.Add(p);
		_view?.Refresh();
		UpdateHeaderLabels();
		Log($"Found {_items.Count} plugin(s).");
	}

	void ApplySort() {
		if (_view == null) return;
		_view.SortDescriptions.Clear();
		switch (_sortKey) {
			case "Enabled":
				_view.SortDescriptions.Add(new SortDescription(nameof(PluginEntry.Enabled), _sortDir));
				_view.SortDescriptions.Add(new SortDescription(nameof(PluginEntry.DisplayName), ListSortDirection.Ascending));
				break;
			case "InstallDate":
				_view.SortDescriptions.Add(new SortDescription(nameof(PluginEntry.InstallDate), _sortDir));
				break;
			case "Name":
			default:
				_view.SortDescriptions.Add(new SortDescription(nameof(PluginEntry.DisplayName), _sortDir));
				break;
		}
		UpdateHeaderLabels();
	}

	void UpdateHeaderLabels() {
		string Arrow(string key) => key == _sortKey ? (_sortDir == ListSortDirection.Ascending ? "  ▲" : "  ▼") : "";
		HdrEnabled.Content = $"Enable/Disable{Arrow("Enabled")}";
		HdrName.Content = $"Mod{Arrow("Name")}";
		HdrInstallDate.Content = $"Install Date{Arrow("InstallDate")}";
	}

	void OnSortHeader(object sender, RoutedEventArgs e) {
		if (sender is not Button btn || btn.Tag is not string key) return;
		if (_sortKey == key) {
			_sortDir = _sortDir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
		} else {
			_sortKey = key;
			_sortDir = ListSortDirection.Ascending;
		}
		ApplySort();
	}

	async void OnInstallBepInEx(object sender, RoutedEventArgs e) {
		var gameExe = Path.Combine(_gameDir, "Everything is Crab.exe");
		if (!File.Exists(gameExe)) {
			MessageBox.Show("Couldn't find 'Everything is Crab.exe' in this folder. " +
				"Place CrabModManager.exe in the game's install directory and try again.",
				"Wrong folder", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		BtnInstallBepInEx.IsEnabled = false;
		try {
			var progress = new Progress<string>(Log);
			await Task.Run(() => _installer.InstallAsync(progress));
			Refresh();
			MessageBox.Show(
				"BepInEx installed. Launch the game once and let it reach the main menu, " +
				"then close it. This generates the interop assemblies BepInEx mods compile against.",
				"BepInEx ready", MessageBoxButton.OK, MessageBoxImage.Information);
		} catch (Exception ex) {
			Log($"Install failed: {ex.Message}");
			MessageBox.Show(ex.Message, "Install failed", MessageBoxButton.OK, MessageBoxImage.Error);
		} finally {
			BtnInstallBepInEx.IsEnabled = true;
		}
	}

	void OnLaunchGame(object sender, RoutedEventArgs e) {
		try {
			var exe = Path.Combine(_gameDir, "Everything is Crab.exe");
			Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = _gameDir });
			Log("Launched game.");
		} catch (Exception ex) { Log($"Launch failed: {ex.Message}"); }
	}

	void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

	void OnCredits(object sender, RoutedEventArgs e) {
		var w = new CreditsWindow { Owner = this };
		w.ShowDialog();
	}

	void OnDragEnterDrop(object sender, DragEventArgs e) {
		e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		e.Handled = true;
	}

	void OnDrop(object sender, DragEventArgs e) {
		if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
		var files = (string[])e.Data.GetData(DataFormats.FileDrop);
		InstallZips(files);
	}

	void OnDropZoneClick(object sender, RoutedEventArgs e) {
		var dlg = new OpenFileDialog {
			Filter = "Mod archives (*.zip)|*.zip",
			Title = "Choose a mod archive to install",
			Multiselect = true,
		};
		if (dlg.ShowDialog() == true) InstallZips(dlg.FileNames);
	}

	void InstallZips(string[] paths) {
		int installed = 0;
		foreach (var p in paths.Where(p => p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))) {
			try {
				var name = _plugins.InstallFromZip(p);
				installed++;
				Log($"Installed: {name}");
			} catch (Exception ex) {
				Log($"Failed to install {Path.GetFileName(p)}: {ex.Message}");
				MessageBox.Show(ex.Message, $"Install failed: {Path.GetFileName(p)}",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		Refresh();
		Log($"Installed {installed} mod(s).");
	}

	void OnToggleEnabled(object sender, RoutedEventArgs e) {
		if (sender is not CheckBox cb || cb.Tag is not PluginEntry entry) return;
		try {
			var newState = cb.IsChecked == true;
			_plugins.SetEnabled(entry, newState);
			Log($"{(newState ? "Enabled" : "Disabled")}: {entry.DisplayName}");
			Refresh();
		} catch (Exception ex) {
			Log($"Toggle failed: {ex.Message}");
			Refresh();
		}
	}

	void OnUninstall(object sender, RoutedEventArgs e) {
		if (sender is not Button btn || btn.Tag is not PluginEntry entry) return;
		var result = MessageBox.Show(
			$"Uninstall '{entry.DisplayName}'? This deletes the plugin files.",
			"Uninstall mod", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (result != MessageBoxResult.Yes) return;
		try {
			_plugins.Uninstall(entry);
			Log($"Uninstalled: {entry.DisplayName}");
			Refresh();
		} catch (Exception ex) {
			Log($"Uninstall failed: {ex.Message}");
		}
	}
}
