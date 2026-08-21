# AURA_assistente — Guia para Agentes de IA

Este arquivo é a entrada técnica recomendada para agentes de IA que precisam analisar, modificar, testar ou revisar o código-fonte do AURA_assistente.

## Objetivo

AURA_assistente é um assistente pessoal multiplataforma construído em .NET MAUI, com arquitetura modular. O código existente é a fonte de verdade; esta documentação não deve ser usada para justificar uma arquitetura paralela.

## Estrutura principal

```text
src/
├── AURA.Abstractions/   Contratos e abstrações compartilhadas
├── AURA.Core/           Núcleo, logging e componentes fundamentais
├── AURA.Memory/         Memória e armazenamento de soluções/regras
├── AURA.Agents/         Orquestração, agentes, intenção, políticas e programas
├── AURA.AI/             Integração com provedores/modelos de IA
├── AURA.Modules/        Metadados e ciclo de vida de módulos
├── AURA.Mobile/         Aplicação .NET MAUI e integração Android
└── AURA.Windows/        Componentes da plataforma Windows

tests/                   Testes automatizados
AURA.sln                Solution principal
.github/workflows/      CI/CD e validações
```

Confirme os nomes e responsabilidades no código antes de fazer alterações estruturais.

## Regras de dependência

- `AURA.Abstractions` deve permanecer independente de Android, MAUI e implementações concretas.
- `AURA.Core` contém fundamentos reutilizáveis e não deve depender da UI móvel.
- `AURA.Memory` fornece memória sem depender da UI.
- `AURA.Agents` concentra agentes, intenção, políticas, ferramentas e orquestração.
- `AURA.AI` integra modelos/provedores sem tornar um provedor específico obrigatório para o Kernel.
- `AURA.Mobile` contém UI e adapters específicos de Android/MAUI.
- Serviços Android não devem ser expostos diretamente por contratos em `AURA.Abstractions`.

Antes de adicionar uma referência entre projetos, verifique os `.csproj` reais.

## Fluxo do Kernel

O Kernel é distribuído pelos módulos existentes; não crie um projeto artificial `AURA.Kernel` sem necessidade arquitetural comprovada.

```text
Entrada do usuário
      ↓
AuraOrchestrator
      ↓
Memória / intenção
      ↓
PolicyGuard
      ↓
ToolResolver / programa / ferramenta
      ↓
Runner / execução
      ↓
Resultado
      ↓
UI ou resposta do agente
```

LLM/provedores são componentes de inteligência/inferência e não devem ser requisito obrigatório para operações determinísticas.

## Segurança

Toda execução que use capacidades deve passar pela política antes da execução:

```text
Resolver → identificar capacidades → PolicyGuard → executar
```

Uma capacidade desconhecida deve ser bloqueada por padrão. A UI não deve contornar o `PolicyGuard`.

## Cell Programs

Cell Programs são programas internos controlados. A V1 não representa isolamento real de processos ou sandbox de segurança.

Componentes principais:

- `IAuraCellProgram`
- `IAuraCellContext`
- `IAuraCellContextFactory`
- `CellProgramRegistry`
- `CellProgramRunner`
- `DeviceDiagnosticProgram`

Cada programa declara `RequiredCapabilities`.

O contexto de `AURA.Abstractions` não deve expor `IAndroidCapabilityService`. A integração Android ocorre por adapter/contexto em `AURA.Mobile`.

Fluxo:

```text
Apps → Programas → ProgramsPage
                  ↓
            ProgramCardViewModel
                  ↓
             PolicyGuard
                  ↓
          CellProgramRunner
                  ↓
           IAuraCellContext
                  ↓
            Adapter Android
                  ↓
       IAndroidCapabilityService
                  ↓
          resultado no card
```

## UI de Programas

Arquivos principais:

- `src/AURA.Mobile/Pages/ProgramsPage.xaml`
- `src/AURA.Mobile/Pages/ProgramsPage.xaml.cs`
- `src/AURA.Mobile/ViewModels/ProgramsPageViewModel.cs`

Cada programa aparece como um card independente com estado, capacidades, resultado e ação de execução. A UI não deve chamar diretamente serviços Android quando existe um programa/runner/policy para a operação.

## IntentResolver

O resolvedor transforma comandos em intenções estruturadas. Exemplo da V1:

```text
"diagnóstico do aparelho"
        ↓
intent = android
        ↓
action = device-diagnostic
        ↓
CellProgramRegistry.Resolve()
```

Localize primeiro o resolver realmente usado pelo `AuraOrchestrator`; não duplique regras em camadas diferentes.

## Memória

`AURA.Memory` contém os mecanismos existentes, incluindo `MemoryStore`, `MemoryEntry` e regras/soluções. Antes de criar outro sistema de memória, procure e reutilize o existente quando compatível.

## IA e provedores

`AURA.AI` contém componentes de sessão/agente, resultados de ferramentas e runtime/catalogação de provedores. Falha de um provedor não deve ser confundida com falha estrutural do Kernel.

## DI

`src/AURA.Mobile/MauiProgram.cs` é um ponto central de composição MAUI. Ao adicionar serviços, confirme contrato, implementação, projeto correto, consumidores e construtores antes de registrar no DI.

Não injete uma implementação Android em um projeto que deve permanecer multiplataforma.

## Testes

Priorize testes para resolução de intenção, autorização de capacidades, bloqueio de capacidades desconhecidas, registry, runner e regras arquiteturais. Testes existentes de Cell Programs cobrem `device-diagnostic`, capacidades permitidas/bloqueadas e resolução case-insensitive do registry.

## CI/CD

GitHub Actions executa build, testes e análises remotamente. Ao diagnosticar CI, identifique workflow/job, leia a etapa que falhou, classifique a causa como código/dependência/SDK/workload/configuração, corrija a causa e valide novamente.

## Procedimento recomendado para uma IA

Antes de modificar código:

1. Leia `AURA.sln`.
2. Liste os projetos `src/*` e `tests/*`.
3. Leia os `.csproj` relevantes.
4. Localize interfaces e implementações existentes.
5. Siga os consumidores do componente alterado.
6. Verifique `MauiProgram.cs`.
7. Verifique `AuraOrchestrator`.
8. Verifique `PolicyGuard` quando houver execução de capacidades.
9. Procure testes existentes.
10. Faça a menor alteração necessária.

## Regras para alterações automatizadas

- Não duplicar interfaces existentes.
- Não criar dependências Android em abstrações.
- Não mover código entre camadas sem necessidade comprovada.
- Não criar implementações especulativas quando já existe uma implementação funcional.
- Não contornar `PolicyGuard`.
- Não adicionar scripts temporários de teste ao produto sem necessidade.
- Não colocar segredos ou API keys no código.
- Preferir mudanças pequenas e verificáveis.
- Atualizar testes quando o comportamento mudar.
- Usar CI como validação final.

## Fonte de verdade

Este documento é um mapa para agentes de IA, não uma cópia do código. O código-fonte atual sempre tem precedência. Se houver divergência, a IA deve verificar o código e atualizar esta documentação quando a mudança arquitetural for intencional.

## Ordem recomendada de leitura

```text
AURA.sln
  ↓
*.csproj relevantes
  ↓
AURA.Abstractions
  ↓
AURA.Core
  ↓
AURA.Memory
  ↓
AURA.Agents
  ↓
AURA.AI
  ↓
AURA.Mobile/MauiProgram.cs
  ↓
AURA.Mobile/MainPage.cs
  ↓
testes relacionados
  ↓
.github/workflows/*
```
