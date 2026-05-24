using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace CrabModManager.Services;

public enum BepInExState { NotInstalled, Installed }

public class BepInExInstaller {
	// Hardcoded known-good build for Unity 6 IL2CPP. Future versions can resolve dynamically
	// by scraping https://builds.bepinex.dev/projects/bepinex_be.
	const string DefaultUrl = "https://builds.bepinex.dev/projects/bepinex_be/755/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755%2B3fab71a.zip";

	public readonly string GameDir;
	public BepInExInstaller(string gameDir) { GameDir = gameDir; }

	public BepInExState GetState() {
		var doorstop = Path.Combine(GameDir, "winhttp.dll");
		var core = Path.Combine(GameDir, "BepInEx", "core", "BepInEx.Core.dll");
		return (File.Exists(doorstop) && File.Exists(core)) ? BepInExState.Installed : BepInExState.NotInstalled;
	}

	public string GetInstalledVersion() {
		try {
			var p = Path.Combine(GameDir, "BepInEx", "core", "BepInEx.Core.dll");
			if (!File.Exists(p)) return "(not installed)";
			var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(p);
			return info.ProductVersion ?? info.FileVersion ?? "(unknown)";
		} catch { return "(unknown)"; }
	}

	public async Task InstallAsync(IProgress<string>? log = null) {
		var url = DefaultUrl;
		log?.Report($"Downloading BepInEx: {url}");
		var tmp = Path.Combine(Path.GetTempPath(), $"bepinex_{Guid.NewGuid():N}.zip");
		try {
			using (var http = new HttpClient()) {
				http.Timeout = TimeSpan.FromMinutes(5);
				var bytes = await http.GetByteArrayAsync(url);
				await File.WriteAllBytesAsync(tmp, bytes);
				log?.Report($"Downloaded {bytes.Length / 1024 / 1024} MB.");
			}
			log?.Report($"Extracting into {GameDir}...");
			using (var zip = ZipFile.OpenRead(tmp)) {
				foreach (var entry in zip.Entries) {
					var dest = Path.Combine(GameDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
					if (entry.FullName.EndsWith("/")) {
						Directory.CreateDirectory(dest);
						continue;
					}
					Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
					entry.ExtractToFile(dest, overwrite: true);
				}
			}
			log?.Report("BepInEx installed. Launch the game once so it can generate interop DLLs.");
		} finally {
			try { File.Delete(tmp); } catch { /* ignore */ }
		}
	}
}
