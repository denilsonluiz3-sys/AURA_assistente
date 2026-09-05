using AURA.Modules.Executors;

namespace AURA.Mobile.Diagnostics;

/// <summary>Diagnóstico rápido de shell + workspace.</summary>
public static class ShellStatus
{
    public static string Report()
    {
        string shellLine = ShellExecutor.DescribeAvailability();
        string ws = AgentWorkspace.Describe();
        string perm;
        try
        {
            perm = StoragePermissionHelper.IsAllFilesAccessGranted()
                ? "Armazenamento: Todos os arquivos OK"
                : "Armazenamento: conceda 'Todos os arquivos' para Download/AURA";
        }
        catch
        {
            perm = "Armazenamento: verifique permissão Todos os arquivos";
        }

        return shellLine + "\n" + ws + "\n" + perm +
               "\nADB: só via PC (adb shell). In-app: sh/toybox/Termux.";
    }
}
