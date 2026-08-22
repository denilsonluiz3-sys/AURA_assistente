# `aura_git.py` — Git + Python (essenciais AURA)

Script local para o workspace do agente. **Não faz push.**

## Requisitos

- `python3`
- `git` no PATH
- diretório com repositório Git (`git init` ou clone)

## Comandos

```bash
# Estado
python3 scripts/aura_git.py status
python3 scripts/aura_git.py --json status

# Relatório em reports/aura-snapshot-*.md
python3 scripts/aura_git.py snapshot --root /caminho/do/workspace

# Commit local de tudo que for seguro (sem .env / keystore / keys)
python3 scripts/aura_git.py commit -m "chore: estado do workspace" --all-safe

# Commit de arquivos específicos
python3 scripts/aura_git.py commit -m "docs: notas" -- memory-notes.md

# Desfazer alterações de um arquivo (volta ao HEAD)
python3 scripts/aura_git.py restore -- path/to/file.cs
```

## Segurança

Bloqueados automaticamente no `commit --all-safe`:

- `.env*`, `*.jks`, `*.keystore`, `*.pem`
- caminhos com `keystore`, `id_rsa`, `api_key`, `credentials`

## Integração com o agente

Via `ShellAgentTool` / Cell Program:

```text
python3 scripts/aura_git.py --json status
python3 scripts/aura_git.py snapshot
python3 scripts/aura_git.py commit -m "aura: snapshot" --all-safe
```

O agente deve tratar a saída JSON de `status` como fonte de verdade antes de propor commit.
