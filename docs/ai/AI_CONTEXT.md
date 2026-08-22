# AURA — Contexto operacional para IAs

Este diretório é a camada de contexto operacional do repositório. O código-fonte e o GitHub são a fonte de verdade.

## Entrada rápida

1. Leia `README_AI.md`.
2. Leia `docs/ai/CI_FAILURE_LATEST.md` quando existir.
3. Leia `docs/ai/CI_FAILURE_LATEST.json` para contexto estruturado.
4. Consulte `docs/ai/ci-failures/` para histórico de falhas.
5. Consulte `.github/workflows/` para entender como a validação funciona.
6. Inspecione diretamente os arquivos e commits indicados no relatório.

## Quando houver uma falha

Não solicite logs manualmente se a execução estiver no GitHub Actions.

O workflow `AURA AI Failure Diagnostics` registra automaticamente:

- workflow e run ID;
- status e conclusão;
- branch e commit SHA;
- evento e URLs;
- jobs afetados;
- etapas que falharam;
- pull requests associados;
- arquivos alterados no commit;
- artifacts disponíveis;
- categoria provável da falha;
- evidência do log com contexto do primeiro erro causal;
- um resumo JSON para consumo automatizado.

O relatório mais recente está em `docs/ai/CI_FAILURE_LATEST.md`.

## Regra de investigação

A IA deve investigar diretamente o repositório antes de propor uma alteração:

```text
GitHub run
  ↓
job/step com falha
  ↓
primeiro erro causal
  ↓
commit/PR/arquivos envolvidos
  ↓
README_AI.md + arquitetura real
  ↓
correção mínima
  ↓
novo CI
```

Erros em cascata não devem ser tratados como causas independentes.

## Fonte de verdade

A documentação é contexto. O código, os `.csproj`, os workflows, os commits e os resultados reais do GitHub Actions têm precedência.

Não invente interfaces, dependências, APIs, scripts ou estados que não existam no repositório.

## Segurança

Os relatórios são sanitizados antes de serem publicados. Nunca publique secrets, tokens, senhas, chaves privadas ou credenciais. Se uma correção exigir PRoot/Ubuntu, gere instruções ou scripts com base nos arquivos reais do repositório.

## Prevenção de loops

Os workflows de diagnóstico publicam apenas em `docs/ai/**`. Os workflows de build/sincronização devem ignorar esse caminho quando apropriado para que atualizar o contexto não gere uma nova cadeia de builds.
