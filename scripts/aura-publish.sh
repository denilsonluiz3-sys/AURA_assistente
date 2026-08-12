#!/usr/bin/env bash
set -euo pipefail

REPO="/root/AURA_assistente"
REMOTE="origin"
MAIN="main"
APK_DIR="$REPO/aura-mobile-apk-release"
APK_DEST="/storage/emulated/0/objetivo"

cd "$REPO"

echo "=============================================="
echo " AURA - PUBLICAÇÃO E VALIDAÇÃO"
echo "=============================================="

# ------------------------------------------------
# 1. Ambiente
# ------------------------------------------------

echo
echo "[1/8] Verificando ambiente..."

command -v git >/dev/null || {
    echo "❌ git não encontrado."
    exit 1
}

command -v gh >/dev/null || {
    echo "❌ gh não encontrado."
    exit 1
}

if ! gh auth status >/dev/null 2>&1; then
    echo "❌ GitHub CLI não está autenticado."
    echo "Execute: gh auth login"
    exit 1
fi

echo "✅ Git"
echo "✅ GitHub CLI"

# ------------------------------------------------
# 2. Estado Git
# ------------------------------------------------

echo
echo "[2/8] Verificando Git..."

BRANCH="$(git branch --show-current)"
HEAD="$(git rev-parse --short HEAD)"

echo "Branch: $BRANCH"
echo "HEAD:   $HEAD"

if [ "$BRANCH" != "$MAIN" ]; then
    echo "⚠️ Branch atual não é main."
    echo "O script não fará merge automático."
    exit 1
fi

git fetch "$REMOTE" "$MAIN"

LOCAL="$(git rev-parse HEAD)"
REMOTE_HEAD="$(git rev-parse "$REMOTE/$MAIN")"

echo "Local:  ${LOCAL:0:12}"
echo "Remote: ${REMOTE_HEAD:0:12}"

# ------------------------------------------------
# 3. Arquivos
# ------------------------------------------------

echo
echo "[3/8] Arquivos modificados..."

STATUS="$(git status --short)"

if [ -z "$STATUS" ]; then
    echo "Nenhuma alteração local."
else
    echo "$STATUS"
fi

# ------------------------------------------------
# 4. Validação básica
# ------------------------------------------------

echo
echo "[4/8] Validando estrutura..."

if [ ! -f "AURA.sln" ] && [ ! -f "*.sln" ]; then
    echo "⚠️ Solution não encontrada no diretório esperado."
fi

if [ -f "scripts/salvar-contexto.sh" ]; then
    echo "Atualizando contexto..."
    ./scripts/salvar-contexto.sh
else
    echo "⚠️ salvar-contexto.sh não encontrado."
fi

# ------------------------------------------------
# 5. Commit
# ------------------------------------------------

echo
echo "[5/8] Commit..."

STATUS="$(git status --short)"

if [ -n "$STATUS" ]; then

    echo "$STATUS"

    git add -A

    echo
    echo "Arquivos preparados:"
    git status --short

    MSG="${1:-chore: synchronize AURA project state}"

    git commit -m "$MSG"

    echo "✅ Commit criado."

else
    echo "Nenhuma alteração para commit."
fi

# ------------------------------------------------
# 6. Push
# ------------------------------------------------

echo
echo "[6/8] Push para main..."

LOCAL="$(git rev-parse HEAD)"
REMOTE_HEAD="$(git rev-parse "$REMOTE/$MAIN")"

if [ "$LOCAL" != "$REMOTE_HEAD" ]; then

    # Segurança: nunca force push.
    if git push "$REMOTE" "$MAIN"; then
        echo "✅ Push concluído."
    else
        echo
        echo "❌ Push recusado."
        echo "O repositório pode exigir Pull Request."
        echo "Nenhuma alteração foi forçada."
        exit 1
    fi

else
    echo "✅ main já está sincronizada."
fi

# ------------------------------------------------
# 7. CI
# ------------------------------------------------

echo
echo "[7/8] Verificando GitHub Actions..."

sleep 3

RUN_ID="$(
    gh run list \
        --branch "$MAIN" \
        --limit 1 \
        --json databaseId \
        --jq '.[0].databaseId' 2>/dev/null || true
)"

if [ -n "$RUN_ID" ]; then

    echo "Última execução: $RUN_ID"

    gh run watch "$RUN_ID" --exit-status || {
        echo
        echo "❌ GitHub Actions terminou com falha."
        echo
        gh run view "$RUN_ID" --log-failed || true
        exit 1
    }

    echo "✅ GitHub Actions OK."

else
    echo "⚠️ Nenhuma execução encontrada."
fi

# ------------------------------------------------
# 8. APK
# ------------------------------------------------

echo
echo "[8/8] Procurando APK..."

mkdir -p "$APK_DIR"

APK="$(
    find "$APK_DIR" \
        -type f \
        -name "*-Signed.apk" \
        -print \
        | head -n 1
)"

if [ -z "$APK" ]; then
    echo "⚠️ APK Release não encontrado localmente."
    echo
    echo "O código foi publicado e o CI foi validado."
    echo "Nenhum APK será inventado ou criado artificialmente."
    exit 0
fi

echo "APK:"
echo "$APK"

if [ -d "/storage/emulated/0" ]; then

    mkdir -p "$APK_DEST"

    cp "$APK" "$APK_DEST/"

    echo
    echo "✅ APK copiado para:"
    echo "$APK_DEST/$(basename "$APK")"

else
    echo
    echo "⚠️ Armazenamento Android não disponível."
    echo "APK permanece em:"
    echo "$APK"
fi

echo
echo "=============================================="
echo " AURA PUBLICADA COM SUCESSO"
echo "=============================================="
echo
echo "Commit:"
git log -1 --oneline

echo
echo "Branch:"
git branch --show-current

echo
echo "APK:"
if [ -n "$APK" ]; then
    echo "$APK"
fi

