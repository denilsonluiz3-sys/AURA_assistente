# AURA — Guia de Diagnóstico para Agentes de IA

Este arquivo define como uma IA deve investigar falhas do AURA no GitHub Actions e como transformar uma falha em correção verificável ou em um script reproduzível para PRoot Ubuntu.

## Fonte de verdade

A fonte de verdade é sempre o código e os logs reais do workflow. `README_AI.md` é o mapa arquitetural. Os relatórios em `docs/ai/ci-failures/` são evidências históricas.

## Ordem obrigatória de diagnóstico

1. Identificar workflow, run, job e commit que falharam.
2. Ler o resumo e os logs do job que realmente falhou.
3. Identificar a primeira mensagem de erro relevante; não começar pelo último erro em cascata.
4. Classificar a causa: código, dependência/NuGet, SDK/.NET, MAUI workload, Android SDK/NDK/JDK, CI/YAML, segurança ou ambiente.
5. Conferir `global.json`, `.csproj`, `Directory.Build.props`, `NuGet.Config` e workflow relacionado.
6. Localizar o símbolo/arquivo real antes de alterar qualquer código.
7. Procurar solução anterior em `docs/ai/ci-failures/`.
8. Fazer a menor correção possível.
9. Validar no GitHub Actions; não considerar uma correção concluída apenas porque compila mentalmente.
10. Se a correção exigir ambiente local, gerar ou usar um script em `scripts/ai/proot/`.

## Regras de segurança

- Nunca inserir secrets, tokens, keystores ou chaves em relatórios.
- Não executar comandos destrutivos automaticamente.
- Não fazer `git reset --hard`, `git clean -fdx` ou apagar dados do usuário sem solicitação explícita.
- Não assumir que um erro de CI é causado pelo último arquivo alterado.
- Não corrigir uma dependência arquitetural criando outra dependência proibida.
- Em `AURA.Abstractions`, não introduzir dependências de Android/MAUI sem evidência arquitetural e revisão dos projetos.
- Execuções de capacidades continuam subordinadas ao `PolicyGuard`.

## Classificação rápida de erros

### C# / compilação

Verificar primeiro o arquivo, símbolo, namespace, assinatura e referências de projeto. Procurar todas as ocorrências do tipo antes de alterar.

### NETSDK / SDK

Conferir `global.json`, versão instalada pelo runner e target framework dos `.csproj`.

### MAUI workload

Para `NETSDK1147` ou workload ausente, conferir se o runner instala `maui-android` e se a versão do SDK é compatível com o projeto.

### Android SDK / NDK / JDK

Conferir API level, NDK, Java e configuração do workflow. Não instalar versões aleatórias: alinhar ao projeto e ao workflow atual.

### NuGet / restore

Conferir `NuGet.Config`, feeds, versões e `project.assets.json`. Separar falha de rede/restore de incompatibilidade de pacote.

### XAML / Source Generator

Verificar primeiro o XAML e os tipos usados, depois o arquivo `.g.cs` gerado. Arquivos em `obj/` são sintomas, não fonte de correção.

### CI / GitHub Actions

Verificar gatilho, `paths`, permissões, concurrency, runner, actions usadas e se o workflow está reagindo a uma alteração gerada por outro workflow.

## Política de correção automática

Correção automática é aceitável quando a causa é determinística e a alteração é pequena, reversível e validável. Exemplos: YAML inválido conhecido, referência duplicada, configuração claramente incorreta ou ajuste de caminho comprovado.

Para mudanças arquiteturais, segurança, remoção de dados, secrets, permissões ou alterações de comportamento, preparar uma correção em branch/PR e deixar a validação do CI decidir.

## Quando gerar script PRoot Ubuntu

Gere um script completo quando a solução depender de ações no ambiente local. O script deve:

- usar `#!/usr/bin/env bash` e `set -euo pipefail`;
- descobrir automaticamente `~/AURA_assistente` quando possível;
- verificar pré-requisitos antes de modificar algo;
- ser idempotente;
- explicar cada ação;
- ter modo de diagnóstico sem alterações;
- não conter secrets;
- não fazer build Android por padrão, porque o build oficial ocorre no GitHub Actions;
- retornar código de saída diferente de zero quando a correção não puder ser concluída.

Modelo de uso:

```bash
cd ~/AURA_assistente
chmod +x scripts/ai/proot/aura-repair.sh
bash scripts/ai/proot/aura-repair.sh --diagnose
```

Quando a IA tiver uma correção determinística compatível, ela pode recomendar:

```bash
bash scripts/ai/proot/aura-repair.sh --apply
```

O build remoto continua sendo a validação final.

## Histórico

O workflow `ai-failure-diagnostics.yml` registra falhas de `AURA Android APK` e `CodeQL Advanced` em `docs/ai/ci-failures/`. O relatório contém run, commit, workflow, job, causa provável e trecho de log suficiente para uma IA iniciar o diagnóstico.

## Resultado esperado para uma solicitação de ajuda

Uma IA que receba um erro do AURA deve responder com uma destas duas formas:

1. Correção implementável: arquivo(s), alteração mínima, validação e resultado do CI.
2. Correção local: causa, pré-requisitos, script PRoot Ubuntu completo e comando exato para executá-lo.

Nunca responder apenas com uma hipótese quando o log real estiver disponível.
