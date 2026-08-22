#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AURA — Git workspace helper (essenciais)

Comandos:
  status     Estado do repositório (branch, dirty, resumo)
  snapshot   Gera reports/aura-snapshot-YYYYMMDD-HHMMSS.md
  commit     Commit local seletivo (nunca push)
  restore    Restaura arquivo(s) do último commit (git restore)

Política de segurança (alinhada ao PolicyGuard):
  - Só opera dentro de --root (default: cwd)
  - Nunca faz push / force-push / remote set-url
  - Nunca adiciona segredos óbvios (chaves, keystore, .env)
  - Commit exige mensagem explícita

Uso:
  python3 scripts/aura_git.py status
  python3 scripts/aura_git.py snapshot --root /caminho/workspace
  python3 scripts/aura_git.py commit -m "chore: snapshot local" --all-safe
  python3 scripts/aura_git.py restore -- path/to/file
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import List, Optional, Sequence, Tuple

# Extensões / nomes que nunca entram em commit automático
BLOCKED_NAME_PATTERNS = (
    re.compile(r"(^|/)\.env($|\.)", re.I),
    re.compile(r"(^|/)\.env\..+$", re.I),
    re.compile(r"\.jks$", re.I),
    re.compile(r"\.keystore$", re.I),
    re.compile(r"keystore", re.I),
    re.compile(r"id_rsa", re.I),
    re.compile(r"id_ed25519", re.I),
    re.compile(r"\.pem$", re.I),
    re.compile(r"secrets?\.(json|yml|yaml|txt)$", re.I),
    re.compile(r"api[_-]?key", re.I),
    re.compile(r"credentials", re.I),
)

BLOCKED_PATH_FRAGMENTS = (
    "/keystore/",
    "\\keystore\\",
    "/.ssh/",
    "\\.ssh\\",
)


class AuraGitError(Exception):
    pass


def run_git(root: Path, args: Sequence[str], check: bool = True) -> subprocess.CompletedProcess:
    cmd = ["git", "-C", str(root), *args]
    try:
        cp = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
    except FileNotFoundError as exc:
        raise AuraGitError(
            "git não encontrado no PATH. Instale o Git ou use um ambiente com git."
        ) from exc

    if check and cp.returncode != 0:
        err = (cp.stderr or cp.stdout or "").strip()
        raise AuraGitError(f"git {' '.join(args)} falhou (code={cp.returncode}): {err}")
    return cp


def ensure_git_repo(root: Path) -> None:
    if not root.is_dir():
        raise AuraGitError(f"--root não é diretório: {root}")
    cp = run_git(root, ["rev-parse", "--is-inside-work-tree"], check=False)
    if cp.returncode != 0 or (cp.stdout or "").strip() != "true":
        raise AuraGitError(
            f"'{root}' não é um repositório Git. "
            "Inicialize com: git init  (ou aponte --root para o clone certo)."
        )


def is_blocked(path: str) -> bool:
    norm = path.replace("\\", "/")
    low = "/" + norm.lower()
    for frag in BLOCKED_PATH_FRAGMENTS:
        if frag.replace("\\", "/") in low:
            return True
    for pat in BLOCKED_NAME_PATTERNS:
        if pat.search(norm):
            return True
    return False


def porcelain_status(root: Path) -> List[Tuple[str, str]]:
    """Lista (code, path) do git status --porcelain."""
    cp = run_git(root, ["status", "--porcelain", "-uall"])
    rows: List[Tuple[str, str]] = []
    for line in (cp.stdout or "").splitlines():
        if not line.strip():
            continue
        # XY PATH  ou  XY PATH -> PATH2
        code = line[:2]
        rest = line[3:] if len(line) > 3 else ""
        if " -> " in rest:
            rest = rest.split(" -> ", 1)[-1]
        rest = rest.strip().strip('"')
        rows.append((code, rest))
    return rows


def branch_name(root: Path) -> str:
    cp = run_git(root, ["rev-parse", "--abbrev-ref", "HEAD"], check=False)
    if cp.returncode != 0:
        return "(unknown)"
    name = (cp.stdout or "").strip()
    return name or "(detached)"


def short_head(root: Path) -> str:
    cp = run_git(root, ["rev-parse", "--short", "HEAD"], check=False)
    if cp.returncode != 0:
        return "(none)"
    return (cp.stdout or "").strip() or "(none)"


def cmd_status(root: Path, as_json: bool) -> int:
    ensure_git_repo(root)
    rows = porcelain_status(root)
    blocked = [(c, p) for c, p in rows if is_blocked(p)]
    safe = [(c, p) for c, p in rows if not is_blocked(p)]

    payload = {
        "root": str(root.resolve()),
        "branch": branch_name(root),
        "head": short_head(root),
        "dirty": len(rows) > 0,
        "changed": len(rows),
        "safe_to_commit": len(safe),
        "blocked": [{"code": c, "path": p} for c, p in blocked],
        "files": [{"code": c, "path": p, "blocked": is_blocked(p)} for c, p in rows],
    }

    if as_json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    print(f"root:   {payload['root']}")
    print(f"branch: {payload['branch']} @ {payload['head']}")
    print(f"dirty:  {payload['dirty']}  ({payload['changed']} path(s), "
          f"{payload['safe_to_commit']} safe)")
    if not rows:
        print("(working tree limpa)")
        return 0
    print("\nArquivos:")
    for c, p in rows:
        tag = " [BLOCKED]" if is_blocked(p) else ""
        print(f"  {c} {p}{tag}")
    return 0


def read_memory_tail(root: Path, limit: int = 8) -> List[str]:
    """Tenta resumir memory.json se existir (AURA MemoryStore)."""
    candidates = [
        root / "memory.json",
        root / "AURA" / "memory.json",
        Path.home() / "AURA" / "memory.json",
    ]
    path = next((p for p in candidates if p.is_file()), None)
    if path is None:
        return []

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return [f"(memory.json ilegível em {path})"]

    entries = data.get("Entries") or data.get("entries") or []
    if not isinstance(entries, list):
        return []

    lines: List[str] = [f"Memória: {path} ({len(entries)} registro(s))"]
    for e in entries[-limit:]:
        if not isinstance(e, dict):
            continue
        role = e.get("Role") or e.get("role") or "?"
        text = e.get("Text") or e.get("text") or ""
        text = str(text).replace("\n", " ").strip()
        if len(text) > 120:
            text = text[:120] + "…"
        lines.append(f"- [{role}] {text}")
    return lines


def cmd_snapshot(root: Path, out_dir: Optional[Path]) -> int:
    ensure_git_repo(root)
    rows = porcelain_status(root)
    stamp = dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    reports = out_dir or (root / "reports")
    reports.mkdir(parents=True, exist_ok=True)
    out = reports / f"aura-snapshot-{stamp}.md"

    log = run_git(root, ["log", "-5", "--oneline"], check=False)
    recent = (log.stdout or "").strip()

    lines = [
        f"# AURA snapshot — {stamp}",
        "",
        f"- root: `{root.resolve()}`",
        f"- branch: `{branch_name(root)}`",
        f"- HEAD: `{short_head(root)}`",
        f"- dirty: **{len(rows) > 0}** ({len(rows)} path(s))",
        "",
        "## Status",
        "",
        "```",
    ]
    if rows:
        for c, p in rows:
            tag = " BLOCKED" if is_blocked(p) else ""
            lines.append(f"{c} {p}{tag}")
    else:
        lines.append("(clean)")
    lines.append("```")
    lines.append("")
    lines.append("## Últimos commits")
    lines.append("")
    lines.append("```")
    lines.append(recent or "(sem histórico)")
    lines.append("```")
    lines.append("")

    mem = read_memory_tail(root)
    if mem:
        lines.append("## Memória (tail)")
        lines.append("")
        lines.extend(mem)
        lines.append("")

    lines.append("---")
    lines.append("_Gerado por `scripts/aura_git.py snapshot` — sem push._")
    lines.append("")

    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"snapshot: {out}")
    return 0


def cmd_commit(root: Path, message: str, all_safe: bool, paths: List[str]) -> int:
    ensure_git_repo(root)
    message = (message or "").strip()
    if not message:
        raise AuraGitError("commit exige -m / --message")

    if all_safe:
        rows = porcelain_status(root)
        to_add = [p for _, p in rows if p and not is_blocked(p)]
    else:
        to_add = list(paths)

    if not to_add:
        print("nada para commitar (lista vazia ou só arquivos bloqueados)")
        return 0

    blocked_requested = [p for p in to_add if is_blocked(p)]
    if blocked_requested:
        raise AuraGitError(
            "recusado: caminhos bloqueados (segredo/keystore/env):\n  - "
            + "\n  - ".join(blocked_requested)
        )

    # stage
    run_git(root, ["add", "--", *to_add])

    # se nada staged, sair limpo
    staged = run_git(root, ["diff", "--cached", "--name-only"])
    staged_files = [ln for ln in (staged.stdout or "").splitlines() if ln.strip()]
    if not staged_files:
        print("nada staged após filtro — commit cancelado")
        return 0

    run_git(root, ["commit", "-m", message])
    print(f"commit ok @ {short_head(root)} — {len(staged_files)} arquivo(s)")
    for f in staged_files:
        print(f"  + {f}")
    print("(local only — nenhum push foi feito)")
    return 0


def cmd_restore(root: Path, paths: List[str]) -> int:
    ensure_git_repo(root)
    if not paths:
        raise AuraGitError("restore exige pelo menos um caminho após --")

    for p in paths:
        if is_blocked(p):
            # restore de blocked ainda pode ser útil, mas avisamos
            print(f"aviso: restaurando caminho sensível: {p}", file=sys.stderr)

    run_git(root, ["restore", "--source=HEAD", "--worktree", "--", *paths])
    print(f"restore ok: {', '.join(paths)}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="aura_git.py",
        description="AURA Git helper — status / snapshot / commit local / restore",
    )
    p.add_argument(
        "--root",
        default=".",
        help="raiz do workspace Git (default: diretório atual)",
    )
    p.add_argument(
        "--json",
        action="store_true",
        help="saída JSON (útil para o agente)",
    )

    sub = p.add_subparsers(dest="cmd", required=True)

    sub.add_parser("status", help="estado do working tree")

    sp = sub.add_parser("snapshot", help="gera relatório markdown em reports/")
    sp.add_argument(
        "--out-dir",
        default=None,
        help="pasta de saída (default: <root>/reports)",
    )

    cp = sub.add_parser("commit", help="commit local (sem push)")
    cp.add_argument("-m", "--message", required=True, help="mensagem do commit")
    cp.add_argument(
        "--all-safe",
        action="store_true",
        help="adiciona todos os arquivos não bloqueados",
    )
    cp.add_argument(
        "paths",
        nargs="*",
        help="caminhos específicos (se não usar --all-safe)",
    )

    rp = sub.add_parser("restore", help="git restore de caminhos a partir de HEAD")
    rp.add_argument("paths", nargs="+", help="arquivos a restaurar")

    return p


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    root = Path(args.root).expanduser().resolve()

    try:
        if args.cmd == "status":
            return cmd_status(root, as_json=args.json)
        if args.cmd == "snapshot":
            out = Path(args.out_dir).expanduser() if args.out_dir else None
            return cmd_snapshot(root, out)
        if args.cmd == "commit":
            return cmd_commit(
                root,
                message=args.message,
                all_safe=args.all_safe,
                paths=list(args.paths or []),
            )
        if args.cmd == "restore":
            return cmd_restore(root, list(args.paths))
        parser.error(f"comando desconhecido: {args.cmd}")
        return 2
    except AuraGitError as exc:
        print(f"erro: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
