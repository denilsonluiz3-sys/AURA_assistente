#!/data/data/com.termux/files/usr/bin/bash
set -e

FILE="$HOME/AURA/src/AURA.CLI/Program.cs"

echo "=== ANALISANDO AURA.CLI ==="
echo
echo "Arquivo: $FILE"
echo

echo "=== COMANDOS EXISTENTES ==="
grep -nE '"(chat|agent|ask|ajuda|help|exit|quit|sair)"|case |Command|parts\[0\]|parts\.Length' "$FILE" | head -120 || true

echo
echo "=== ESTRUTURA DO DISPATCHER ==="

grep -nE 'while|switch|if .*command|if .*parts|StartsWith|Equals|Split|Console.ReadLine|Main\(' "$FILE" | head -160 || true

echo
echo "=== TRECHO INICIAL ==="
sed -n '1,180p' "$FILE"

echo
echo "=== TRECHO DO CHAT/AGENT ==="
grep -n -B 15 -A 25 'ChatCommand\|AgentCommand\|Ask(' "$FILE" | head -240 || true
