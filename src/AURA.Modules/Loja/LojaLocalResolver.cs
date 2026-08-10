using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AURA.Core.Logging;
using AURA.Modules;

namespace AURA.Modules.Loja
{
    public sealed class LojaEntry
    {
        public string Id { get; set; } = string.Empty;
        public List<string> PayloadFiles { get; set; } = new List<string>();
    }

    public sealed class LojaLocalResolver
    {
        private static readonly Regex SafeName = new Regex("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly ILogger _logger;
        private readonly string _lojaRoot;
        private readonly string _packagesDir;
        private readonly string _pluginsRoot;
        private readonly Func<string, ModuleInfo?> _getById;

        public LojaLocalResolver(ILogger logger, string lojaRoot, string packagesDir, string pluginsRoot,
            Func<string, ModuleInfo?>? getById = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lojaRoot = Environment.ExpandEnvironmentVariables(lojaRoot ?? "~/AURA/loja");
            _packagesDir = Environment.ExpandEnvironmentVariables(packagesDir ?? "~/AURA/packages");
            _pluginsRoot = Environment.ExpandEnvironmentVariables(pluginsRoot ?? "~/AURA/plugins");
            _getById = getById ?? ModuleCatalog.GetById;
        }

        public IReadOnlyList<LojaEntry> ListAvailable()
        {
            if (!Directory.Exists(_lojaRoot))
            {
                return Array.Empty<LojaEntry>();
            }

            var dirs = Directory.GetDirectories(_lojaRoot);
            var list = new List<LojaEntry>();
            foreach (string dir in dirs)
            {
                string manifest = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifest))
                {
                    _logger.Warning($"Loja entry missing manifest: {dir}");
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(manifest);
                    var entry = JsonSerializer.Deserialize<LojaEntry>(json);
                    if (entry != null)
                    {
                        list.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to read manifest {manifest}: {ex.Message}");
                }
            }

            return list;
        }

        /// <summary>
        /// Install a module from the local loja by id.
        /// Throws if id is not present in the ModuleCatalog BEFORE copying anything.
        /// </summary>
        public void InstallFromLoja(string id, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id required", nameof(id));

            // locate manifest in loja
            string entryDir = Path.Combine(_lojaRoot, id);
            string manifestPath = Path.Combine(entryDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException($"No manifest for id '{id}' in loja.");
            }

            // validate catalog entry first (important: fail before touching files)
            ModuleInfo? info = _getById(id);
            if (info == null)
            {
                throw new InvalidOperationException($"Module id '{id}' not found in ModuleCatalog.");
            }

            // parse manifest
            string manifestJson = File.ReadAllText(manifestPath);
            LojaEntry manifest = JsonSerializer.Deserialize<LojaEntry>(manifestJson) ?? throw new InvalidOperationException("Invalid manifest");
            if (!string.Equals(manifest.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manifest id mismatch");
            }

            if (manifest.PayloadFiles == null || manifest.PayloadFiles.Count == 0)
            {
                throw new InvalidOperationException("Manifest has no payloadFiles");
            }

            // validate payload file names
            foreach (string f in manifest.PayloadFiles)
            {
                if (!SafeName.IsMatch(f))
                {
                    throw new InvalidOperationException("Invalid payload file name: " + f);
                }
            }

            // ensure packages dir for id
            string packageDirForId = Path.Combine(_packagesDir, id);
            Directory.CreateDirectory(packageDirForId);

            // acquire simple lock file for this id
            string lockPath = Path.Combine(packageDirForId, ".install.lock");
            using (FileStream? lockFs = TryAcquireLock(lockPath))
            {
                if (lockFs == null)
                {
                    throw new InvalidOperationException("Could not acquire install lock for id: " + id);
                }

                // ensure payload files exist in loja
                string payloadRoot = Path.Combine(entryDir, "payload");
                foreach (string f in manifest.PayloadFiles)
                {
                    string src = Path.Combine(payloadRoot, f);
                    if (!File.Exists(src))
                    {
                        throw new InvalidOperationException("Payload file missing: " + f);
                    }
                }

                // copy to temp dir under pluginsRoot
                string tmpInstallDir = Path.Combine(_pluginsRoot, ".tmp_install_" + id + "_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpInstallDir);

                try
                {
                    var installed = new List<string>();
                    foreach (string f in manifest.PayloadFiles)
                    {
                        string src = Path.Combine(payloadRoot, f);
                        string tmpDest = Path.Combine(tmpInstallDir, f);
                        File.Copy(src, tmpDest, overwrite: true);
                    }

                    // move files atomically to pluginsRoot flat
                    foreach (string f in manifest.PayloadFiles)
                    {
                        string tmpSrc = Path.Combine(tmpInstallDir, f);
                        string finalDest = Path.Combine(_pluginsRoot, f);

                        if (File.Exists(finalDest))
                        {
                            if (!overwrite)
                            {
                                throw new InvalidOperationException("Target file already exists: " + finalDest);
                            }
                            else
                            {
                                File.Delete(finalDest);
                            }
                        }

                        File.Move(tmpSrc, finalDest);
                        installed.Add(f);
                    }

                    // write module.json based on ModuleCatalog info (schema real)
                    string moduleJsonTmp = Path.Combine(packageDirForId, "module.json.tmp");
                    var moduleDoc = new
                    {
                        id = info.Id,
                        name = info.DisplayName,
                        version = info.PackageVersion,
                        description = info.ShortDescription,
                        features = info.Features,
                        pages = info.Includes
                    };

                    string moduleJson = JsonSerializer.Serialize(moduleDoc, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(moduleJsonTmp, moduleJson);
                    string moduleJsonFinal = Path.Combine(packageDirForId, "module.json");
                    if (File.Exists(moduleJsonFinal)) File.Delete(moduleJsonFinal);
                    File.Move(moduleJsonTmp, moduleJsonFinal);

                    // write installedFiles.json
                    string installedJsonTmp = Path.Combine(packageDirForId, "installedFiles.json.tmp");
                    string installedJson = JsonSerializer.Serialize(installed, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(installedJsonTmp, installedJson);
                    string installedJsonFinal = Path.Combine(packageDirForId, "installedFiles.json");
                    if (File.Exists(installedJsonFinal)) File.Delete(installedJsonFinal);
                    File.Move(installedJsonTmp, installedJsonFinal);

                    _logger.Info($"Installed module '{id}' with {installed.Count} files.");
                }
                catch
                {
                    // rollback: attempt to delete any tmp files and leave system consistent
                    try
                    {
                        if (Directory.Exists(tmpInstallDir)) Directory.Delete(tmpInstallDir, true);
                    }
                    catch { }

                    throw;
                }
                finally
                {
                    // release lock by disposing lockFs (using block)
                }
            }
        }

        private static FileStream? TryAcquireLock(string lockPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lockPath) ?? "");
                // create or open with exclusive lock
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                return null;
            }
        }
    }
}
