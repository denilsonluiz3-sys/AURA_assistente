using System.IO;

namespace AURA.Mobile.Diagnostics
{
    /// <summary>
    /// Workspace do agente. Preferência: /storage/emulated/0/Download/AURA
    /// (visível e editável no gerenciador de arquivos). Fallback: pasta privada do app.
    /// </summary>
    public static class AgentWorkspace
    {
        public const string PublicFolderName = "AURA";

        /// <summary>Pasta privada legada (sem permissão extra).</summary>
        public static string WorkspaceRoot => Path.Combine(FileSystem.AppDataDirectory, "workspace");

        /// <summary>Caminho público desejado (Download/AURA).</summary>
        public static string PublicWorkspaceRoot
        {
            get
            {
                try
                {
#if ANDROID
                    var downloads = Android.OS.Environment
                        .GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)
                        ?.AbsolutePath;
                    if (!string.IsNullOrWhiteSpace(downloads))
                        return Path.Combine(downloads, PublicFolderName);
#endif
                    return Path.Combine("/storage/emulated/0/Download", PublicFolderName);
                }
                catch
                {
                    return Path.Combine("/storage/emulated/0/Download", PublicFolderName);
                }
            }
        }

        /// <summary>
        /// Raiz ativa: projeto vinculado → Download/AURA se gravável → privado.
        /// </summary>
        public static string ActiveRoot
        {
            get
            {
                if (ProjectAccessService.IsLinked)
                    return ProjectAccessService.ProjectWorkspaceRoot;

                string pub = PublicWorkspaceRoot;
                if (TryEnsureWritable(pub))
                    return pub;

                return EnsurePrivate();
            }
        }

        public static string EnsureCreated()
        {
            string root = ActiveRoot;
            try
            {
                Directory.CreateDirectory(root);
                WriteReadme(root);
            }
            catch
            {
                root = EnsurePrivate();
            }
            return root;
        }

        public static int CountFiles(string? root = null)
        {
            root ??= ActiveRoot;
            if (!Directory.Exists(root))
                return 0;
            try
            {
                return Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        public static string Describe()
        {
            string root = ActiveRoot;
            bool isPublic = root.Contains("/Download/", StringComparison.OrdinalIgnoreCase);
            return (isPublic ? "Público: " : "Privado: ") + root;
        }

        private static string EnsurePrivate()
        {
            Directory.CreateDirectory(WorkspaceRoot);
            WriteReadme(WorkspaceRoot);
            return WorkspaceRoot;
        }

        private static bool TryEnsureWritable(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string probe = Path.Combine(path, ".aura_write_test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                WriteReadme(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteReadme(string root)
        {
            try
            {
                string readme = Path.Combine(root, "README_AURA.txt");
                if (!File.Exists(readme))
                {
                    File.WriteAllText(readme,
                        "Workspace da AURA\n" +
                        "Pasta visível em Download/AURA.\n" +
                        "O agente lê e grava arquivos aqui.\n");
                }
            }
            catch { /* ignore */ }
        }
    }
}
