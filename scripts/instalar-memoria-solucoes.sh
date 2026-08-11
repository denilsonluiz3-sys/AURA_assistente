#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="$ROOT/.aura/backup-solutions-$STAMP"
LOG="$ROOT/.aura/solutions-install-$STAMP.log"

mkdir -p "$BACKUP_DIR"

exec > >(tee -a "$LOG") 2>&1

echo "=============================================="
echo " AURA - Memória de Soluções"
echo "=============================================="
echo "ROOT: $ROOT"
echo "DATA: $STAMP"
echo

fail() {
    echo
    echo "[ERRO] $1"
    echo "[ERRO] Log: $LOG"
    exit 1
}

echo "[1/10] Verificando Git..."

git rev-parse --is-inside-work-tree >/dev/null 2>&1 \
    || fail "Não estamos dentro de um repositório Git."

BRANCH="$(git branch --show-current)"
echo "Branch: $BRANCH"

[ -n "$BRANCH" ] || fail "Não foi possível identificar a branch."

echo
echo "[2/10] Verificando estado atual..."

git status --short

echo
echo "[3/10] Criando backup..."

# Backup do estado Git
git diff > "$BACKUP_DIR/working-tree.patch" || true
git diff --cached > "$BACKUP_DIR/staged.patch" || true
git status --short > "$BACKUP_DIR/status.txt" || true

# Backup dos arquivos que vamos criar, caso algum já exista.
for FILE in \
    "src/AURA.Memory/RequestContext.cs" \
    "src/AURA.Memory/SolutionRule.cs" \
    "src/AURA.Memory/SolutionStore.cs"
do
    if [ -f "$FILE" ]; then
        mkdir -p "$BACKUP_DIR/$(dirname "$FILE")"
        cp "$FILE" "$BACKUP_DIR/$FILE" \
            || fail "Falha ao fazer backup de $FILE"
    fi
done

echo "Backup: $BACKUP_DIR"

echo
echo "[4/10] Localizando projeto para build..."

PROJECT=""

if [ -f "$ROOT/AURA.sln" ]; then
    PROJECT="$ROOT/AURA.sln"
elif [ -f "$ROOT/AURA.slnx" ]; then
    PROJECT="$ROOT/AURA.slnx"
else
    PROJECT="$(find "$ROOT" -maxdepth 3 \
        \( -name "*.sln" -o -name "*.slnx" \) \
        -print -quit)"
fi

if [ -z "$PROJECT" ]; then
    PROJECT="$ROOT/src/AURA.CLI/AURA.CLI.csproj"
fi

[ -f "$PROJECT" ] || fail "Nenhuma solution/projeto encontrado."

echo "Build alvo: $PROJECT"

echo
echo "[5/10] BUILD BASELINE..."
echo "Nenhuma alteração será feita se o build atual já estiver quebrado."

if ! dotnet build "$PROJECT" --nologo; then
    fail "O projeto já falhava antes da alteração. Nenhuma modificação foi aplicada."
fi

echo
echo "[6/10] Criando camada de conhecimento..."

mkdir -p "$ROOT/src/AURA.Memory"

cat > "$ROOT/src/AURA.Memory/RequestContext.cs" <<'CS'
using System;
using System.Collections.Generic;

namespace AURA.Memory
{
    /// <summary>
    /// Representa a solicitação do usuário em formato estruturado.
    /// A intenção é separar a solicitação textual dos procedimentos
    /// que a AURA já conhece e consegue executar.
    /// </summary>
    public sealed class RequestContext
    {
        public string Intent { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public string Workspace { get; set; } = string.Empty;

        public List<string> Files { get; set; } = new List<string>();

        public List<string> Constraints { get; set; } = new List<string>();

        public List<string> Validation { get; set; } = new List<string>();

        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool RequiresAiFallback { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
CS

cat > "$ROOT/src/AURA.Memory/SolutionRule.cs" <<'CS'
using System;
using System.Collections.Generic;

namespace AURA.Memory
{
    /// <summary>
    /// Procedimento conhecido e validado pela AURA.
    ///
    /// Uma regra só deve ser marcada como validada depois que sua execução
    /// produzir o resultado esperado.
    /// </summary>
    public sealed class SolutionRule
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Intent { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public List<string> Steps { get; set; } =
            new List<string>();

        public List<string> ValidationSteps { get; set; } =
            new List<string>();

        public bool Validated { get; set; }

        public int SuccessCount { get; set; }

        public DateTime? LastValidatedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
CS

cat > "$ROOT/src/AURA.Memory/SolutionStore.cs" <<'CS'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AURA.Core.Logging;
using AURA.Core.Runtime;

namespace AURA.Memory
{
    /// <summary>
    /// Armazena somente procedimentos conhecidos pela AURA.
    ///
    /// Diferente do histórico de conversa, este armazenamento representa
    /// conhecimento operacional reutilizável.
    /// </summary>
    public sealed class SolutionStore
    {
        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            };

        private readonly ILogger _logger;
        private readonly string _path;
        private readonly object _sync = new object();

        public SolutionStore(
            ILogger? logger = null,
            string? path = null)
        {
            _logger = logger ?? new ConsoleLogger();

            _path = path ??
                SimulationRuntime.ExpandUserHome(
                    "~/.aura/solutions.json");
        }

        public string Path => _path;

        public IReadOnlyList<SolutionRule> ReadAll()
        {
            lock (_sync)
            {
                return LoadLocked()
                    .Where(x => x.Validated)
                    .ToList();
            }
        }

        public SolutionRule? Find(
            string intent,
            string target,
            string goal)
        {
            lock (_sync)
            {
                return LoadLocked()
                    .Where(x => x.Validated)
                    .OrderByDescending(x => x.SuccessCount)
                    .FirstOrDefault(x =>
                        Same(x.Intent, intent) &&
                        Same(x.Target, target) &&
                        Same(x.Goal, goal));
            }
        }

        public void SaveValidated(SolutionRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (!rule.Validated)
                throw new InvalidOperationException(
                    "Somente soluções validadas podem ser armazenadas.");

            if (string.IsNullOrWhiteSpace(rule.Id))
                throw new InvalidOperationException(
                    "A solução precisa de um Id.");

            lock (_sync)
            {
                List<SolutionRule> all = LoadLocked();

                SolutionRule? existing =
                    all.FirstOrDefault(x =>
                        string.Equals(
                            x.Id,
                            rule.Id,
                            StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    all.Add(rule);
                }
                else
                {
                    int index = all.IndexOf(existing);
                    all[index] = rule;
                }

                PersistLocked(all);
            }
        }

        public void RegisterSuccess(
            string id,
            DateTime? validatedAtUtc = null)
        {
            lock (_sync)
            {
                List<SolutionRule> all = LoadLocked();

                SolutionRule? rule =
                    all.FirstOrDefault(x =>
                        string.Equals(
                            x.Id,
                            id,
                            StringComparison.OrdinalIgnoreCase));

                if (rule == null)
                    return;

                rule.Validated = true;
                rule.SuccessCount++;
                rule.LastValidatedAtUtc =
                    validatedAtUtc ?? DateTime.UtcNow;

                PersistLocked(all);
            }
        }

        private List<SolutionRule> LoadLocked()
        {
            try
            {
                if (!File.Exists(_path))
                    return new List<SolutionRule>();

                string json = File.ReadAllText(_path);

                return JsonSerializer.Deserialize<
                    List<SolutionRule>>(json, Options)
                    ?? new List<SolutionRule>();
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Falha ao carregar soluções em '" +
                    _path + "': " +
                    ex.Message);

                return new List<SolutionRule>();
            }
        }

        private void PersistLocked(
            List<SolutionRule> rules)
        {
            string? directory =
                System.IO.Path.GetDirectoryName(_path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json =
                JsonSerializer.Serialize(rules, Options);

            string tmp = _path + ".tmp";

            File.WriteAllText(tmp, json);

            try
            {
                File.Move(
                    tmp,
                    _path,
                    overwrite: true);
            }
            catch
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);

                throw;
            }
        }

        private static bool Same(
            string? a,
            string? b)
        {
            return string.Equals(
                a?.Trim(),
                b?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
CS

echo "Arquivos criados."

echo
echo "[7/10] BUILD DA ALTERAÇÃO..."

if ! dotnet build "$PROJECT" --nologo; then
    echo
    echo "[ERRO] Build falhou."
    echo "[ROLLBACK] Removendo os arquivos novos..."

    for FILE in \
        "src/AURA.Memory/RequestContext.cs" \
        "src/AURA.Memory/SolutionRule.cs" \
        "src/AURA.Memory/SolutionStore.cs"
    do
        if [ -f "$BACKUP_DIR/$FILE" ]; then
            cp "$BACKUP_DIR/$FILE" "$FILE"
        else
            rm -f "$FILE"
        fi
    done

    echo "[ROLLBACK] Concluído."
    echo "Backup: $BACKUP_DIR"
    exit 1
fi

echo
echo "[8/10] Verificando alterações..."

git status --short

git diff --check \
    || fail "Git encontrou problemas de whitespace."

echo
echo "[9/10] Commit..."

git add \
    src/AURA.Memory/RequestContext.cs \
    src/AURA.Memory/SolutionRule.cs \
    src/AURA.Memory/SolutionStore.cs

if git diff --cached --quiet; then
    echo "[INFO] Nenhuma alteração para commit."
else
    git commit \
        -m "feat: adiciona memória de soluções validadas" \
        || fail "Commit falhou."
fi

echo
echo "[10/10] PUSH..."

git push origin HEAD \
    || fail "Push falhou. As alterações continuam no commit local."

echo
echo "=============================================="
echo " SUCESSO"
echo "=============================================="
echo "Branch: $BRANCH"
echo "Backup: $BACKUP_DIR"
echo "Log:    $LOG"
echo
echo "Memória operacional criada:"
echo "  src/AURA.Memory/RequestContext.cs"
echo "  src/AURA.Memory/SolutionRule.cs"
echo "  src/AURA.Memory/SolutionStore.cs"
echo
echo "Build: OK"
echo "Commit: enviado ao GitHub"
echo
echo "PRÓXIMA FASE:"
echo "Conectar SolutionStore ao AgentSession."
echo "=============================================="
