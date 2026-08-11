#!/usr/bin/env bash
set -u

LOG="$HOME/aura-project-access-rebuild-$(date +%Y%m%d_%H%M%S).log"
BRANCH="feat/project-access-clean"
REMOTE="origin"
BASE="origin/main"

# Commits funcionais identificados anteriormente
COMMITS=(
  606081a
  8363fcc
  c6d42fc
)

exec > >(tee -a "$LOG") 2>&1

echo "============================================================"
echo " AURA — RECONSTRUÇÃO LIMPA PROJECT ACCESS"
echo "============================================================"
echo "Log: $LOG"
echo

fail() {
    echo
    echo "============================================================"
    echo " ERRO — PROCESSO PARADO"
    echo "============================================================"
    echo "$1"
    echo
    echo "Estado:"
    git status --short
    echo
    echo "Conflitos:"
    git diff --name-only --diff-filter=U 2>/dev/null || true
    echo
    echo "Log: $LOG"
    exit 1
}

echo "[1/10] Atualizando referências..."
git fetch "$REMOTE" --prune || fail "git fetch falhou."

echo
echo "[2/10] Criando backup COMPLETO do estado atual..."

BACKUP="$HOME/aura-backup-before-clean-$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP"

git status --short > "$BACKUP/status.txt" 2>&1 || true
git diff > "$BACKUP/tracked.diff" 2>&1 || true
git diff --cached > "$BACKUP/staged.diff" 2>&1 || true

# Guarda tudo que está no diretório, inclusive untracked.
git stash push -u -m "AURA backup antes reconstrução Project Access $(date +%Y%m%d_%H%M%S)" \
    || fail "Não foi possível criar backup via git stash."

git stash list -1 > "$BACKUP/stash.txt"

echo "[PASS] Backup criado:"
echo "$BACKUP"
echo

echo "[3/10] Encerrando qualquer cherry-pick interrompido..."

if [ -f .git/CHERRY_PICK_HEAD ] || [ -d .git/sequencer ]; then
    git cherry-pick --abort || true
fi

git status --short
echo

echo "[4/10] Verificando branch limpa..."

if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
    echo "[INFO] Branch $BRANCH já existe."

    git switch "$BRANCH" || fail "Não foi possível mudar para $BRANCH."

    echo "[INFO] Reposicionando branch em $BASE..."
    git reset --hard "$BASE" || fail "Reset da branch falhou."
else
    git switch -c "$BRANCH" "$BASE" \
        || fail "Não foi possível criar $BRANCH a partir de $BASE."
fi

git clean -fdn > "$BACKUP/clean-preview.txt"

echo "[PASS] Base limpa em $BASE"
echo

echo "[5/10] Aplicando commits funcionais..."

for COMMIT in "${COMMITS[@]}"; do
    echo
    echo "------------------------------------------------------------"
    echo "Cherry-pick: $COMMIT"
    echo "------------------------------------------------------------"

    if ! git cat-file -e "$COMMIT^{commit}" 2>/dev/null; then
        fail "Commit $COMMIT não existe neste clone."
    fi

    if ! git cherry-pick "$COMMIT"; then
        echo
        echo "============================================================"
        echo " CONFLITO NO COMMIT $COMMIT"
        echo "============================================================"
        echo
        echo "Arquivos conflitantes:"
        git diff --name-only --diff-filter=U
        echo
        echo "Resumo:"
        git status --short
        echo
        echo "Detalhes:"
        git diff --check || true
        echo
        echo "O backup está em:"
        echo "$BACKUP"
        echo
        echo "O script PAROU. Nenhum conflito será resolvido automaticamente."
        exit 2
    fi
done

echo
echo "[PASS] Todos os commits aplicados."
echo

echo "[6/10] Verificando conflitos..."
CONFLICTS=$(git diff --name-only --diff-filter=U)

if [ -n "$CONFLICTS" ]; then
    echo "$CONFLICTS"
    fail "Ainda existem conflitos."
fi

git diff --check || fail "Problemas de whitespace encontrados."

echo "[PASS] Sem conflitos."
echo

echo "[7/10] Build..."
if ! dotnet build; then
    echo
    echo "============================================================"
    echo " BUILD FALHOU"
    echo "============================================================"
    echo "Branch: $BRANCH"
    echo "Log: $LOG"
    exit 3
fi

echo
echo "[PASS] BUILD"
echo

echo "[8/10] Testes..."
if ! dotnet test; then
    echo
    echo "============================================================"
    echo " TESTES FALHARAM"
    echo "============================================================"
    echo "Branch: $BRANCH"
    echo "Log: $LOG"
    exit 4
fi

echo
echo "[PASS] TESTES"
echo

echo "[9/10] Auditoria final..."

echo "=== BRANCH ==="
git branch --show-current

echo
echo "=== COMMITS ==="
git log --oneline --decorate -8

echo
echo "=== ALTERAÇÕES ==="
git status --short

echo
echo "=== DIFF STAT ==="
git diff "$BASE...HEAD" --stat

echo
echo "[10/10] Push..."

git push -u "$REMOTE" "$BRANCH" \
    || fail "Push falhou."

echo
echo "============================================================"
echo " RECONSTRUÇÃO CONCLUÍDA"
echo "============================================================"
echo
echo "Branch: $BRANCH"
echo "Base:   $BASE"
echo
echo "Commits transportados:"
for COMMIT in "${COMMITS[@]}"; do
    echo "  - $COMMIT"
done
echo
echo "Backup:"
echo "  $BACKUP"
echo
echo "Log:"
echo "  $LOG"
echo
echo "Próximo passo:"
echo "  Abrir/revisar PR $BRANCH -> main"
echo
echo "IMPORTANTE:"
echo "O backup original continua preservado no stash."
