using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CrabModManager.Models;

public class PluginEntry : INotifyPropertyChanged {
	public string FolderName { get; init; } = "";
	public string DisplayName { get; init; } = "";
	public string Version { get; init; } = "";
	public string Description { get; init; } = "";
	public string FolderPath { get; init; } = "";
	public DateTime InstallDate { get; init; }
	public string InstallDateText => InstallDate == DateTime.MinValue ? "" : InstallDate.ToString("yyyy-MM-dd HH:mm");

	bool _enabled;
	public bool Enabled {
		get => _enabled;
		set { if (_enabled != value) { _enabled = value; OnChanged(); } }
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
