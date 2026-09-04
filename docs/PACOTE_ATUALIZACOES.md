# Pacote de atualizações AURA — segurança do Agente

Data: 2026-09-04  
Branch: `fix/agent-safety-aura-sh-dedupe`

## Problemas atacados

1. Execução automática de blocos `aura-sh` após qualquer resposta do modelo.
2. Tool calls duplicadas no mesmo run.
3. Tool calls com argumentos vazios (`write_file {}`).

## Mudanças

### AgentSession.cs (aplicado neste branch)
- Dedupe por assinatura `nome|argumentos` no mesmo run.
- Validação: rejeita `{}` / vazio para write_file, edit_file, read_file, list_dir, run_shell.

### AgentPage.xaml.cs
Substituir o método `DeliverAnswerAsync` por:

```csharp
private async Task DeliverAnswerAsync(string answer, string processId, string completeMessage)
{
    string text = string.IsNullOrWhiteSpace(answer) ? "(sem texto na resposta)" : answer.Trim();
    _lastAssistantText = text;

    await AppendBubbleAsync(text, user: false);

    // NÃO executar aura-sh automaticamente.
    // Execução só em fluxos explícitos (ex.: Colar plano).
    string? shell = LocalPlaybook.ExtractAuraShell(text);
    if (!string.IsNullOrWhiteSpace(shell))
    {
        await AppendBubbleAsync(
            "Bloco aura-sh detectado. Não executei automaticamente. Use Colar plano se quiser rodar.",
            user: false, isTool: true);
    }

    _processes.Complete(processId, completeMessage);
    _voice?.SetLastUtterance(text);
    await SpeakAsync(text);
}
```

## Como validar

1. Pedido textual simples → sem tool calls.
2. Resposta com aura-sh → aviso, sem execução.
3. Colar plano com aura-sh → executa.
4. Tool duplicada → erro de duplicata.
5. write_file {} → erro de argumentos.

## Próximos pacotes

- Modelos com/sem tools
- Unificar caminhos workspace
- Cleanup de checkpoints
