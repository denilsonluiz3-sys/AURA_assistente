# AURA — Integração contínua de capacidades no fluxo do Agente

## Objetivo
Uma solicitação do Agente deve possuir uma única identidade de execução e uma única superfície temporária de acompanhamento. Capacidades existentes continuam sendo executores reais; a mudança é de orquestração e apresentação, não de substituição.

## Pipeline comum

```text
AgentPage
  ↓
ProcessRegistry.Begin
  ↓
CorrelationId
  ↓
AgentExecutionCoordinator
  ↓
IToolExecutor existente
  ↓
ProcessExecutorBase / stdout / stderr incremental
  ↓
AgentCapabilitySurface
  ↓
resultado padronizado
  ↓
bolha do Agente
```

## Capacidades que devem reutilizar o pipeline

- [x] Shell: executor real existente.
- [x] stdout/stderr incremental: `ProcessExecutorBase` publica eventos.
- [x] CorrelationId: `ExecutionRequest` e `ProcessOutputEventArgs` carregam a identidade.
- [x] CapabilitySurface: pode ser vinculada explicitamente ao processo.
- [x] `AgentExecutionCoordinator`: cria o processo, injeta `CorrelationId`, preserva `AgentWorkspace.ActiveRoot` e fecha o estado.
- [ ] AgentPage usar o coordenador no caminho de shell, eliminando o caminho paralelo de apresentação.
- [ ] run_program reutilizar o mesmo coordenador/superfície.
- [ ] Python reutilizar o mesmo coordenador/superfície.
- [ ] Node reutilizar o mesmo coordenador/superfície.
- [ ] Git reutilizar o mesmo coordenador/superfície.
- [ ] Memória reutilizar a mesma apresentação de processo.
- [ ] Android reutilizar a mesma apresentação de processo.
- [ ] Células reutilizar a mesma apresentação de processo.

## Integridade do fluxo

- [ ] Uma solicitação = uma execução principal.
- [ ] Uma execução = um CorrelationId.
- [ ] Uma superfície aceita apenas a execução vinculada.
- [ ] stdout/stderr não são duplicados como novas execuções.
- [ ] Resultado final não dispara novamente o mesmo comando.
- [ ] Execução concluída/falha encerra a superfície temporária.
- [ ] Cancelamento e timeout fecham corretamente o processo.

## UI — somente depois da integração

- [ ] Remover atalhos duplicados que apenas chamam o mesmo fluxo.
- [ ] Manter as capacidades disponíveis no menu do Agente.
- [ ] Esconder Chat, Logs, Executores, Células e outras superfícies secundárias somente quando o fluxo equivalente dentro do Agente estiver validado.
- [ ] Não apagar implementações existentes antes da validação no aparelho.

## Validação por lote

Cada lote deve manter:

```text
245 Warning(s)
0 Error(s)
Build: OK
Publish APK
```

Falhas de compilação devem ser corrigidas antes do próximo lote. O objetivo é chegar à integração completa sem criar um segundo motor de execução e sem remover capacidades existentes.

## Histórico

- Núcleo de processos e cards vivos já existente.
- Correlação explícita adicionada ao contrato de execução e aos eventos.
- `AgentCapabilitySurface` preparada para vínculo por processo.
- `AgentExecutionCoordinator` adicionado como porta comum para os executores existentes.
