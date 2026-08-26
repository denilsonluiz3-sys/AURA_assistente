#!/bin/sh
# test-script.sh — executável via run_shell
echo "=== Teste AURA Agent ==="
echo "Data: $(date 2>/dev/null || echo 'date indisponivel')"
echo " diretorio: $(pwd)"
echo " arquivos:"
ls -la
echo ""
echo "=== Sistema ==="
echo "modelo: $(getprop ro.product.model 2>/dev/null || echo 'indisponivel')"
echo "android: $(getprop ro.build.version.release 2>/dev/null || echo 'indisponivel')"
echo "disco:"
df -h 2>/dev/null | head -n 4
echo ""
echo "=== Fim do teste ==="
