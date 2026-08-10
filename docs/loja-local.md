# Loja local — uso e políticas

Resumo mínimo para desenvolvedores e CI sobre a loja local (`~/AURA/loja`).

- Estrutura esperada:

```
~/AURA/loja/
  <module-id>/
    manifest.json      # id + payloadFiles
    payload/
      <arquivos .dll>
```

- Comportamento:
  - `LojaLocalResolver.InstallFromLoja(id)` valida que `ModuleCatalog.GetById(id)` existe **antes** de copiar qualquer arquivo.
  - Copia os arquivos listados em `manifest.json` para `~/AURA/plugins/` (pasta flat).
  - Gera `~/AURA/packages/<id>/module.json` a partir do `ModuleCatalog` (schema real).
  - Não habilita o módulo; chame `ModuleManager.Apply(id)` para habilitar.

- Segurança e política default:
  - Rejeita `payloadFiles` com path traversal ou separadores — apenas nomes simples permitidos.
  - Política de colisão default = FAIL: se um arquivo destino já existir, a instalação falhará; use `overwrite=true` para forçar.
  - Registro de arquivos instalados: `packages/<id>/installedFiles.json` é escrito para facilitar uninstall/cleanup.

- Testes:
  - Unitários cobrem validação, cópia e geração de `module.json`.
  - Integração leve valida que arquivos chegam em `~/AURA/plugins/` e que `module.json` foi gerado.

