#!/data/data/com.termux/files/usr/bin/bash

set +e

ROOT="$(pwd)"
OUT="$ROOT/auditoria_memoria_AURA.txt"

{
echo "============================================================"
echo " AUDITORIA DO SISTEMA DE MEMÓRIA - AURA"
echo "============================================================"
echo
echo "Data: $(date)"
echo "Projeto: $ROOT"
echo "Usuário: $(whoami)"
echo "Shell: $SHELL"
echo "Termux: ${PREFIX:-desconhecido}"
echo

echo "============================================================"
echo "1. ESTRUTURA DO PROJETO"
echo "============================================================"
echo

echo "--- Projetos .csproj ---"
find "$ROOT/src" -type f -name '*.csproj' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -print 2>/dev/null

echo
echo "--- Soluções ---"
find "$ROOT" -type f \( -name '*.sln' -o -name '*.slnx' \) \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -print 2>/dev/null

echo
echo "--- Diretórios principais ---"
find "$ROOT/src" -maxdepth 2 -type d \
    -not -path '*/bin*' \
    -not -path '*/obj*' \
    -print 2>/dev/null

echo

echo "============================================================"
echo "2. ARQUIVOS RELACIONADOS À MEMÓRIA"
echo "============================================================"
echo

find "$ROOT/src" -type f \
    \( \
    -iname '*memory*' -o \
    -iname '*memo*' -o \
    -iname '*store*' -o \
    -iname '*context*' -o \
    -iname '*knowledge*' -o \
    -iname '*lesson*' -o \
    -iname '*experience*' -o \
    -iname '*recall*' -o \
    -iname '*history*' \
    \) \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -print 2>/dev/null

echo

echo "============================================================"
echo "3. CLASSES / INTERFACES DE MEMÓRIA"
echo "============================================================"
echo

grep -RniE \
'Memory|MemoryStore|PersistentMemory|IMemory|MemoryService|MemoryManager|MemoryRepository|I.*Memory' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "4. PERSISTÊNCIA"
echo "============================================================"
echo

grep -RniE \
'SQLite|Sqlite|sqlite|\.db|\.sqlite|\.sqlite3|JsonSerializer|Serialize|Deserialize|WriteAllText|ReadAllText|WriteAllBytes|ReadAllBytes|FileStream|StreamWriter|StreamReader|OpenOrCreate|FileMode|Directory.CreateDirectory' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "5. APRENDIZADO / EXPERIÊNCIAS / LIÇÕES"
echo "============================================================"
echo

grep -RniE \
'Learn|Learning|Learned|Lesson|Experience|Feedback|Correction|Outcome|Reflection|Reflect|Success|Failure|Observation|Pattern' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "6. RECUPERAÇÃO DE MEMÓRIA"
echo "============================================================"
echo

grep -RniE \
'Recall|Retrieve|Retrieval|SearchMemory|FindMemory|GetMemory|LoadMemory|Load.*Context|Relevant|Similarity|Embedding|Vector|Semantic' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "7. CONTEXTO ENVIADO AO MODELO"
echo "============================================================"
echo

grep -RniE \
'Ollama|Chat|Prompt|Messages|SystemMessage|UserMessage|Completion|Generate|Inference|Model|Context' \
"$ROOT/src/AURA.Agents" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "8. AGENTE / ORQUESTRAÇÃO"
echo "============================================================"
echo

grep -RniE \
'ToolCall|ToolResult|ExecuteTool|InvokeTool|CallTool|ExecuteAsync|ToolName|Arguments|AgentSession|AgentLoop|Iteration|MaxIterations|MaxSteps' \
"$ROOT/src/AURA.Agents" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "9. CONFIGURAÇÕES DA AURA"
echo "============================================================"
echo

find "$ROOT" -type f \
    \( \
    -name '*.json' -o \
    -name '*.yaml' -o \
    -name '*.yml' -o \
    -name '*.xml' -o \
    -name '*.config' \
    \) \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -not -path '*/.git/*' \
    -print 2>/dev/null

echo

echo "============================================================"
echo "10. DIRETÓRIO ~/.aura"
echo "============================================================"
echo

if [ -d "$HOME/.aura" ]; then
    echo "Existe: $HOME/.aura"
    echo

    echo "--- Arquivos ---"
    find "$HOME/.aura" -type f -print 2>/dev/null

    echo
    echo "--- Tamanhos ---"
    du -ah "$HOME/.aura" 2>/dev/null | sort -h | tail -50
else
    echo "Diretório ~/.aura NÃO encontrado."
fi

echo

echo "============================================================"
echo "11. POSSÍVEIS ARQUIVOS DE BANCO / MEMÓRIA"
echo "============================================================"
echo

find "$HOME/.aura" "$ROOT" \
    -type f \
    \( \
    -iname '*.db' -o \
    -iname '*.sqlite' -o \
    -iname '*.sqlite3' -o \
    -iname '*memory*.json' -o \
    -iname '*memory*.db' -o \
    -iname '*history*.json' -o \
    -iname '*knowledge*.json' \
    \) \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -not -path '*/.git/*' \
    -print 2>/dev/null

echo

echo "============================================================"
echo "12. REFERÊNCIAS A ~/.aura / WORKSPACE"
echo "============================================================"
echo

grep -RniE \
'\.aura|AURA_HOME|workspace|Workspace|HOME|Environment.GetFolderPath|Environment.GetEnvironmentVariable' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null

echo

echo "============================================================"
echo "13. DEPENDÊNCIAS RELACIONADAS À MEMÓRIA"
echo "============================================================"
echo

grep -RniE \
'SQLite|sqlite|EntityFramework|EFCore|LiteDB|Lucene|Qdrant|Chroma|Milvus|Vector|Embedding|Semantic|Memory' \
"$ROOT" \
--include='*.csproj' \
--include='*.props' \
--include='*.targets' \
--include='*.json' \
--exclude-dir=bin \
--exclude-dir=obj \
--exclude-dir=.git \
2>/dev/null

echo

echo "============================================================"
echo "14. GIT / ESTADO DO PROJETO"
echo "============================================================"
echo

git status --short 2>/dev/null
echo
git branch --show-current 2>/dev/null
echo
git log -5 --oneline 2>/dev/null

echo

echo "============================================================"
echo "15. SDK / DOTNET"
echo "============================================================"
echo

dotnet --info 2>&1

echo

echo "============================================================"
echo "16. ARQUIVOS CS RELACIONADOS AO AGENTE"
echo "============================================================"
echo

find "$ROOT/src/AURA.Agents" -type f -name '*.cs' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    -print 2>/dev/null

echo

echo "============================================================"
echo "17. RESUMO AUTOMÁTICO"
echo "============================================================"
echo

echo "Quantidade de arquivos C#:"
find "$ROOT/src" -type f -name '*.cs' \
    -not -path '*/bin/*' \
    -not -path '*/obj/*' \
    2>/dev/null | wc -l

echo
echo "Quantidade de referências a Memory:"
grep -RniE 'Memory|MemoryStore|PersistentMemory|IMemory' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null | wc -l

echo
echo "Quantidade de referências a Learning:"
grep -RniE 'Learn|Learning|Lesson|Experience|Feedback|Reflection' \
"$ROOT/src" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null | wc -l

echo
echo "Quantidade de referências a SQLite:"
grep -RniE 'SQLite|Sqlite|sqlite' \
"$ROOT/src" \
--include='*.cs' \
--include='*.csproj' \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null | wc -l

echo
echo "============================================================"
echo " FIM DA AUDITORIA"
echo "============================================================"

} > "$OUT" 2>&1

echo
echo "=============================================="
echo " AUDITORIA CONCLUÍDA"
echo "=============================================="
echo
echo "Arquivo criado:"
echo "$OUT"
echo
echo "Tamanho:"
wc -c "$OUT"
echo
echo "Para visualizar:"
echo "cat \"$OUT\""
echo
