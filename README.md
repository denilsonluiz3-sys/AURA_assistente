# AURA_assistente

Assistente pessoal multiplataforma construído com .NET MAUI, organizado em módulos para núcleo, abstrações, memória, agentes, IA e interfaces de plataforma.

## Documentação para agentes de IA

Para agentes de IA que precisam analisar ou modificar o código-fonte, consulte primeiro o guia técnico [`README_AI.md`](README_AI.md).

Ele documenta a arquitetura real, regras de dependência, fluxo do Kernel, `PolicyGuard`, Cell Programs, integração Android, DI, testes e CI/CD, além de uma ordem recomendada para leitura do repositório.

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
