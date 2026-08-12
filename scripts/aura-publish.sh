#!/usr/bin/env bash

set -u

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"

if [ -z "$REPO_ROOT" ]; then
    echo "❌ Este diretório não é um repositório Git."
    exit 1
fi

cd "$REPO_ROOT" || exit 1

REMOTE="origin"
BRANCH="$(git branch --show-current)"

if [ -z "$BRANCH" ]; then
    echo "❌ Não foi possível identificar a branch atual."
    exit 1
fi

MAX_SIZE=$((100 * 1024 * 1024))

pause() {
    echo
    read -r -p "Pressione ENTER para continuar..." _
}

header() {
    clear 2>/dev/null || true
    echo "=============================================="
    echo "        AURA GitHub Manager"
    echo "=============================================="
    echo "Repositório: $(basename "$REPO_ROOT")"
    echo "Branch:      $BRANCH"
    echo "=============================================="
    echo
}

show_status() {
    header

    echo "=== STATUS ==="
    git status

    echo
    echo "=== REMOTO ==="
    git remote -v

    echo
    echo "=== ÚLTIMOS COMMITS ==="
    git log --oneline --decorate -8

    pause
}

show_commits() {
    header

    echo "=== ÚLTIMOS 20 COMMITS ==="
    git log --oneline --decorate --graph -20

    pause
}

check_large_files() {
    header

    echo "=== VERIFICANDO ARQUIVOS GRANDES ==="
    echo

    found=0

    while IFS= read -r file; do
        [ -f "$file" ] || continue

        size=$(wc -c < "$file")

        if [ "$size" -gt "$MAX_SIZE" ]; then
            mb=$((size / 1024 / 1024))
            echo "⚠️  ${mb} MB  $file"
            found=1
        fi
    done < <(git ls-files)

    if [ "$found" -eq 0 ]; then
        echo "✅ Nenhum arquivo rastreado ultrapassa 100 MB."
    else
        echo
        echo "❌ Existem arquivos acima do limite do GitHub."
        echo "Eles precisam ser removidos do Git antes do push."
    fi

    pause
}

ignore_generated_apks() {
    echo "=== PROTEÇÃO DE APKs GERADOS ==="

    if ! grep -qxF '/aura-mobile-apk-debug/*.apk' .gitignore 2>/dev/null; then
        printf '\n# APKs gerados localmente\n/aura-mobile-apk-debug/*.apk\n' >> .gitignore
        echo "✅ Regra adicionada ao .gitignore."
    else
        echo "✅ APKs já estão no .gitignore."
    fi
}

remove_tracked_apks() {
    echo "=== REMOVENDO APKs DO GIT ==="

    found=0

    while IFS= read -r file; do
        [ -n "$file" ] || continue

        if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
            git rm --cached -- "$file"
            found=1
        fi
    done < <(find aura-mobile-apk-debug -type f -name '*.apk' 2>/dev/null)

    if [ "$found" -eq 0 ]; then
        echo "✅ Nenhum APK rastreado pelo Git."
    else
        echo "✅ APKs removidos do índice."
        echo "   Os arquivos locais não foram apagados."
    fi
}

prepare_generated_files() {
    echo "=== PREPARANDO ARQUIVOS GERADOS ==="

    ignore_generated_apks
    remove_tracked_apks

    echo
}

update_remote() {
    echo "=== ATUALIZANDO REFERÊNCIAS DO GITHUB ==="

    if ! git fetch "$REMOTE" --prune; then
        echo "❌ Falha ao atualizar informações do remoto."
        return 1
    fi

    echo "✅ Informações do GitHub atualizadas."
}

check_sync() {
    echo
    echo "=== SINCRONIZAÇÃO ==="

    git fetch "$REMOTE" --prune >/dev/null 2>&1 || true

    LOCAL=$(git rev-parse HEAD)
    REMOTE_HEAD=$(git rev-parse "$REMOTE/$BRANCH" 2>/dev/null || true)

    if [ -z "$REMOTE_HEAD" ]; then
        echo "ℹ️ Branch remota ainda não encontrada."
        return 0
    fi

    if [ "$LOCAL" = "$REMOTE_HEAD" ]; then
        echo "✅ Local e GitHub estão sincronizados."
    else
        AHEAD=$(git rev-list --count "$REMOTE/$BRANCH..HEAD")
        BEHIND=$(git rev-list --count "HEAD..$REMOTE/$BRANCH")

        echo "Local à frente: $AHEAD commit(s)"
        echo "Local atrás:    $BEHIND commit(s)"
    fi
}

commit_and_push() {
    header

    echo "=== PREPARAÇÃO ==="
    prepare_generated_files

    echo
    echo "=== ARQUIVOS MODIFICADOS ==="
    git status --short

    echo
    if git diff --quiet && git diff --cached --quiet; then
        echo "ℹ️ Nenhuma alteração para commit."
        pause
        return 0
    fi

    echo "=== VERIFICAÇÃO DE ARQUIVOS GRANDES ==="

    large=0

    while IFS= read -r file; do
        [ -f "$file" ] || continue

        size=$(wc -c < "$file")

        if [ "$size" -gt "$MAX_SIZE" ]; then
            mb=$((size / 1024 / 1024))
            echo "❌ ${mb} MB  $file"
            large=1
        fi
    done < <(git ls-files)

    if [ "$large" -eq 1 ]; then
        echo
        echo "❌ Push cancelado."
        echo "Existem arquivos acima de 100 MB."
        echo "Nenhum force push será executado."
        pause
        return 1
    fi

    echo
    read -r -p "Mensagem do commit: " MESSAGE

    if [ -z "$MESSAGE" ]; then
        MESSAGE="chore: synchronize AURA project state"
    fi

    git add -A

    if git diff --cached --quiet; then
        echo "ℹ️ Nenhuma alteração para commit."
        pause
        return 0
    fi

    echo
    echo "=== CRIANDO COMMIT ==="

    if ! git commit -m "$MESSAGE"; then
        echo "❌ Commit falhou."
        pause
        return 1
    fi

    echo
    echo "=== PUSH ==="

    if git push "$REMOTE" "$BRANCH"; then
        echo
        echo "=============================================="
        echo "✅ PUSH CONCLUÍDO"
        echo "=============================================="
        git status --short
        echo
        git log -1 --oneline
    else
        echo
        echo "=============================================="
        echo "❌ PUSH RECUSADO"
        echo "=============================================="
        echo
        echo "Nenhum force push será executado."
        echo "O commit local foi preservado."
        echo
        echo "Diagnóstico:"
        git status
        return 1
    fi

    pause
}

full_sync() {
    header

    echo "=== SINCRONIZAÇÃO COMPLETA DA AURA ==="
    echo

    if ! update_remote; then
        pause
        return 1
    fi

    echo
    check_sync

    echo
    echo "=== STATUS ATUAL ==="
    git status --short

    echo
    echo "=== ARQUIVOS GERADOS ==="
    prepare_generated_files

    echo
    echo "=== STATUS FINAL DA PREPARAÇÃO ==="
    git status --short

    echo
    echo "A sincronização está pronta."
    echo "Use 'Commit e Push' para publicar alterações."

    pause
}

menu() {
    while true; do
        header

        echo "1) Status"
        echo "2) Atualizar do GitHub"
        echo "3) Commit e Push"
        echo "4) Ver commits"
        echo "5) Ver arquivos grandes"
        echo "6) Preparar APKs / .gitignore"
        echo "7) Sincronização completa"
        echo "0) Sair"
        echo

        read -r -p "Escolha: " OPTION

        case "$OPTION" in
            1)
                show_status
                ;;
            2)
                update_remote
                pause
                ;;
            3)
                commit_and_push
                ;;
            4)
                show_commits
                ;;
            5)
                check_large_files
                ;;
            6)
                header
                prepare_generated_files
                git status --short
                pause
                ;;
            7)
                full_sync
                ;;
            0)
                echo "Saindo."
                exit 0
                ;;
            *)
                echo "❌ Opção inválida."
                sleep 1
                ;;
        esac
    done
}

menu
