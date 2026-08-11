#!/data/data/com.termux/files/usr/bin/bash
set +e

ROOT="$HOME/AURA"
OUT="$ROOT/coleta_analise.txt"

{
echo "=========================================="
echo " COLETA PARA ANÁLISE - AURA"
echo " Data: $(date)"
echo "=========================================="
echo

echo "===== 1. AgentSession.cs (arquivo do agente) ====="
if [ -f "$ROOT/src/AURA.AI/AgentSession.cs" ]; then
    cat "$ROOT/src/AURA.AI/AgentSession.cs"
else
    echo "[NÃO ENCONTRADO em src/AURA.AI/AgentSession.cs]"
    echo "Procurando em outro lugar..."
    find "$ROOT/src" -iname "AgentSession.cs" -not -path "*/bin/*" -not -path "*/obj/*"
fi
echo

echo "===== 2. Últimas 30 mensagens do git log ====="
cd "$ROOT" && git log -30 --oneline
echo

echo "===== 3. Status do git (mudanças não commitadas) ====="
cd "$ROOT" && git status --short
echo

echo "===== 4. Estrutura de AURA.AI / AURA.Agents ====="
find "$ROOT/src" -type d \( -iname "*AI*" -o -iname "*Agent*" \) -not -path "*/bin/*" -not -path "*/obj/*"
echo
find "$ROOT/src" -type f -name "*.cs" \( -path "*AI*" -o -path "*Agent*" \) -not -path "*/bin/*" -not -path "*/obj/*"
echo

echo "===== 5. Arquivos alterados nos últimos 3 dias ====="
find "$ROOT/src" -name "*.cs" -mtime -3 -not -path "*/bin/*" -not -path "*/obj/*"
echo

echo "===== 6. Se auditoria_memoria_AURA.txt existir, incluir ====="
if [ -f "$ROOT/auditoria_memoria_AURA.txt" ]; then
    cat "$ROOT/auditoria_memoria_AURA.txt"
else
    echo "[Ainda não foi gerado - rode ./auditoria_memoria.sh primeiro]"
fi
echo

echo "===== 7. Se existir backup da correção aplicada ====="
if [ -d "$ROOT/.aura" ]; then
    find "$ROOT/.aura" -iname "*memory-fix*" -type d
fi
echo

echo "===== FIM DA COLETA ====="

} > "$OUT" 2>&1

echo
echo "=============================================="
echo " COLETA CONCLUÍDA"
echo "=============================================="
echo "Arquivo: $OUT"
echo "Tamanho:"
wc -l "$OUT"
echo
echo "Para copiar pra área de compartilhamento do Android (se tiver storage configurado):"
echo "cp \"$OUT\" ~/storage/shared/coleta_analise.txt"
