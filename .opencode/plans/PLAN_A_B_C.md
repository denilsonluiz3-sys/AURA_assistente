# Plano AURA — Fases A + B + C

**Data:** 2026-08-26
**Autor:** Denilson Luiz Spinassi (via IA)
**Regra:** mínimo de código novo; memória = ações executáveis; APK via Codemagic

---

## Diagnóstico

A AURA tem peças boas espalhadas. O agente tem 13 tools mas o app ainda forceixa tudo pelo campo de texto. A memória procedural funciona mas o LLM em loop gasta tokens à toa. Células e executores existem mas quase não são acessíveis pelo agente.

**Norte:** O Agente é o controle remoto do app. Botões e células fazem o trabalho. LLM é consultor opcional.

---

## Fase A — Correção de Bugs de Sessão

**Objetivo:** o agente para de crashar e de gastar tokens à toa.

### A1. Defesa contra "Collection was modified"

**Arquivo:** `src/AURA.AI/AgentSession.cs` (linha ~113-171) + `src/AURA.AI/OpenRouterClient.cs` (linha ~164)

**Problema:** `_messages` (List) é passada por referência ao `ChatToolsAsync`, que faz `foreach` nela. Se algum evento `Step` ou thread secundária modificar a lista durante iteração, crasha.

**Correção:** copiar a lista antes de passar ao `ChatToolsAsync`:

```csharp
// AgentSession.cs, na chamada do loop:
var snapshot = new List<AgentMessage>(_messages);
AgentChatResponse response = await _client.ChatToolsAsync(snapshot, ...);
```

Mínimo de código: 1 linha.

### A2. TrimHistory seguro

**Arquivo:** `src/AURA.AI/AgentSession.cs` (linhas 43-49)

**Problema:** `RemoveRange(0, count - 16)` pode cortar no meio de um tool_call + tool_result, quebrando o protocolo do LLM e causando erro 400.

**Correção:** só cortar em pares (assistant + tool_result):

```csharp
private void TrimHistory()
{
    if (_messages.Count <= MaxHistoryMessages)
        return;

    // Encontrar ponto seguro: pular do início até um par user/assistant
    int removeUpTo = 0;
    for (int i = 0; i < _messages.Count - MaxHistoryMessages; i++)
    {
        if (_messages[i].Role == "user" || _messages[i].Role == "assistant")
        {
            removeUpTo = i + 1;
        }
    }

    if (removeUpTo > 0)
        _messages.RemoveRange(0, removeUpTo);
}
```

### A3. MaxRounds configurável + baixo

**Arquivo:** `src/AURA.AI/AgentSession.cs` (linha 22)

**Problema:** `MaxRounds = 16` é hardcoded. Em uso real, 2-3 rounds bastam.

**Correção:** tornar configurável via construtor, com default baixo:

```csharp
private readonly int _maxRounds;

public AgentSession(OpenRouterClient client, ..., int maxRounds = 3)
{
    _maxRounds = maxRounds;
    // ...
}
```

Mudar `while (round++ < MaxRounds)` → `while (round++ < _maxRounds)`.

### A4. Erros 429/400 com ErrorKind real

**Arquivo:** `src/AURA.AI/AgentSession.cs` (linhas 131-133)

**Problema:** `ClassifyError` popula `ErrorKind` no `AgentChatResponse`, mas o session descarta e lança `InvalidOperationException`. A UI faz `m.Contains("429")` (frágil).

**Correção:** usar `AgentLlmException` que já existe mas nunca é usado:

```csharp
if (!string.IsNullOrEmpty(response.Error))
{
    throw new AgentLlmException(response.Error, response.ErrorKind);
}
```

Na UI (`AgentPage.xaml.cs`), pegar `ErrorKind` diretamente:

```csharp
catch (AgentLlmException ex)
{
    string msg = ex.ErrorKind switch
    {
        AgentErrorKind.RateLimited => "Rate limit. Use Web AI ou aguarde.",
        AgentErrorKind.PaymentRequired => "Sem créditos. Use Web AI (📋 Contexto).",
        AgentErrorKind.InvalidApiKey => "API key inválida.",
        _ => FriendlyLlmError(ex)
    };
    await DeliverErrorBubbleAsync(msg);
}
```

### A5. Mensagem amigável para 429

Já existe parcialmente. Reforçar com chip sugerido "use Web AI":

```csharp
if (ex.ErrorKind == AgentErrorKind.RateLimited)
    return "Rate limit atingido. Use a aba Web AI ou copie o contexto (📋) e cole no ChatGPT/DeepSeek.";
```

### Arquivos da Fase A

| Arquivo | Mudança |
|---------|---------|
| `src/AURA.AI/AgentSession.cs` | Snapshot da lista, TrimHistory seguro, MaxRounds configurável, throw AgentLlmException |
| `src/AURA.Mobile/Pages/AgentPage.xaml.cs` | Catch AgentLlmException com ErrorKind |

---

## Fase B — Gaveta do Agente (Menu de Ações)

**Objetivo:** o agente vira um controle remoto com botões, não só um campo de texto.

### B1. Botão de Menu no header

**Arquivo:** `src/AURA.Mobile/Pages/AgentPage.xaml`

Adicionar um botão de menu (ícone ⚡) ao lado do botão de config (gear):

```xml
<!-- Row 0, header bar, após ConfigButton -->
<Button x:Name="AgentMenuButton"
        Text="⚡" FontSize="18"
        BackgroundColor="Transparent"
        Clicked="OnAgentMenuClicked" />
```

### B2. Popup de ações (DisplayActionSheet)

**Arquivo:** `src/AURA.Mobile/Pages/AgentPage.xaml.cs`

Usar o padrão já existente no `BrowserPage` (`DisplayActionSheetAsync`). Menu principal com subfunções:

```csharp
private async void OnAgentMenuClicked(object? sender, EventArgs e)
{
    string action = await DisplayActionSheetAsync(
        "⚡ AURA Agent", "Fechar", null,
        // ── Ações Locais (sem LLM) ──
        "📂 Workspace",
        "🔍 Diagnóstico",
        "🧠 Memória",
        "▶ Continuar",
        "🖥️ Shell",
        // ── Células ──
        "📋 Células",
        "▶ Rodar programa",
        // ── Web ──
        "🌐 Contexto para Web AI",
        "📋 Colar plano",
        // ── Config ──
        "⚙ Configurar IA",
        // ── Adicionar ──
        "➕ Adicionar atalho"
    );

    switch (action)
    {
        case "📂 Workspace": OnChipWorkspace(sender, e); break;
        case "🔍 Diagnóstico": OnChipDiagnostic(sender, e); break;
        case "🧠 Memória": OnChipMemory(sender, e); break;
        case "▶ Continuar": OnChipContinue(sender, e); break;
        case "🖥️ Shell": OnChipShellSafe(sender, e); break;
        case "📋 Células": await OnCellsSubmenu(); break;
        case "▶ Rodar programa": await OnRunProgramSubmenu(); break;
        case "🌐 Contexto para Web AI": OnCopyContextClicked(sender, e); break;
        case "📋 Colar plano": OnPastePlanClicked(sender, e); break;
        case "⚙ Configurar IA": SetConfigVisible(true); break;
        case "➕ Adicionar atalho": await OnAddShortcutAsync(); break;
    }
}
```

### B3. Submenu de Células

```csharp
private async Task OnCellsSubmenu()
{
    string action = await DisplayActionSheetAsync(
        "📋 Células", "Voltar", null,
        "Ver status", "Ver log", "Parar célula");

    switch (action)
    {
        case "Ver status":
            CommandEditor.Text = "liste as células e mostre o status de cada uma";
            await OnRunClicked(sender: this, EventArgs.Empty);
            break;
        case "Ver log":
            CommandEditor.Text = "mostre o log das últimas 30 linhas de todas as células";
            await OnRunClicked(sender: this, EventArgs.Empty);
            break;
        case "Parar célula":
            CommandEditor.Text = "pare todas as células que estejam rodando";
            await OnRunClicked(sender: this, EventArgs.Empty);
            break;
    }
}
```

### B4. Submenu "Rodar programa"

```csharp
private async Task OnRunProgramSubmenu()
{
    if (_cellRegistry == null)
    {
        await DeliverErrorBubbleAsync("Células não disponíveis.");
        return;
    }

    var programs = _cellRegistry.All.ToList();
    if (programs.Count == 0)
    {
        await DeliverErrorBubbleAsync("Nenhum programa registrado.");
        return;
    }

    string[] names = programs.Select(p => p.Name).ToArray();
    string chosen = await DisplayActionSheetAsync(
        "▶ Rodar programa", "Cancelar", null, names);

    var program = programs.FirstOrDefault(p => p.Name == chosen);
    if (program != null)
    {
        CommandEditor.Text = "run_program " + program.Name;
        await OnRunClicked(sender: this, EventArgs.Empty);
    }
}
```

### B5. "Adicionar atalho" — funções customizáveis

**Objetivo:** o usuário pode criar atalhos que o agente executa sem LLM.

```csharp
private async Task OnAddShortcutAsync()
{
    string name = await DisplayPromptAsync("Novo atalho", "Nome do atalho:");
    if (string.IsNullOrWhiteSpace(name)) return;

    string command = await DisplayPromptAsync("Comando", "Comando shell ou ação:",
        "ex.: getprop ro.product.model");
    if (string.IsNullOrWhiteSpace(command)) return;

    // Salvar no SolutionStore (memória procedural)
    _solutions?.Record(
        task: name,
        actionTaken: "```aura-sh\n" + command + "\n```",
        result: "atalho criado pelo usuário",
        success: true);

    await DeliverAnswerBubbleAsync("✅ Atalho '" + name + "' criado. Diga \"" + name + "\" para executar.");
}
```

### B6. Chip ➕ no bar existente

Adicionar chip `➕` no bar de chips do XAML:

```xml
<Button Text="➕" Clicked="OnAddShortcutClicked"
        FontSize="11" Padding="12,6" HeightRequest="34"
        BackgroundColor="{DynamicResource AuraAccent}"
        CornerRadius="17" TextColor="{DynamicResource AuraTextPrimary}" />
```

Handler:
```csharp
private void OnAddShortcutClicked(object? sender, EventArgs e)
    => _ = OnAddShortcutAsync();
```

### Arquivos da Fase B

| Arquivo | Mudança |
|---------|---------|
| `src/AURA.Mobile/Pages/AgentPage.xaml` | Botão ⚡ no header + chip ➕ |
| `src/AURA.Mobile/Pages/AgentPage.xaml.cs` | `OnAgentMenuClicked`, submenus, `OnAddShortcutAsync`, `OnAddShortcutClicked` |

---

## Fase C — Células via Menu do Agente

**Objetivo:** células saem do subsystem isolado e viram ações acessíveis pelo agente.

### C1. Comando "células" no agente

Quando o usuário digita "células" ou clica no submenu, o agente:

1. Lista células via shell: `ls -la /data/data/com.aura.genesis/files/cells/`
2. Mostra status de cada uma
3. Oferece ações: iniciar, parar, ver log

### C2. Comando "run_program" via menu

Já coberto pelo submenu B4. O fluxo é:

```
Menu → "▶ Rodar programa" → escolher programa → CommandEditor.Text = "run_program X" → OnRunClicked
```

### C3. Comando "cell log" / "cell stop"

Via submenu B3, que preenche o CommandEditor com o comando apropriado e dispara `OnRunClicked`.

### C4. Prompt do agente atualizado

No `EnsureSession()`, atualizar o system prompt para mencionar o menu:

```csharp
"MENU RÁPIDO: use o botão ⚡ para acessar Workspace, Diagnóstico, Memória, Células, Web AI e Adicionar atalhos sem digitar."
```

### Arquivos da Fase C

| Arquivo | Mudança |
|---------|---------|
| `src/AURA.Mobile/Pages/AgentPage.xaml.cs` | Submenus de células, system prompt atualizado |

---

## Ordem de Implementação

```
Fase A (bugs) → Fase B (menu) → Fase C (células via menu) → commit → Codemagic
```

### Sequência de commits

1. `fix(agent): defensive snapshot + safe trim + configurable MaxRounds`
2. `fix(agent): use AgentLlmException with ErrorKind instead of InvalidOperationException`
3. `feat(agent): add ⚡ action menu with local actions + submenus`
4. `feat(agent): add shortcut creator (➕) for user-defined actions`
5. `feat(agent): cells submenu (status/log/stop) + program runner via menu`

### Validação

- [ ] Build no Codemagic passa (error CS = zero)
- [ ] Menu ⚡ aparece no header
- [ ] Cada opção do menu executa a ação correta
- [ ] "Adicionar atalho" salva e reexecuta
- [ ] Submenu células mostra status
- [ ] Erro 429 mostra mensagem amigável com sugestão Web AI
- [ ] MaxRounds baixo (3) evita loops longos

---

## O que NÃO fazer (da Fase A-C)

- Não reativar cascata de GitHub Actions
- Não criar novo projeto ou refatorar módulos inteiros
- Não depender de prompt para "economizar tokens"
- Não expandir executores Git/Python/Node no Android (secundário)
- Não mexer no runtime de células (SimulationRuntime) — só acessar via shell
