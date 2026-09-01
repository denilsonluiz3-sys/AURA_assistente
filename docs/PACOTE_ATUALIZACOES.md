# Pacote de atualizações AURA (estado no main)

## Já aplicado

| Item | Onde |
|------|------|
| API key persistente (Preferences + SecureStorage) | `RuntimeConfig` |
| Validação de endpoint | `EndpointValidator` |
| Validação de API key (formato + live) | `ApiKeyValidator` |
| Config compacta (preset → key → modelo → Conectar) | `AiConfigView` |
| Painel ⚙ limitado (~260px) | `AgentPage.xaml` ConfigHost |
| Regras shell padrão no núcleo | `DefaultAgentSystemPrompt` + `AgentSession.Merge` |
| Continuidade entre recriações de sessão | `AgentSession` SharedHistory |
| Terminal com comandos padrão | `TerminalPage` |
| Status curto key/modelo | `AiStatusText` |

## Ainda recomendado no AgentPage (cirúrgico, sem rewrite)

1. `ModelLabel.Text = AiStatusText.ForClient(_client);`
2. `string systemPrompt = AgentSystemPrompt.Build();`
3. Remover `_session = null` antes de `EnsureSession` no run (SharedHistory já cobre)
4. Em Limpar chat: `AgentSession.ClearSharedHistory();`
5. `HasLocalLlmWithoutKey`: honrar `!RuntimeConfig.RequiresApiKey`

**Não** substituir o arquivo inteiro do AgentPage (risco PLACEHOLDER).

## Como validar no aparelho

1. ⚙ → preset → key → modelo → **Conectar**
2. Mensagem curta “Pronto · key: ok”
3. Pedir `ls` ou `pwd` no Agente
4. Terminal: `help` lista comandos padrão
