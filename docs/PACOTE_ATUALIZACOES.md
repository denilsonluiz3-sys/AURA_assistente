# Pacote AURA — visão e estado

## Visão (prioridade)

**Agente útil + terminal padrão + config simples + continuar de onde parou + menos abas.**

## Abas visíveis (MainPage)

| Seção | Aba |
|-------|-----|
| Assistente | **Agente** |
| Ferramentas | **Terminal** |
| Sistema | Início, Diagnóstico (se módulo system) |

Chat, Células, Programas, Executores, Módulos, Logs, Navegador separado, etc. **não** entram nas abas (código/DI preservados).

Web AI fica **dentro** do Agente (modo Web AI).

## Já no núcleo

- Config: preset → key → modelo → **Conectar** (painel curto)
- Key/endpoint validados
- Shell padrão no `DefaultAgentSystemPrompt`
- Continuidade: `AgentSession` SharedHistory
- Stop: `BeginAmbientRun` / `CancelAmbientRun` + CT no HTTP
- UX: ▶/■, 🔊 nas bolhas, status `AiStatusText`

## Validar no APK

1. Só ver poucas abas (Agente + Terminal + Início…)
2. ⚙ Conectar → key ok
3. `ls` / continue na 2ª mensagem
4. ■ cancela run
5. 🔊 lê resposta
