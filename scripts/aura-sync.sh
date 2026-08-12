#!/usr/bin/env bash
export LC_ALL=C.UTF-8
export LANG=C.UTF-8
set -u
REMOTE="origin"
MAIN="main"
MAX_SIZE=$((100 * 1024 * 1024))
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
[ -z "$REPO_ROOT" ] && echo "❌ Não estamos dentro de um repositório Git." && exit 1
cd "$REPO_ROOT" || exit 1
pause(){ echo; read -r -p "Pressione ENTER para continuar..." _; }
header(){ clear 2>/dev/null || true; echo "=============================================="; echo "          AURA SYNC MANAGER"; echo "=============================================="; echo "Repo:   $(basename "$REPO_ROOT")"; echo "Branch: $(git branch --show-current)"; echo "=============================================="; echo; }
fetch_repo(){ echo "=== ATUALIZANDO GITHUB ==="; git fetch "$REMOTE" --prune || { echo "❌ Falha no fetch."; return 1; }; echo "✅ GitHub atualizado."; }
large_files(){ echo; echo "=== VERIFICANDO ARQUIVOS GRANDES ==="; local found=0; git ls-files -z | while IFS= read -r -d '' file; do [ -f "$file" ] || continue; size=$(wc -c < "$file"); [ "$size" -gt "$MAX_SIZE" ] && echo "❌ $((size / 1024 / 1024)) MB  $file" && found=1; done; [ "$found" -eq 0 ] && echo "✅ Nenhum arquivo rastreado ultrapassa 100 MB." && return 0; return 1; }
clean_generated(){ echo; echo "=== PROTEÇÃO DE ARQUIVOS GERADOS ==="; touch .gitignore; grep -qxF '/aura-mobile-apk-debug/*.apk' .gitignore || { printf '\n# APKs gerados localmente\n/aura-mobile-apk-debug/*.apk\n' >> .gitignore; echo "✅ APKs no .gitignore."; }; git ls-files -z 'aura-mobile-apk-debug/*.apk' | while IFS= read -r -d '' file; do git rm --cached -- "$file" >/dev/null 2>&1 || true; echo "✅ Removido do Git: $file"; done; }
status_clean(){ git diff --quiet && git diff --cached --quiet && [ -z "$(git status --short)" ]; }
pending_count(){ local branch="$1"; git rev-list --count "$REMOTE/$MAIN..$REMOTE/$branch" 2>/dev/null || echo 0; }

show_plan(){
    header
    echo "===== MAIN ====="
    git log "$REMOTE/$MAIN" -1 --oneline
    echo
    echo "===== PENDÊNCIAS POR PRIORIDADE ====="
    for branch in "${PRIORITY[@]}"; do 
        if git show-ref --verify --quiet "refs/remotes/$REMOTE/$branch"; then 
            count=$(pending_count "$branch")
            if [ "$count" -gt 0 ]; then 
                echo
                echo "🔴 $branch — $count commit(s)"
                git log --oneline --no-merges "$REMOTE/$MAIN..$REMOTE/$branch" | head -5
            else 
                echo "🟢 $branch — já sincronizada"
            fi
        else 
            echo "⚪ $branch — não encontrada"
        fi
    done
    echo
    large_files || true
}

git_commit(){ header; echo "=== COMMIT ==="; clean_generated; echo; echo "=== ARQUIVOS MODIFICADOS ==="; git status --short; status_clean && echo "ℹ️ Nenhuma alteração para commit." && pause && return 0; echo; read -r -p "Mensagem do commit: " MESSAGE; [ -z "$MESSAGE" ] && MESSAGE="chore: update AURA"; git add -A; git commit -m "$MESSAGE" || { echo "❌ Commit falhou."; pause; return 1; }; echo "✅ Commit criado."; git log -1 --oneline; pause; }
git_push(){ header; echo "=== PUSH ==="; BRANCH="$(git branch --show-current)"; large_files || { echo "❌ Arquivos acima de 100 MB. Push cancelado."; pause; return 1; }; git push "$REMOTE" "$BRANCH" && echo "✅ Push concluído para $BRANCH" || echo "❌ Push falhou. Verifica token com 'workflow'"; pause; }
build_project(){ header; echo "=== BUILD DO PROJETO ==="; [ -f "package.json" ] && echo "📦 NodeJS" && npm install && npm run build; [ -f "requirements.txt" ] && echo "🐍 Python" && pip install -r requirements.txt && echo "✅ Dependências instaladas"; [ -f "build.gradle" ] || [ -f "gradlew" ] && echo "🤖 Android" && pkg install -y openjdk-17 gradle 2>/dev/null || true && ./gradlew assembleDebug; [ -f "Makefile" ] && echo "🔨 Makefile" && make clean && make; [ -f "CMakeLists.txt" ] && echo "🔨 CMake" && cmake . && make; echo; echo "✅ Build finalizado"; pause; }

integrate_branch(){
    local branch="$1"
    echo
    echo "=============================================="
    echo " INTEGRANDO: $branch"
    echo "=============================================="
    count=$(pending_count "$branch")
    [ "$count" -eq 0 ] && echo "🟢 Nenhum commit pendente." && return 0
    echo "Commits pendentes: $count"
    git log --oneline --no-merges "$REMOTE/$MAIN..$REMOTE/$branch" | head -3
    echo
    
    if ! status_clean; then
        echo "❌ Existem alterações locais. Faça commit antes."
        return 1
    fi
    
    git checkout "$MAIN" || return 1
    git pull --ff-only "$REMOTE" "$MAIN" || { echo "❌ Não foi possível atualizar main."; return 1; }
    
    echo
    echo "=== FAZENDO MERGE ==="
    if git merge --no-ff --no-edit "$REMOTE/$branch"; then
        echo "✅ Merge concluído."
    else
        echo "❌ CONFLITO. Resolva manualmente e depois: git merge --abort"
        return 1
    fi
    
    large_files && echo "✅ Verificação OK" || { echo "❌ Arquivo grande. Revertendo."; git merge --abort 2>/dev/null || git reset --hard HEAD~1; return 1; }
    echo "✅ Branch integrada: $branch"
    return 0
}

run_all(){
    header
    echo "🚀 INICIANDO SINCRONIZAÇÃO DA AURA"
    echo
    fetch_repo || exit 1
    clean_generated
    
    if ! status_clean; then
        echo "⚠️ Alterações locais detectadas. Faça commit primeiro."
        git status --short
        exit 1
    fi
    
    large_files || { echo "❌ Existem arquivos acima de 100 MB."; exit 1; }
    
    echo
    show_plan
    echo
    echo "⚠️ VAI INTEGRAR TODAS AS BRANCHES PENDENTES NA MAIN"
    read -r -p "Digite SIM para continuar: " confirm
    [ "$confirm" != "SIM" ] && echo "Operação cancelada." && exit 0
    
    for branch in "${PRIORITY[@]}"; do 
        if git show-ref --verify --quiet "refs/remotes/$REMOTE/$branch" && [ "$(pending_count "$branch")" -gt 0 ]; then 
            integrate_branch "$branch" || { echo "🛑 PARADO NA BRANCH: $branch"; exit 1; }
        fi
    done
    
    echo
    echo "=============================================="
    echo "✅ INTEGRAÇÃO CONCLUÍDA"
    echo "=============================================="
    echo "Agora rode: git push origin main"
}

menu(){
    while true; do 
        header
        echo "1) Mostrar plano"
        echo "2) Executar sincronização e MERGE automático"
        echo "3) Ver status"
        echo "4) Ver branches pendentes"
        echo "5) Ver arquivos grandes"
        echo "6) Commit"
        echo "7) Push"
        echo "8) Build"
        echo "9) Sincronizar + Commit + Push + Build TUDO"
        echo "0) Sair"
        echo
        read -r -p "Escolha: " option
        case "$option" in 
            1) fetch_repo || true; show_plan; pause ;;
            2) run_all; pause ;;
            3) git status; pause ;;
            4) fetch_repo || true; show_plan; pause ;;
            5) large_files || true; pause ;;
            6) git_commit ;;
            7) git_push ;;
            8) build_project ;;
            9) git_commit && git_push && build_project ;;
            0) exit 0 ;;
            *) echo "❌ Opção inválida."; sleep 1 ;;
        esac
    done
}
menu
