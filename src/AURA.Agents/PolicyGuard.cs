using System;
using System.Collections.Generic;

namespace AURA.Agents;

public enum AuthorizationDecision
{
    Allowed,
    RequiresConfirmation,
    Blocked
}

public sealed record AuthorizationResult(
    AuthorizationDecision Decision,
    string Message = "");

/// <summary>
/// Política determinística para separar comandos seguros de ações que exigem confirmação.
/// Programas internos possuem uma allowlist explícita de capacidades.
/// </summary>
public sealed class PolicyGuard
{
    private readonly HashSet<string> _confirmationRequired = new(StringComparer.OrdinalIgnoreCase)
    {
        "execute",
        "delete_file",
        "shell"
    };

    private static readonly HashSet<string> AllowedProgramCapabilities = new(StringComparer.OrdinalIgnoreCase)
    {
        "android.device.read",
        "android.battery.read",
        "android.network.read"
    };

    public AuthorizationResult Authorize(string intent, string command)
    {
        if (string.Equals(intent, "blocked", StringComparison.OrdinalIgnoreCase))
            return new AuthorizationResult(AuthorizationDecision.Blocked, "Ação bloqueada pela política local.");

        if (_confirmationRequired.Contains(intent))
        {
            return new AuthorizationResult(
                AuthorizationDecision.RequiresConfirmation,
                "Ação de execução requer confirmação explícita.");
        }

        return new AuthorizationResult(AuthorizationDecision.Allowed);
    }

    public AuthorizationResult Authorize(IEnumerable<string> requiredCapabilities, string command)
    {
        if (requiredCapabilities == null)
            return new AuthorizationResult(AuthorizationDecision.Blocked, "Capacidades não informadas.");

        foreach (var capability in requiredCapabilities)
        {
            if (string.IsNullOrWhiteSpace(capability) || !AllowedProgramCapabilities.Contains(capability))
                return new AuthorizationResult(AuthorizationDecision.Blocked, $"Capacidade não autorizada: {capability}");
        }

        return new AuthorizationResult(AuthorizationDecision.Allowed);
    }
}
