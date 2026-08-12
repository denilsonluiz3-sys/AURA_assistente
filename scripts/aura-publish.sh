#!/usr/bin/env bash

set -u

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"

if [ -z "$REPO_ROOT" ]; then
    echo "❌ Não é um repositório Git."
    exit 1
fi

cd "$REPO_ROOT" || exit 1

REMOTE="origin"
MAX_SIZE=$((100 * 1024 * 1024))

header() {
    clear 2>/dev/null || true
    echo "=============================================="
    echo "          AURA GitHub Manager"
    echo "=============================================="
    echo "Repositório: $(basename "$REPO_ROOT")"
    echo "Branch:      $(git branch --show-current)"
    echo "=============================================="
    echo
}

pause() {
    echo
    read -r -p "Pressione ENTER para continuar..." _
}

status_repo() {
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

commits() {
    header

    echo "=== ÚLTIMOS 20 COMMITS ==="
    git log --oneline --decorate --graph -20

    pause
}

update_remote() {
    echo "=== ATUALIZANDO GITHUB ==="

    if git fetch "$REMOTE" --prune; then
        echo "✅ GitHub atualizado."
    else
        echo "❌ Falha no git fetch."
        return 1
    fi
}

sync_status() {
    local branch
    local local_head
    local remote_head
    local ahead
    local behind

    branch="$(git branch --show-current)"

    git fetch "$REMOTE" --prune >/dev/null 2>&1 || true

    local_head="$(git rev-parse HEAD)"
    remote_head="$(git rev-parse "$REMOTE/$branch" 2>/dev/null || true)"

    echo
    echo "=== SINCRONIZAÇÃO ==="

    if [ -z "$remote_head" ]; then
        echo "ℹ️ Branch remota não encontrada."
        return 0
    fi

    if [ "$local_head" = "$remote_head" ]; then
        echo "✅ Local e GitHub estão sincronizados."
        return 0
    fi

    ahead="$(git rev-list --count "$REMOTE/$branch..HEAD")"
    behind="$(git rev-list --count "HEAD..$REMOTE/$branch")"

    echo "Local à frente: $ahead commit(s)"
    echo "Local atrás:    $behind commit(s)"
}

check_large_files() {
    header

    echo "=== ARQUIVOS MAIORES QUE 100 MB ==="
    echo

    local found=0
    local file
    local size
    local mb

    while IFS= read -r -d '' file; do
        size="$(wc -c < "$file")"

        if [ "$size" -gt "$MAX_SIZE" ]; then
            mb=$((size / 1024 / 1024))
            echo "❌ ${mb} MB  $file"
            found=1
        fi
    done < <(git ls-files -z)

    if [ "$found" -eq 0 ]; then
        echo "✅ Nenhum arquivo rastreado ultrapassa 100 MB."
    else
        echo
        echo "⚠️ Arquivos acima do limite do GitHub encontrados."
    fi

    pause
}

prepare_apks() {
    echo "=== PROTEÇÃO DOS APKs GERADOS ==="

    if ! grep -qxF '/aura-mobile-apk-debug/*.apk' .gitignore 2>/dev/null; then
        printf '\n# APKs gerados localmente\n/aura-mobile-apk-debug/*.apk\n' >> .gitignore
        echo "✅ APKs adicionados ao .gitignore."
    else
        echo "✅ APKs já estão protegidos pelo .gitignore."
    fi

    local found=0
    local file

    while IFS= read -r -d '' file; do
        if git ls-files --error-unmatch "$file" >/dev/null 2>&1; then
            git rm --cached -- "$file"
            found=1
        fi
    done < <(find aura-mobile-apk-debug -type f -name '*.apk' -print0 2>/dev/null)

    if [ "$found" -eq 1 ]; then
        echo "✅ APKs removidos do índice Git."
    else
        echo "✅ Nenhum APK rastreado."
    fi
}

select_branch() {
    header

    echo "=== BRANCHES DISPONÍVEIS ==="
    echo

    mapfile -t branches < <(
        git for-each-ref \
            --format='%(refname:short)' \
            refs/heads refs/remotes/origin |
        sed 's#^origin/##' |
        sort -u
    )

    local current
    current="$(git branch --show-current)"

    local i=1
    local branch

    for branch in "${branches[@]}"; do
        if [ "$branch" = "$current" ]; then
            echo "$i) $branch [ATUAL]"
        else
            echo "$i) $branch"
        fi
        ((i++))
    done

    echo "$i) Digitar branch manualmente"
    echo

    local choice
    read -r -p "Escolha: " choice

    local selected

    if [ "$choice" -eq "$i" ] 2>/dev/null; then
        read -r -p "Nome da branch: " selected
    elif [ "$choice" -ge 1 ] 2>/dev/null &&
         [ "$choice" -le "${#branches[@]}" ]; then
        selected="${branches[$((choice - 1))]}"
    else
        echo "❌ Opção inválida."
        pause
        return
    fi

    if git show-ref --verify --quiet "refs/heads/$selected"; then
        git checkout "$selected"
    elif git ls-remote --exit-code "$REMOTE" "refs/heads/$selected" >/dev/null 2>&1; then
        git checkout -b "$selected" "$REMOTE/$selected"
    else
        echo "❌ Branch não encontrada no GitHub."
        pause
        return
    fi

    echo
    echo "✅ Branch atual: $selected"

    pause
}

commit_push() {
    header

    local branch
    branch="$(git branch --show-current)"

    echo "=== PREPARANDO REPOSITÓRIO ==="
    echo

    prepare_apks

    echo
    echo "=== STATUS ==="
    git status --short

    echo
    echo "=== VERIFICANDO ARQUIVOS GRANDES ==="

    local large=0
    local file
    local size
    local mb

    while IFS= read -r -d '' file; do
        size="$(wc -c < "$file")"

        if [ "$size" -gt "$MAX_SIZE" ]; then
            mb=$((size / 1024 / 1024))
            echo "❌ ${mb} MB  $file"
            large=1
        fi
    done < <(git ls-files -z)

    if [ "$large" -eq 1 ]; then
        echo
        echo "❌ Commit/PUSH cancelado."
        echo "Existem arquivos rastreados acima de 100 MB."
        pause
        return 1
    fi

    git add -A

    if git diff --cached --quiet; then
        echo
        echo "ℹ️ Nenhuma alteração para commit."
        pause
        return 0
    fi

    echo
    echo "=== ALTERAÇÕES QUE SERÃO COMMITADAS ==="
    git diff --cached --stat

    echo
    read -r -p "Mensagem do commit: " message

    if [ -z "$message" ]; then
        message="chore: synchronize AURA project state"
    fi

    echo
    echo "=== CRIANDO COMMIT ==="

    if ! git commit -m "$message"; then
        echo "❌ Commit falhou."
        pause
        return 1
    fi

    echo
    echo "=== PUSH ==="
    echo "Branch: $branch"

    if git push "$REMOTE" "$branch"; then
        echo
        echo "=============================================="
        echo "✅ PUSH CONCLUÍDO"
        echo "=============================================="
        git log -1 --oneline
    else
        echo
        echo "=============================================="
        echo "❌ PUSH RECUSADO"
        echo "=============================================="
        echo
        echo "Nenhum push foi forçado."
        echo "O commit local continua preservado."
        pause
        return 1
    fi

    pause
}

full_sync() {
    header

    echo "=== SINCRONIZAÇÃO COMPLETA ==="
    echo

    update_remote || {
        pause
        return 1
    }

    sync_status

    echo
    echo "=== PREPARANDO APKs ==="

    prepare_apks

    echo
    echo "=== STATUS FINAL ==="
    git status --short

    pause
}

menu() {
    while true; do
        header

        echo "1) Selecionar branch"
        echo "2) Status"
        echo "3) Atualizar do GitHub"
        echo "4) Commit e Push"
        echo "5) Ver commits"
        echo "6) Ver arquivos grandes"
        echo "7) Proteger/remover APKs do Git"
        echo "8) Sincronização completa"
        echo "0) Sair"
        echo

        local option
        read -r -p "Escolha: " option

        case "$option" in
            1)
                select_branch
                ;;
            2)
                status_repo
                ;;
            3)
                header
                update_remote
                pause
                ;;
            4)
                commit_push
                ;;
            5)
                commits
                ;;
            6)
                check_large_files
                ;;
            7)
                header
                prepare_apks
                git status --short
                pause
                ;;
            8)
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
