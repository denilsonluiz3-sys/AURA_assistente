# AURA — plano de IA offline com tool calls

## Objetivo

Permitir que o Agente use um modelo local no Android para conversar e executar
ferramentas controladas, sem depender da API, autenticação, Firebase ou código
do Luzia.

## Situação encontrada

A AURA já possui a maior parte do núcleo agêntico:

- `AgentSession` mantém o contexto e executa o loop de ferramentas;
- `ToolRegistry` centraliza definições e resolução;
- `AgentTool` fornece o contrato das ferramentas;
- `PolicyGuard` controla operações sensíveis;
- `AgentRunStore` persiste checkpoints;
- `UniversalAiClient` já representa respostas com `AgentToolCall`.

O que falta é um runtime de inferência local que implemente o mesmo contrato de
resposta e seja integrado ao Android.

## Fases de implantação

### Fase 1 — protocolo e testes

- aceitar resposta local em JSON estrito;
- aceitar uma ou várias chamadas de ferramenta;
- aceitar JSON puro ou bloco markdown;
- rejeitar texto livre como execução;
- preservar `AgentSession` como único loop;
- cobrir parsing e falhas com testes unitários.

### Fase 2 — runtime local

- criar contrato `ILocalAiRuntime`;
- adaptar a resposta local para `AgentChatResponse`;
- manter o mesmo `ToolRegistry` usado pela IA online;
- suportar cancelamento, limite de rodadas e erro de modelo;
- não adicionar uma segunda implementação de sessão.

### Fase 3 — Android

- integrar um runtime nativo de modelo quantizado;
- armazenar o modelo em local controlado;
- informar tamanho, memória necessária e estado de carregamento;
- permitir cancelar inferência;
- descarregar o modelo quando a memória estiver sob pressão.

### Fase 4 — ferramentas offline prioritárias

A primeira lista deve ser pequena e útil:

- memória;
- leitura e busca no Workspace;
- tarefas e lembretes locais;
- diagnóstico local;
- listagem de pendências.

Shell, Terminal, execução de programas, navegador e ações destrutivas não
entram no primeiro modo offline.

### Fase 5 — seleção de modo

- Online;
- Offline;
- Automático, somente quando o usuário permitir fallback.

A UI deve mostrar o modo ativo e nunca fingir que uma resposta online foi
produzida localmente.

## Regras de segurança

- somente ferramentas registradas podem ser chamadas;
- argumentos devem ser JSON válido;
- schemas obrigatórios devem ser validados;
- ações persistentes ou externas exigem confirmação;
- `aura-sh` nunca é executado automaticamente;
- nenhuma credencial ou endpoint do Luzia será importado;
- nenhum login ou backend multiusuário é necessário para o primeiro piloto.

## Critérios de aceite

- o modelo local responde sem rede;
- uma chamada de ferramenta retorna ao modelo como resultado;
- o modelo produz uma resposta final após a ferramenta;
- a execução pode ser cancelada e retomada;
- ferramentas não permitidas são bloqueadas;
- testes unitários e APK Android passam no CI;
- o modo online existente permanece funcionando.
