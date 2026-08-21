# AURA_assistente

Assistente pessoal multiplataforma construído com .NET MAUI, organizado em módulos para núcleo, abstrações, memória, agentes, IA e interfaces de plataforma.

## Documentação para agentes de IA

Para agentes de IA que precisam analisar ou modificar o código-fonte, consulte primeiro o guia técnico [`README_AI.md`](README_AI.md).

Ele documenta a arquitetura real, regras de dependência, fluxo do Kernel, `PolicyGuard`, Cell Programs, integração Android, DI, testes e CI/CD, além de uma ordem recomendada para leitura do repositório.

O `README_AI.md` também possui um snapshot automático da árvore de código e das referências entre projetos, atualizado pelo GitHub Actions após mudanças relevantes no código.

## Atualização automática da documentação de IA

O workflow [`update-ai-docs.yml`](.github/workflows/update-ai-docs.yml) mantém o `README_AI.md` sincronizado com o estado real do repositório.

Ele é acionado após mudanças em `src/`, `tests/`, arquivos `.csproj`, `AURA.sln`, configurações de build ou workflows. O workflow reconstrói o snapshot técnico e publica a atualização automaticamente na `main`.

Alterações feitas somente no `README_AI.md` não acionam o próprio workflow, evitando loops de execução.

O código-fonte continua sendo a fonte de verdade; o `README_AI.md` é um mapa atualizado para agentes de IA.

## Código-fonte

- `src/AURA.Abstractions` — contratos compartilhados
- `src/AURA.Core` — núcleo e componentes fundamentais
- `src/AURA.Memory` — memória e soluções/regras
- `src/AURA.Agents` — agentes, intenção, políticas, ferramentas e orquestração
- `src/AURA.AI` — integração com IA e provedores
- `src/AURA.Modules` — módulos e metadados
- `src/AURA.Mobile` — aplicação .NET MAUI e Android
- `src/AURA.Windows` — componentes Windows
- `tests` — testes automatizados

## Desenvolvimento

O código existente é a fonte de verdade. Antes de alterar uma camada, verifique os projetos, contratos, referências entre `.csproj`, DI, testes e workflows do GitHub Actions.

Consulte [`README_AI.md`](README_AI.md) para as regras completas de arquitetura e desenvolvimento automatizado.
