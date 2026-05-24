using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using CrabModManager.Models;

namespace CrabModManager.Services;

public class PluginManager {
	public readonly string PluginsDir;
	public readonly string DisabledDir;

	public PluginManager(string gameDir) {
		PluginsDir = Path.Combine(gameDir, "BepInEx", "plugins");
		DisabledDir = Path.Combine(gameDir, "BepInEx", "plugins-disabled");
	}

	public IReadOnlyList<PluginEntry> Scan() {
		var entries = new List<PluginEntry>();
		ScanFolder(PluginsDir, enabled: true, entries);
		ScanFolder(DisabledDir, enabled: false, entries);
		return entries.OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
	}

	void ScanFolder(string root, bool enabled, List<PluginEntry> into) {
		if (!Directory.Exists(root)) return;
		// Each immediate subfolder is treated as a plugin package (Thunderstore convention).
		foreach (var dir in Directory.EnumerateDirectories(root)) {
			var hasDll = Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories).Any();
			if (!hasDll) continue;
			var entry = ReadManifest(dir) ?? new PluginEntry {
				DisplayName = Path.GetFileName(dir),
				Version = "",
				Description = "",
			};
			// init-only props are set above; clone-then-mutate via a fresh record
			into.Add(new PluginEntry {
				FolderName = Path.GetFileName(dir),
				DisplayName = entry.DisplayName,
				Version = entry.Version,
				Description = entry.Description,
				FolderPath = dir,
				Enabled = enabled,
			});
		}
		// Also surface loose .dll files at the root (legacy plugins).
		foreach (var dll in Directory.EnumerateFiles(root, "*.dll")) {
			into.Add(new PluginEntry {
				FolderName = Path.GetFileName(dll),
				DisplayName = Path.GetFileNameWithoutExtension(dll),
				Version = "",
				Description = "(loose DLL)",
				FolderPath = dll,
				Enabled = enabled,
			});
		}
	}

	PluginEntry? ReadManifest(string folder) {
		var path = Path.Combine(folder, "manifest.json");
		if (!File.Exists(path)) return null;
		try {
			using var s = File.OpenRead(path);
			using var doc = JsonDocument.Parse(s);
			var root = doc.RootElement;
			return new PluginEntry {
				DisplayName = root.TryGetProperty("name", out var n) ? n.GetString() ?? Path.GetFileName(folder) : Path.GetFileName(folder),
				Version = root.TryGetProperty("version_number", out var v) ? v.GetString() ?? "" : "",
				Description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
			};
		} catch { return null; }
	}

	public void SetEnabled(PluginEntry entry, bool enable) {
		Directory.CreateDirectory(PluginsDir);
		Directory.CreateDirectory(DisabledDir);
		var dest = Path.Combine(enable ? PluginsDir : DisabledDir, entry.FolderName);
		if (File.Exists(entry.FolderPath)) {
			// Loose DLL.
			File.Move(entry.FolderPath, dest, overwrite: true);
		} else {
			if (Directory.Exists(dest)) {
				// Shouldn't normally happen — but if it does, delete the destination first.
				Directory.Delete(dest, recursive: true);
			}
			Directory.Move(entry.FolderPath, dest);
		}
	}

	public void Uninstall(PluginEntry entry) {
		if (File.Exists(entry.FolderPath)) File.Delete(entry.FolderPath);
		else if (Directory.Exists(entry.FolderPath)) Directory.Delete(entry.FolderPath, recursive: true);
	}

	// Returns the installed plugin name (best-effort).
	public string InstallFromZip(string zipPath) {
		Directory.CreateDirectory(PluginsDir);
		using var zip = ZipFile.OpenRead(zipPath);

		// Look for plugins/<package>/*.dll inside the zip — Thunderstore convention.
		var pluginEntries = zip.Entries.Where(e => e.FullName.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase)
			|| e.FullName.StartsWith("plugins\\", StringComparison.OrdinalIgnoreCase)).ToList();

		string targetFolder;

		if (pluginEntries.Count > 0) {
			// Thunderstore-style: take everything under plugins/<package>/ and copy into BepInEx/plugins/<package>/
			var pkg = pluginEntries
				.Select(e => e.FullName.Replace('\\', '/').Split('/'))
				.Where(parts => parts.Length >= 2)
				.Select(parts => parts[1])
				.FirstOrDefault(s => !string.IsNullOrEmpty(s));
			pkg ??= Path.GetFileNameWithoutExtension(zipPath);
			targetFolder = Path.Combine(PluginsDir, pkg);
			Directory.CreateDirectory(targetFolder);
			foreach (var e in pluginEntries) {
				var rel = e.FullName.Replace('\\', '/');
				// strip "plugins/<pkg>/" prefix
				var prefix = $"plugins/{pkg}/";
				if (!rel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
				var sub = rel.Substring(prefix.Length);
				if (string.IsNullOrEmpty(sub)) continue;
				var dest = Path.Combine(targetFolder, sub.Replace('/', Path.DirectorySeparatorChar));
				if (e.FullName.EndsWith("/")) { Directory.CreateDirectory(dest); continue; }
				Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
				e.ExtractToFile(dest, overwrite: true);
			}
			return pkg;
		}

		// Fallback: assume the whole zip is the plugin folder; use zip filename as folder name.
		var pkgName = Path.GetFileNameWithoutExtension(zipPath);
		targetFolder = Path.Combine(PluginsDir, pkgName);
		Directory.CreateDirectory(targetFolder);
		foreach (var e in zip.Entries) {
			var dest = Path.Combine(targetFolder, e.FullName.Replace('/', Path.DirectorySeparatorChar));
			if (e.FullName.EndsWith("/")) { Directory.CreateDirectory(dest); continue; }
			Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
			e.ExtractToFile(dest, overwrite: true);
		}
		return pkgName;
	}
}
