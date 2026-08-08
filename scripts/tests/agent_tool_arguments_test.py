import json

def normalize_path(value):
    if value is None:
        return "."

    if isinstance(value, dict):
        # Modelo confundiu schema com valor.
        value = value.get("description", ".")

    if not isinstance(value, str):
        return "."

    value = value.strip()

    if not value:
        return "."

    if value in ("./workspace", "workspace", "./.aura/workspace"):
        return "."

    if value.startswith("./workspace/"):
        return value[len("./workspace/"):]

    if value.startswith("workspace/"):
        return value[len("workspace/"):]

    return value

tests = [
    (None, "."),
    ("", "."),
    (".", "."),
    ("./workspace", "."),
    ("workspace", "."),
    ("teste.txt", "teste.txt"),
    ("./teste.txt", "./teste.txt"),
    ("workspace/teste.txt", "teste.txt"),
    (
        {
            "type": "string",
            "description": "Caminho relativo ao workspace."
        },
        "Caminho relativo ao workspace."
    ),
]

print("=== NORMALIZAÇÃO DE PATH ===")

for value, expected in tests:
    result = normalize_path(value)

    print("entrada :", repr(value))
    print("saída   :", repr(result))

    if result != expected:
        raise SystemExit(
            "FALHA: esperado %r, obtido %r"
            % (expected, result)
        )

print("PATH TEST: OK")
