# Falha CI — AURA Android APK — run 32605329261

## Identidade da execução

- Workflow: `AURA Android APK`
- Run ID: `32605329261`
- Status: `completed`
- Conclusão: `failure`
- Branch: `main`
- Commit: `0ce57cffcb6a3d15d7ed970d46e4a2f32b6d98d7`
- Evento: `push`
- Criado: `2026-08-22T23:28:50Z`
- Atualizado: `2026-08-22T23:30:34Z`
- URL: https://github.com/denilsonluiz3-sys/AURA_assistente/actions/runs/32605329261
- Categoria provável: **C# / compilação**
- Infra-only (não publicar commit): `False`

## Jobs relevantes

- `build-apk` — conclusion=`failure`, status=`completed`

## Etapas com falha

- `build-apk` → `Build APK` — conclusion=`failure`

## Pull requests associados

- PR #79: feat: playbook local (menos IA) + APK sem Kokoro/MP4 — https://github.com/denilsonluiz3-sys/AURA_assistente/pull/79

## Arquivos alterados no commit

- `docs/ai/AURA_LOCAL_PLAYBOOK.md`
- `src/AURA.Mobile/AURA.Mobile.csproj`
- `src/AURA.Mobile/Pages/AgentPage.xaml.cs.fix`
- `src/AURA.Mobile/Pages/AgentPage.xaml.cs.tmp`
- `src/AURA.Mobile/Resources/Raw/.gitkeep`
- `src/AURA.Mobile/Services/LocalPlaybook.cs`

## Procedimento obrigatório para uma IA

1. Trate o GitHub como fonte de verdade.
2. Leia `README_AI.md` antes de alterar código.
3. Ignore falhas de quota de artifact / CodeQL desabilitado — não são bugs de produto.
4. Localize o primeiro erro causal de compilação/código.
5. Faça a menor correção e valide no Actions.

## Evidência do log

```text
ssistente/AURA_assistente/src/AURA.Core/Runtime/SimulationRuntime.cs(124,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6661239Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6675143Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Modules' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6683847Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(7,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6692634Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(8,23): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6701460Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(9,23): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6709545Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(10,23): warning CS8618: Non-nullable property 'Status' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6718321Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(11,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6725232Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(72,28): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6731208Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(76,24): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6737090Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(81,24): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6742996Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/EventBus.cs(23,54): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6748978Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/EventBus.cs(38,63): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6754994Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(89,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6761553Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/EventBus.cs(54,64): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6767118Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/DependencyInjection/ServiceContainer.cs(32,50): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6774857Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(60,23): warning CS8618: Non-nullable property 'ModuleId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6781631Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/DependencyInjection/ServiceContainer.cs(40,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6787613Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/DependencyInjection/ServiceContainer.cs(46,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6795362Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(43,23): warning CS8618: Non-nullable property 'Executor' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6803474Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(45,23): warning CS8618: Non-nullable property 'Command' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6811470Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(26,23): warning CS8618: Non-nullable property 'Assistant' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6819614Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(28,23): warning CS8618: Non-nullable property 'Question' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6828056Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(30,23): warning CS8618: Non-nullable property 'Answer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6836442Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(32,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6844410Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(11,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6852153Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(13,23): warning CS8618: Non-nullable property 'From' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6859949Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/AuraEvents.cs(15,23): warning CS8618: Non-nullable property 'To' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6866968Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Logging/FileLogger.cs(19,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6872794Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Launchers/PythonLauncher.cs(47,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]

--- additional failure markers ---
build-apk	Build APK	2026-08-22T23:30:19.6420128Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Knowledge/KnowledgeManager.cs(28,52): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6454948Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Knowledge/KnowledgeManager.cs(28,75): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6477188Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Launchers/CellCommand.cs(12,64): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6503748Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Launchers/Runner.cs(61,32): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6534511Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Launchers/Runner.cs(62,35): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6542483Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Runtime/CellStore.cs(25,56): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6566585Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Runtime/PluginWatcher.cs(33,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6592721Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Runtime/SimulationRuntime.cs(123,73): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6612903Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Runtime/SimulationRuntime.cs(124,35): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6634338Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Runtime/SimulationRuntime.cs(124,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6661239Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6675143Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Modules' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6683847Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(7,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6692634Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(8,23): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6701460Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(9,23): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6709545Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(10,23): warning CS8618: Non-nullable property 'Status' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6718321Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Events/OrchestrationStepEvent.cs(11,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6725232Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(72,28): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
build-apk	Build APK	2026-08-22T23:30:19.6731208Z ##[warning]/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/Configuration/ConfigLoader.cs(76,24): warning CS8603: Possible null reference return. [/home/runner/work/AURA_assistente/AURA_assistente/src/AURA.Core/AURA.Core.csproj]
```
