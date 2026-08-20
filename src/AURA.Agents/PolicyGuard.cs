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
    }
}
