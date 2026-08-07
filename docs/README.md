# AURA — Orquestrador de Aplicativos (user-space sobre Linux LTS)

![CI](https://github.com/denilsonluiz3-sys/AURA_assistente/actions/workflows/build-test.yml/badge.svg)

AURA não é um kernel nem um OS tradicional. É um **orquestrador**: o usuário
escolhe o programa (`.py`, `.jar`, `.dll`, ...), e a AURA decide **como rodar**
— dentro de uma **célula isolada**, com ciclo de vida gerenciado (start,
pause/resume, stop, delete e recriação automática quando a célula quebra).

Hoje a AURA roda no Termux (sem root). Amanhã, no mesmo código, roda em
qualquer Linux LTS; o isolamento ganha namespaces/cgroups (e depois qcow2/KVM)
apenas trocando o backend da célula.

## Arquitetura

```
Hardware → Linux LTS (kernel) → Serviços AURA (user space) → Células isoladas
```

- **AURA.Core** — bootstrap, DI, eventos, logging, config, e agora o
  `Runtime/` de células e `Launchers/`.
- **AURA.Core/Runtime** — o coração:
  - `SimulationRuntime` — célula = processo OS separado (crash não derruba a AURA).
    Pause/resume via `SIGSTOP/SIGCONT` (funciona sem root no Termux).
  - `CellWatcher`/reciclagem — processo cai → apaga e recria a célula a partir
    do template (até 5 tentativas, para evitar loop infinito).
  - `ICellBackend` — `DirectoryCellBackend` (hoje) → `Qcow2Backend` (futuro, KVM).
- **AURA.Core/Launchers** — "AURA decide como rodar": extensão → launcher.
  - `Runner` (resolução), `PythonLauncher`, `JavaLauncher`, `DotnetLauncher`.
- **AURA.CLI** — front-end de console com os comandos `run`, `cells`, `cell ...`.
- `AURA.SystemInfo` / `AURA.Network` / `AURA.Modules` — diagnóstico e rede.

## Compilar

Requer .NET 10 SDK. No Termux:

```bash
pkg install dotnet10.0
export DOTNET_GCHeapHardLimit=1C0000000   # workaround OOM no ARM64/proot
cd AURA && dotnet build AURA.sln
```

Publicar para o celular: compilar **no próprio Termux** (cross-publish
single-file para `linux-bionic-arm64` está quebrado no .NET 9/10).

## Mobile (AURA.Mobile) é separado do `AURA.sln` de propósito

O app Android **não** está no `AURA.sln`: ele exige workload `maui-android`,
SDK Android e — para Release assinado — os segredos do keystore
(`AURA_KEYSTORE_*`), que só existem no GitHub Actions. Por isso:

- `dotnet build AURA.sln` no Termux valida **só o CLI/core** (rápido e leve);
- o mobile é buildado pelo workflow `.github/workflows/build-android-apk.yml`
  a cada push que toca `src/AURA.Mobile/**` (ou via `workflow_dispatch`);
- para pegar quebra do mobile **antes** de subir, rode localmente:

```bash
bash scripts/build-mobile.sh          # compila (-t:Compile) com o SDK local
```

O SDK Android esperado fica em `/opt/android-sdk` (defina `ANDROID_SDK_DIR`
se for outro caminho). Gerar o APK localmente sem os segredos produz um APK
assinado com a keystore de debug (instalável por sideload).

## Uso

```text
run <arquivo> [args] [--cell <id>]   # usuário escolhe o programa; AURA decide
                                     #   --mem <MB>   limite de memória (prlimit --as)
                                     #   --cpu <s>    limite de CPU (prlimit --cpu)
                                     #   --files <n>  limite de arquivos abertos
                                     #   --procs <n>  limite de processos/threads
cells                                # lista células
cell start|stop|pause|resume|delete|log|limits <id>
persist                              # grava o índice de células em ~/AURA/cells.json
diagnostico | internet | modulos | launchers | plugins | ajuda | exit
```

As células são persistidas automaticamente em `~/AURA/cells.json` a cada
mudança de estado (criar/iniciar/pausar/parar/excluir). Ao reabrir a AURA, o
runtime recarrega o índice, adota processos ainda vivos (órfãos de um crash
anterior) e recicla os que morreram enquanto a AURA estava desligada. Células
paradas propositalmente ficam `Stopped` e não são reiniciadas.

Plugins `.dll` em `~/AURA/plugins/` são carregados num `AssemblyLoadContext`
coletável e monitorados: substituir um `.dll` recarrega o plugin em tempo real
(launchers novos passam a valer na hora). Exemplo de launcher de plugin:

```csharp
public class TxtLauncher : ILauncher
{
    public string[] SupportedExtensions => new[] { ".txt" };
    public CellCommand BuildCommand(string filePath, string arguments)
        => new CellCommand("/usr/bin/cat", "\"" + filePath + "\"");
}
```

## Roadmap

- **F0 (feita):** migração `net48 → net10.0`, build 0 erros, runtime de
  células com processo isolado + reciclagem em crash.
- **F1 (feita):** persistência das células (`cells.json`) com restauração de
  estado, adoção de órfãos e hot-reload de plugins (`AssemblyLoadContext`
  coletável + `FileSystemWatcher`).
- **F2 (feita):** limites de recursos por célula via `prlimit` (`--as`, `--cpu`,
  `--nofile`, `--nproc`) — sem root, funciona no Termux. `run --mem 256 app.py`
  ou `cell limits <id> --mem 256`.
- **F3 (em andamento):** célula "assistente" — orquestrar aichat/termux-ai/
  opencode como app comum (`aura run <assistente> --cell <id>`) +
  `aura ask "pergunta"`. O opencode roda na raiz do repo (auto-melhoria);
  `scripts/aura-workspace.sh` cria o clone de trabalho; prompts em `prompts/`.
- **F4:** loja de módulos remota (`aura update`) — primeiro loja local
  (`~/AURA/loja`), depois HTTPS. Reaproveita o PluginWatcher.
- **F5:** daemon + API HTTP — no Termux via `termux-services` (runit, não
  systemd); em Linux real `systemd --user`; API via célula dedicada.
- **F6 (opcional/rebaixado):** isolamento forte com `proot`/firejail — só sob
  demanda para células suspeitas (ex.: `.jar` baixado da internet), nunca o
  padrão (proot aninhado é lento e frágil).
- **F7 (estudo):** backend `qcow2`/KVM em Linux real; kernel próprio só como
  estudo. Requer root/`/dev/kvm`, impossível no celular.
