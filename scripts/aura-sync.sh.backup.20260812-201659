#!/usr/bin/env bash

set -u

REMOTE="origin"
MAIN="main"
MAX_SIZE=$((100 * 1024 * 1024))

# Ordem por prioridade/dependência
PRIORITY=(
    "feat/project-access"
    "arch/tool-registry"
    "fix/android-apk-build"
    "fix/module-download-embedded-fallback"
    "feat/voice-assistant-fab"
    "fix/run-wait-go-smoke"
    "feature/ui-holographic-dashboard"
    "feature/ui-holo-f2"
    "feat/rename-angela-to-aura"
)

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"

if [ -z "$REPO_ROOT" ]; then
    echo "❌ Não estamos dentro de um repositório Git."
    exit 1
fi

cd "$REPO_ROOT" || exit 1

pause() {
    echo
    read -r -p "Pressione ENTER para continuar..." _
}

header() {
    clear 2>/dev/null || true
    echo "=============================================="
    echo "          AURA SYNC MANAGER"
    echo "=============================================="
    echo "Repo:   $(basename "$REPO_ROOT")"
    echo "Branch: $(git branch --show-current)"
    echo "=============================================="
    echo
}

fetch_repo() {
    echo "=== ATUALIZANDO GITHUB ==="

    if ! git fetch "$REMOTE" --prune; then
        echo "❌ Falha no fetch."
        return 1
    fi

    echo "✅ GitHub atualizado."
}

large_files() {
    echo
    echo "=== VERIFICANDO ARQUIVOS GRANDES ==="

    local found=0
    local file
    local size

    while IFS= read -r -d '' file; do
        [ -f "$file" ] || continue

        size=$(wc -c < "$file")

        if [ "$size" -gt "$MAX_SIZE" ]; then
            echo "❌ $((size / 1024 / 1024)) MB  $file"
            found=1
        fi
    done < <(git ls-files -z)

    if [ "$found" -eq 0 ]; then
        echo "✅ Nenhum arquivo rastreado ultrapassa 100 MB."
        return 0
    fi

    return 1
}

clean_generated() {
    echo
    echo "=== PROTEÇÃO DE ARQUIVOS GERADOS ==="

    touch .gitignore

    if ! grep -qxF '/aura-mobile-apk-debug/*.apk' .gitignore; then
        printf '\n# APKs gerados localmente\n/aura-mobile-apk-debug/*.apk\n' >> .gitignore
        echo "✅ APKs adicionados ao .gitignore."
    fi

    # Remove apenas APKs do índice, preservando os arquivos locais.
    git ls-files -z 'aura-mobile-apk-debug/*.apk' |
    while IFS= read -r -d '' file; do
        git rm --cached -- "$file" >/dev/null 2>&1 || true
        echo "✅ Removido do Git: $file"
    done
}

status_clean() {
    if ! git diff --quiet || ! git diff --cached --quiet; then
        return 1
    fi

    if [ -n "$(git status --short)" ]; then
        return 1
    fi

    return 0
}

pending_count() {
    local branch="$1"

    git rev-list --count "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null || echo 0
}

show_plan() {
    header

    echo "===== MAIN ====="
    git log "$REMOTE/$MAIN" -1 --oneline
    echo

    echo "===== PENDÊNCIAS POR PRIORIDADE ====="

    local branch
    local count

    for branch in "${PRIORITY[@]}"; do
        if git show-ref --verify --quiet "refs/remotes/$REMOTE/$branch"; then
            count=$(pending_count "$branch")

            if [ "$count" -gt 0 ]; then
                echo
                echo "🔴 $branch — $count commit(s)"

                git log \
                    --oneline \
                    --no-merges \
                    "$REMOTE/$MAIN..$REMOTE/$branch"
            else
                echo "🟢 $branch — já sincronizada"
            fi
        else
            echo "⚪ $branch — não encontrada"
        fi
    done

    echo
    echo "===== ARQUIVOS GRANDES ====="
    large_files || true
}

integrate_branch() {
    local branch="$1"

    echo
    echo "=============================================="
    echo " INTEGRANDO: $branch"
    echo "=============================================="

    local count
    count=$(pending_count "$branch")

    if [ "$count" -eq 0 ]; then
        echo "🟢 Nenhum commit pendente."
        return 0
    fi

    echo "Commits pendentes: $count"
    echo

    git log \
        --oneline \
        --no-merges \
        "$REMOTE/$MAIN..$REMOTE/$branch"

    echo
    read -r -p "Integrar esta branch? [S/n]: " answer

    case "$answer" in
        n|N)
            echo "⏭️ Pulando $branch."
            return 0
            ;;
    esac

    # Garantir main limpa
    if ! status_clean; then
        echo "❌ Existem alterações locais."
        echo "Não vou misturar trabalho existente com a integração."
        return 1
    fi

    # Atualiza main
    git checkout "$MAIN" || return 1
    git pull --ff-only "$REMOTE" "$MAIN" || {
        echo "❌ Não foi possível atualizar main."
        return 1
    }

    echo
    echo "=== TENTANDO MERGE ==="

    if git merge --no-ff --no-edit "$REMOTE/$branch"; then
        echo "✅ Merge concluído."
    else
        echo
        echo "❌ CONFLITO DETECTADO."
        echo
        echo "A integração foi interrompida."
        echo "Resolva o conflito manualmente."
        echo
        echo "Para cancelar:"
        echo "  git merge --abort"
        return 1
    fi

    echo
    echo "=== VERIFICAÇÃO ==="

    if ! large_files; then
        echo "❌ Merge criaria arquivo acima de 100 MB."
        echo "Revertendo o merge."

        git merge --abort 2>/dev/null || \
        git reset --hard HEAD~1

        return 1
    fi

    echo
    echo "=== STATUS ==="
    git status --short

    echo
    echo "✅ Branch integrada: $branch"

    return 0
}

run_all() {
    header

    echo "🚀 INICIANDO SINCRONIZAÇÃO DA AURA"
    echo

    if ! fetch_repo; then
        exit 1
    fi

    clean_generated

    if ! status_clean; then
        echo
        echo "⚠️ Alterações locais detectadas:"
        git status --short
        echo
        echo "Não vou apagá-las."
        echo "Faça commit ou stash antes de continuar."
        exit 1
    fi

    if ! large_files; then
        echo
        echo "❌ Existem arquivos acima de 100 MB."
        exit 1
    fi

    echo
    show_plan

    echo
    echo "=============================================="
    echo " COMEÇAR INTEGRAÇÃO?"
    echo "=============================================="
    read -r -p "Digite SIM para continuar: " confirm

    if [ "$confirm" != "SIM" ]; then
        echo "Operação cancelada."
        exit 0
    fi

    local branch
    local failed=0

    for branch in "${PRIORITY[@]}"; do
        if git show-ref --verify --quiet "refs/remotes/$REMOTE/$branch"; then

            if [ "$(pending_count "$branch")" -gt 0 ]; then

                if ! integrate_branch "$branch"; then
                    echo
                    echo "🛑 PARADO NA BRANCH:"
                    echo "   $branch"
                    echo
                    echo "Nenhuma outra branch será integrada."
                    failed=1
                    break
                fi

                echo
                read -r -p "Continuar para a próxima branch? [S/n]: " next

                case "$next" in
                    n|N)
                        echo "⏸️ Sincronização pausada."
                        break
                        ;;
                esac
            fi
        fi
    done

    echo
    echo "=============================================="
    echo " RESULTADO"
    echo "=============================================="

    if [ "$failed" -eq 1 ]; then
        echo "❌ Processo interrompido."
        exit 1
    fi

    echo "✅ Integração concluída até onde foi possível."
    echo
    git status
    echo
    echo "Para publicar:"
    echo "  git push origin main"
}

menu() {
    while true; do
        header

        echo "1) Mostrar plano"
        echo "2) Executar sincronização automática"
        echo "3) Ver status"
        echo "4) Ver branches pendentes"
        echo "5) Ver arquivos grandes"
        echo "0) Sair"
        echo

        read -r -p "Escolha: " option

        case "$option" in
            1)
                fetch_repo || true
                show_plan
                pause
                ;;
            2)
                run_all
                pause
                ;;
            3)
                git status
                pause
                ;;
            4)
                fetch_repo || true
                show_plan
                pause
                ;;
            5)
                large_files || true
                pause
                ;;
            0)
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
