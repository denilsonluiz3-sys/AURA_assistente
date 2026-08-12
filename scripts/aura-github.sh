#!/usr/bin/env bash

set -u

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"

if [ -z "$REPO_ROOT" ]; then
    echo "❌ Não estamos dentro de um repositório Git."
    exit 1
fi

cd "$REPO_ROOT" || exit 1

REMOTE="origin"
MAIN="main"
MAX_SIZE=$((100 * 1024 * 1024))

# Ordem por prioridade de implementação.
PRIORITY_BRANCHES=(
    "fix/android-apk-build"
    "feat/project-access"
    "arch/tool-registry"
    "fix/module-download-embedded-fallback"
    "feat/voice-assistant-fab"
    "feature/ui-holographic-dashboard"
    "feature/ui-holo-f2"
    "fix/run-wait-go-smoke"
    "feat/rename-angela-to-aura"
    "backup/e44d378-before-update"
)

pause() {
    echo
    read -r -p "ENTER para continuar..." _
}

header() {
    clear 2>/dev/null || true

    echo "=============================================="
    echo "           AURA GITHUB MANAGER"
    echo "=============================================="
    echo "Projeto : $(basename "$REPO_ROOT")"
    echo "Branch  : $(git branch --show-current)"
    echo "Remote  : $REMOTE"
    echo "=============================================="
    echo
}

update_github() {
    echo "=== ATUALIZANDO GITHUB ==="

    if git fetch "$REMOTE" --prune; then
        echo "✅ GitHub atualizado."
    else
        echo "❌ Falha no fetch."
        return 1
    fi
}

status_project() {
    header

    echo "=== STATUS DO PROJETO ==="
    git status --short

    echo
    echo "=== BRANCH ATUAL ==="
    git branch --show-current

    echo
    echo "=== ÚLTIMO COMMIT ==="
    git log -1 --oneline --decorate

    echo
    echo "=== SINCRONIZAÇÃO ==="

    branch="$(git branch --show-current)"

    if git rev-parse "$REMOTE/$branch" >/dev/null 2>&1; then
        ahead="$(git rev-list --count "$REMOTE/$branch..HEAD")"
        behind="$(git rev-list --count "HEAD..$REMOTE/$branch")"

        echo "À frente : $ahead commit(s)"
        echo "Atrás    : $behind commit(s)"

        if [ "$ahead" -eq 0 ] && [ "$behind" -eq 0 ]; then
            echo "✅ Local e GitHub sincronizados."
        fi
    else
        echo "ℹ️ Branch remota não encontrada."
    fi

    pause
}

show_branches() {
    header

    echo "=== PENDÊNCIAS POR PRIORIDADE ==="
    echo

    if ! git fetch "$REMOTE" --prune >/dev/null 2>&1; then
        echo "⚠️ Não foi possível atualizar o remoto."
    fi

    n=1

    for branch in "${PRIORITY_BRANCHES[@]}"; do

        if ! git rev-parse --verify "$REMOTE/$branch" >/dev/null 2>&1; then
            continue
        fi

        commits="$(git rev-list --count "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null || echo 0)"

        echo "$n) $branch"
        echo "   Commits pendentes: $commits"

        if [ "$commits" -gt 0 ]; then
            git log --oneline --no-merges \
                "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null |
                sed 's/^/   /'
        else
            echo "   ✅ Nenhum commit exclusivo."
        fi

        echo
        n=$((n + 1))
    done

    echo "=== OUTRAS BRANCHES REMOTAS ==="
    echo

    git for-each-ref \
        --format='%(refname:short)' \
        "refs/remotes/$REMOTE" |
        sed "s#^$REMOTE/##" |
        grep -v '^HEAD$' |
        grep -v '^main$' |
        while read -r branch; do

            found=0

            for priority in "${PRIORITY_BRANCHES[@]}"; do
                [ "$branch" = "$priority" ] && found=1
            done

            if [ "$found" -eq 0 ]; then
                commits="$(git rev-list --count "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null || echo 0)"
                echo "• $branch — $commits commit(s) exclusivos"
            fi
        done

    pause
}

check_large_files() {
    echo "=== ARQUIVOS GRANDES ==="

    found=0

    while IFS= read -r -d '' file; do

        [ -f "$file" ] || continue

        size="$(wc -c < "$file")"

        if [ "$size" -gt "$MAX_SIZE" ]; then
            mb=$((size / 1024 / 1024))
            echo "❌ ${mb} MB — $file"
            found=1
        fi

    done < <(git ls-files -z)

    if [ "$found" -eq 0 ]; then
        echo "✅ Nenhum arquivo rastreado ultrapassa 100 MB."
    else
        echo
        echo "⚠️ O GitHub rejeitará esses arquivos."
    fi

    return "$found"
}

protect_apks() {
    echo "=== PROTEÇÃO DOS APKs GERADOS ==="

    touch .gitignore

    if ! grep -qxF '/aura-mobile-apk-debug/*.apk' .gitignore; then
        printf '\n# APKs gerados localmente\n/aura-mobile-apk-debug/*.apk\n' >> .gitignore
        echo "✅ APKs adicionados ao .gitignore."
    else
        echo "✅ APKs já protegidos."
    fi

    tracked_apks="$(git ls-files 'aura-mobile-apk-debug/*.apk' 2>/dev/null || true)"

    if [ -n "$tracked_apks" ]; then
        echo
        echo "Removendo APKs do índice Git:"

        while IFS= read -r file; do
            [ -n "$file" ] || continue
            git rm --cached -- "$file"
        done <<< "$tracked_apks"

        echo "✅ APKs deixaram de ser rastreados."
        echo "   Os arquivos locais NÃO foram apagados."
    else
        echo "✅ Nenhum APK rastreado."
    fi
}

prepare_project() {
    header

    echo "=== PREPARAÇÃO DO PROJETO ==="
    echo

    protect_apks

    echo
    check_large_files || true

    echo
    echo "=== STATUS APÓS PREPARAÇÃO ==="
    git status --short

    pause
}

commit_push() {
    header

    echo "=== COMMIT + PUSH ==="
    echo

    branch="$(git branch --show-current)"

    if [ -z "$branch" ]; then
        echo "❌ Não foi possível determinar a branch."
        pause
        return 1
    fi

    echo "Branch: $branch"
    echo

    protect_apks

    echo
    echo "=== ALTERAÇÕES ==="
    git status --short

    if git diff --quiet && git diff --cached --quiet; then
        echo
        echo "ℹ️ Nenhuma alteração para commit."
        pause
        return 0
    fi

    echo
    echo "=== VERIFICAÇÃO DE ARQUIVOS GRANDES ==="

    if ! check_large_files; then
        echo
        echo "❌ Commit/PUSH interrompido."
        echo "Remova os arquivos grandes ou coloque-os fora do Git."
        pause
        return 1
    fi

    echo
    read -r -p "Mensagem do commit [chore: synchronize AURA project state]: " message

    if [ -z "$message" ]; then
        message="chore: synchronize AURA project state"
    fi

    echo
    echo "=== ADD ==="
    git add -A || {
        echo "❌ git add falhou."
        pause
        return 1
    }

    if git diff --cached --quiet; then
        echo "ℹ️ Nenhuma alteração depois do add."
        pause
        return 0
    fi

    echo
    echo "=== COMMIT ==="

    if ! git commit -m "$message"; then
        echo "❌ Commit falhou."
        pause
        return 1
    fi

    echo
    echo "=== PUSH ==="

    if git push "$REMOTE" "$branch"; then

        echo
        echo "=============================================="
        echo "✅ PUBLICADO COM SUCESSO"
        echo "=============================================="
        echo
        git log -1 --oneline --decorate

    else

        echo
        echo "=============================================="
        echo "❌ PUSH RECUSADO"
        echo "=============================================="
        echo
        echo "O commit LOCAL continua preservado."
        echo "Nenhum push forçado foi executado."

        pause
        return 1
    fi

    pause
}

show_commits() {
    header

    echo "=== ÚLTIMOS COMMITS ==="
    echo

    git -c core.pager=cat log \
        --oneline \
        --decorate \
        --graph \
        -25

    pause
}

sync_main() {
    header

    branch="$(git branch --show-current)"

    if [ "$branch" != "$MAIN" ]; then
        echo "⚠️ Você não está na main."
        echo "Branch atual: $branch"
        pause
        return 1
    fi

    echo "=== SINCRONIZAÇÃO DA MAIN ==="
    echo

    update_github || {
        pause
        return 1
    }

    echo
    git status --short

    echo
    echo "=== MAIN REMOTA ==="
    git log "$REMOTE/$MAIN" -1 --oneline --decorate

    echo
    echo "=== MAIN LOCAL ==="
    git log "$MAIN" -1 --oneline --decorate

    echo
    echo "Nenhum merge automático foi executado."

    pause
}

full_diagnosis() {
    header

    echo "=== DIAGNÓSTICO COMPLETO ==="
    echo

    update_github || true

    echo
    echo "===== STATUS ====="
    git status --short

    echo
    echo "===== BRANCH ====="
    git branch --show-current

    echo
    echo "===== MAIN ====="
    git log "$REMOTE/$MAIN" -1 --oneline --decorate

    echo
    echo "===== PENDÊNCIAS ====="

    for branch in "${PRIORITY_BRANCHES[@]}"; do

        if git rev-parse --verify "$REMOTE/$branch" >/dev/null 2>&1; then

            commits="$(git rev-list --count "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null || echo 0)"

            if [ "$commits" -gt 0 ]; then
                echo "🔴 $branch — $commits commit(s)"
            else
                echo "⚪ $branch — sem commits exclusivos"
            fi
        fi
    done

    echo
    echo "===== ARQUIVOS GRANDES ====="
    check_large_files || true

    pause
}

menu() {
    while true; do

        header

        echo "1) 🔎 Diagnóstico completo"
        echo "2) 📋 Ver pendências das branches"
        echo "3) 🔄 Atualizar GitHub"
        echo "4) 📊 Status do projeto"
        echo "5) 🧹 Preparar projeto"
        echo "6) 💾 Commit + Push"
        echo "7) 📜 Ver commits"
        echo "8) 🔄 Sincronizar main"
        echo "0) 🚪 Sair"
        echo

        read -r -p "Escolha: " option

        case "$option" in
            1) full_diagnosis ;;
            2) show_branches ;;
            3)
                update_github
                pause
                ;;
            4) status_project ;;
            5) prepare_project ;;
            6) commit_push ;;
            7) show_commits ;;
            8) sync_main ;;
            0)
                echo "AURA GitHub Manager encerrado."
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
