using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using AURA.Core.Logging;

namespace AURA.Modules.Loja
{
    /// <summary>
    /// Responsible for uninstalling a package previously installed via LojaLocalResolver.
    /// It reads packages/<id>/installedFiles.json and deletes the listed files from the
    /// plugins root, then removes the package directory.
    /// </summary>
    public sealed class LojaUninstaller
    {
        private static readonly Regex SafeName = new Regex("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);
        private readonly ILogger _logger;
        private readonly string _packagesDir;
        private readonly string _pluginsRoot;

        public LojaUninstaller(ILogger logger, string packagesDir, string pluginsRoot)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _packagesDir = packagesDir ?? throw new ArgumentNullException(nameof(packagesDir));
            _pluginsRoot = pluginsRoot ?? throw new ArgumentNullException(nameof(pluginsRoot));
        }

        public void Uninstall(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id required", nameof(id));

            string packageDir = Path.Combine(_packagesDir, id);
            string installedJsonPath = Path.Combine(packageDir, "installedFiles.json");

            if (!Directory.Exists(packageDir) || !File.Exists(installedJsonPath))
            {
                throw new InvalidOperationException($"installedFiles.json not found for id: {id}");
            }

            List<string>? files;
            try
            {
                string json = File.ReadAllText(installedJsonPath);
                files = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to read installedFiles.json for {id}: {ex.Message}");
                throw new InvalidOperationException("Invalid installedFiles.json", ex);
            }

            foreach (var f in files)
            {
                if (!SafeName.IsMatch(f))
                {
                    _logger.Warning($"Skipping unsafe installed file name when uninstalling {id}: {f}");
                    continue;
                }

                try
                {
                    string target = Path.Combine(_pluginsRoot, f);
                    if (File.Exists(target))
                    {
                        File.Delete(target);
                        _logger.Info($"Deleted installed file: {target}");
                    }
                    else
                    {
                        _logger.Warning($"Installed file missing during uninstall: {target}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to delete installed file {f} for {id}: {ex.Message}");
                }
            }

            // attempt to delete the package directory
            try
            {
                if (Directory.Exists(packageDir))
                {
                    Directory.Delete(packageDir, recursive: true);
                    _logger.Info($"Deleted package directory: {packageDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to delete package directory {packageDir}: {ex.Message}");
            }
        }
    }
}
