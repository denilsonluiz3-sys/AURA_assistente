using AURA.Modules.Executors;

namespace AURA.Mobile.Diagnostics;

/// <summary>Diagnóstico rápido de shell + workspace para o Agente.</summary>
public static class ShellStatus
{
    public static string Report()
    {
        var shell = new ShellExecutor();
        string shellLine = ShellExecutor.DescribeAvailability();
        string ws = AgentWorkspace.Describe();
        string perm = StoragePermissionHelper.IsAllFilesAccessGranted()
            ? "Armazenamento: Todos os arquivos OK"
            : "Armazenamento: peça 'Todos os arquivos' (Download/AURA)";

        return shellLine + "\n" + ws + "\n" + perm +
               "\nAlternativas shell: Termux (bash); ADB só no PC (adb shell); toybox sh.";
    }
}
