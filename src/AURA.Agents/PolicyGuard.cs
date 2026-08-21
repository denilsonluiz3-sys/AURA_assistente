using System;
using System.Collections.Generic;

namespace AURA.Agents
{
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
            if (requiredCapabilities is null)
                return new AuthorizationResult(AuthorizationDecision.Blocked, "Capacidades não informadas.");

            foreach (string capability in requiredCapabilities)
            {
                if (!AllowedProgramCapabilities.Contains(capability))
                {
                    return new AuthorizationResult(
                        AuthorizationDecision.Blocked,
                        $"Capacidade não autorizada: {capability}");
                }
            }

            return new AuthorizationResult(AuthorizationDecision.Allowed);
        }
    }
}
