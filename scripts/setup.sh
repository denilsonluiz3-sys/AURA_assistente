#!/usr/bin/env bash
# AURA — setup do ambiente Termux.
# Instala as dependências, configura o GC OOM e faz o primeiro build.
set -euo pipefail

PREFIX="${PREFIX:-}"

log()  { printf '[setup] %s\n' "$*" >&2; }
die()  { log "ERRO: $*"; exit 1; }
have() { command -v "$1" >/dev/null 2>&1; }

# --- Pré-requisitos ---------------------------------------------------------

if [[ -z "$PREFIX" ]]; then
  die "Ambiente Termux não detectado (\$PREFIX vazio). Rode este script DENTRO do Termux."
fi

if [[ "$(id -u)" == "0" ]]; then
  die "Não rode como root: o 'pkg' do Termux não funciona como root."
fi

if [[ ! -d "$PREFIX" ]]; then
  die "PREFIX aponta para diretório inexistente: $PREFIX"
fi

# --- Instalação de pacotes --------------------------------------------------

PACOTES=(dotnet10.0 python3 openjdk-17 curl termux-tools)

log "Atualizando repositórios do Termux..."
pkg update -y || pkg update -y

log "Instalando pacotes: ${PACOTES[*]}"
pkg install -y "${PACOTES[@]}"

# --- Configuração permanente do GC (ARM64 / OOM) ----------------------------

BASHRC="$HOME/.bashrc"
export DOTNET_GCHeapHardLimit=1C0000000 DOTNET_GCHeapCount=2

ensure_export() {
  local line="$1"
  if [[ -f "$BASHRC" ]] && grep -Fq "$line" "$BASHRC"; then
    log "Já presente no .bashrc: $line"
  else
    printf '\n%s\n' "$line" >> "$BASHRC"
    log "Adicionado ao .bashrc: $line"
  fi
}

ensure_export "export DOTNET_GCHeapHardLimit=1C0000000"
ensure_export "export DOTNET_GCHeapCount=2"

# --- Localização do projeto -------------------------------------------------

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
AURA_ROOT="${AURA_ROOT:-$(dirname "$SCRIPT_DIR")}"

if [[ ! -f "$AURA_ROOT/AURA.sln" ]]; then
  die "AURA.sln não encontrado em $AURA_ROOT. Ajuste AURA_ROOT."
fi

# --- Build ------------------------------------------------------------------

log "Compilando a AURA em $AURA_ROOT..."
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
(
  cd "$AURA_ROOT" || exit 1
  dotnet build AURA.sln
)

# --- Entrypoint acessível ---------------------------------------------------

AURA_CLI="$AURA_ROOT/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll"
if [[ ! -f "$AURA_CLI" ]]; then
  die "Build não produziu o DLL do CLI: $AURA_CLI"
fi

BIN_DIR="$HOME/bin"
mkdir -p "$BIN_DIR"
WRAPPER="$BIN_DIR/aura"

if [[ -e "$WRAPPER" ]] && ! grep -Fq "$AURA_ROOT/scripts/aura.sh" "$WRAPPER"; then
  log "AVISO: $WRAPPER já existe e não aponta para a AURA; deixando como está."
else
  cat > "$WRAPPER" <<EOF
#!/usr/bin/env bash
exec "$AURA_ROOT/scripts/aura.sh" "\$@"
EOF
  chmod +x "$WRAPPER"
  log "Entrypoint criado: $WRAPPER"
fi

# Garantir ~/bin no PATH
case ":$PATH:" in
  *":$BIN_DIR:"*) : ;;
  *) printf '\nexport PATH="%s:$PATH"\n' "$BIN_DIR" >> "$BASHRC"
     log "PATH atualizado no .bashrc (reabra o terminal)." ;;
esac

log "Setup concluído. Rode 'aura' (ou '~/bin/aura') para abrir a AURA."
