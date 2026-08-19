namespace AURA.Installer;

/// <summary>
/// Subconjunto curado dos módulos mais comuns da biblioteca padrão do
/// Python 3 — usado pra não sugerir "pip install os" por engano. Não é
/// exaustivo de propósito; se faltar algum módulo raro aqui, ele só vai
/// aparecer (sem grande prejuízo) na lista de dependências sugeridas, e dá
/// pra completar essa lista depois sem mexer no resto do analisador.
/// </summary>
public static class PythonStdlibModules
{
    public static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "os", "sys", "re", "io", "json", "math", "time", "random", "string",
        "subprocess", "pathlib", "typing", "collections", "itertools",
        "functools", "logging", "threading", "asyncio", "datetime",
        "argparse", "shutil", "glob", "csv", "sqlite3", "unittest", "socket",
        "http", "urllib", "copy", "enum", "dataclasses", "abc", "contextlib",
        "traceback", "pickle", "hashlib", "base64", "struct", "warnings",
        "platform", "tempfile", "uuid", "queue", "multiprocessing", "signal",
        "inspect", "importlib", "ast", "textwrap", "operator", "heapq",
        "bisect", "array", "weakref", "gc", "ctypes", "zlib", "gzip",
        "tarfile", "zipfile", "email", "html", "xml", "configparser",
        "getpass", "shlex", "statistics", "decimal", "fractions", "secrets",
        "types", "codecs", "locale", "calendar", "select", "ssl",
        "concurrent", "ipaddress", "dbm", "mimetypes", "cProfile", "profile",
        "pdb", "timeit", "venv", "pkgutil", "site", "sysconfig", "atexit",
        "keyword", "token", "tokenize", "dis", "symtable", "__future__"
    };

    public static bool IsStdlib(string rootModuleName) => Names.Contains(rootModuleName);
}
