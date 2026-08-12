import ollama
import os
import datetime
import re
import json

# ============================================================
# CONFIGURAÇÃO
# ============================================================

MODEL_PRINCIPAL = "qwen2.5-coder:7b"
MODEL_LEVE = "phi3:3.8b"

PASTA_PROJETO = "aura_workspace"
PASTA_LOGS = os.path.join(PASTA_PROJETO, "logs")
PASTA_CODIGO = os.path.join(PASTA_PROJETO, "codigo")
ARQUIVO_MEMORIA = os.path.join(PASTA_PROJETO, "memoria.json")

MAX_HISTORICO = 10


# ============================================================
# AURA
# ============================================================

class AURA:

    def __init__(self):

        os.makedirs(PASTA_PROJETO, exist_ok=True)
        os.makedirs(PASTA_LOGS, exist_ok=True)
        os.makedirs(PASTA_CODIGO, exist_ok=True)

        self.memoria = self.carregar_memoria()
        self.historico = []

        self.modelo_atual = MODEL_PRINCIPAL

        print("=" * 60)
        print("AURA ONLINE")
        print("=" * 60)
        print(f"Modelo principal: {MODEL_PRINCIPAL}")
        print(f"Modelo fallback:  {MODEL_LEVE}")
        print(f"Workspace:        {os.path.abspath(PASTA_PROJETO)}")
        print("=" * 60)


    # ========================================================
    # MEMÓRIA
    # ========================================================

    def carregar_memoria(self):

        if not os.path.exists(ARQUIVO_MEMORIA):
            return {}

        try:
            with open(
                ARQUIVO_MEMORIA,
                "r",
                encoding="utf-8"
            ) as f:

                return json.load(f)

        except Exception:
            return {}


    def salvar_memoria(self):

        with open(
            ARQUIVO_MEMORIA,
            "w",
            encoding="utf-8"
        ) as f:

            json.dump(
                self.memoria,
                f,
                indent=2,
                ensure_ascii=False
            )


    def lembrar(self, chave, valor):

        self.memoria[chave] = valor
        self.salvar_memoria()


    # ========================================================
    # CHAT COM OLLAMA
    # ========================================================

    def perguntar(
        self,
        prompt,
        system=None
    ):

        if system is None:

            system = (
                "Você é a AURA, uma assistente de IA "
                "especialista em programação, arquitetura "
                "de software, automação e desenvolvimento."
            )

        mensagens = [
            {
                "role": "system",
                "content": system
            }
        ]

        # Histórico recente
        mensagens.extend(
            self.historico[-MAX_HISTORICO:]
        )

        mensagens.append(
            {
                "role": "user",
                "content": prompt
            }
        )

        try:

            response = ollama.chat(
                model=self.modelo_atual,
                messages=mensagens
            )

        except Exception as erro:

            print(
                f"\n[ERRO] Modelo principal falhou:"
                f"\n{erro}"
            )

            print(
                "\n[AVISO] Tentando modelo fallback..."
            )

            try:

                self.modelo_atual = MODEL_LEVE

                response = ollama.chat(
                    model=MODEL_LEVE,
                    messages=mensagens
                )

            except Exception as erro2:

                return (
                    "[ERRO] Não foi possível "
                    "consultar nenhum modelo Ollama.\n\n"
                    f"Principal: {erro}\n"
                    f"Fallback: {erro2}"
                )

        resposta = response["message"]["content"]

        # Restaurar modelo principal para próxima chamada
        self.modelo_atual = MODEL_PRINCIPAL

        # Atualizar histórico
        self.historico.append(
            {
                "role": "user",
                "content": prompt
            }
        )

        self.historico.append(
            {
                "role": "assistant",
                "content": resposta
            }
        )

        self.salvar_log(prompt, resposta)

        return resposta


    # ========================================================
    # PLANEJAMENTO
    # ========================================================

    def planejar(self, objetivo):

        prompt = f"""
Objetivo do usuário:

{objetivo}

Crie um plano de desenvolvimento com exatamente
5 etapas numeradas.

Formato obrigatório:

1. ...
2. ...
3. ...
4. ...
5. ...

Cada etapa deve ser objetiva e executável.
"""

        print("\n[AURA] Planejando projeto...")

        return self.perguntar(
            prompt,
            system=(
                "Você é um arquiteto de software sênior. "
                "Crie planos técnicos claros, realistas "
                "e organizados."
            )
        )


    # ========================================================
    # EXTRAIR ETAPAS
    # ========================================================

    def extrair_etapas(self, plano):

        linhas = plano.splitlines()

        etapas = []

        for linha in linhas:

            linha = linha.strip()

            if re.match(r"^\d+[\.\)]\s+", linha):

                etapas.append(linha)

        return etapas


    # ========================================================
    # GERAR CÓDIGO
    # ========================================================

    def codar(self, tarefa):

        prompt = f"""
Tarefa:

{tarefa}

Escreva uma implementação Python completa.

Regras:

- código funcional
- código limpo
- comentários somente quando necessários
- não invente bibliotecas
- trate erros importantes
- retorne SOMENTE o código Python
- não use ```python
- não use ``` 
"""

        print("\n[AURA] Codando...")

        codigo = self.perguntar(
            prompt,
            system=(
                "Você é um programador Python sênior. "
                "Retorne somente código Python válido."
            )
        )

        return self.limpar_codigo(codigo)


    # ========================================================
    # LIMPAR MARKDOWN
    # ========================================================

    def limpar_codigo(self, codigo):

        codigo = codigo.strip()

        codigo = re.sub(
            r"^```python\s*",
            "",
            codigo,
            flags=re.IGNORECASE
        )

        codigo = re.sub(
            r"^```\s*",
            "",
            codigo
        )

        codigo = re.sub(
            r"\s*```$",
            "",
            codigo
        )

        return codigo.strip()


    # ========================================================
    # SALVAR CÓDIGO
    # ========================================================

    def salvar_codigo(
        self,
        nome_arquivo,
        codigo
    ):

        caminho = os.path.join(
            PASTA_CODIGO,
            nome_arquivo
        )

        with open(
            caminho,
            "w",
            encoding="utf-8"
        ) as f:

            f.write(codigo)

        print(
            f"\n[CÓDIGO SALVO]"
            f"\n{os.path.abspath(caminho)}"
        )

        return caminho


    # ========================================================
    # LOG
    # ========================================================

    def salvar_log(
        self,
        prompt,
        resposta
    ):

        data = datetime.datetime.now().strftime(
            "%Y-%m-%d_%H-%M-%S"
        )

        caminho = os.path.join(
            PASTA_LOGS,
            f"log_{data}.txt"
        )

        with open(
            caminho,
            "w",
            encoding="utf-8"
        ) as f:

            f.write(
                "PROMPT:\n"
            )

            f.write(prompt)

            f.write(
                "\n\nRESPOSTA:\n"
            )

            f.write(resposta)


    # ========================================================
    # CRIAR PROJETO
    # ========================================================

    def criar_projeto(self, objetivo):

        plano = self.planejar(objetivo)

        print(
            "\n"
            + "=" * 60
        )

        print("PLANO DA AURA")

        print("=" * 60)

        print(plano)

        etapas = self.extrair_etapas(plano)

        if not etapas:

            print(
                "\n[ERRO] Não consegui identificar "
                "as etapas do plano."
            )

            return

        print(
            f"\n[AURA] {len(etapas)} etapas encontradas."
        )

        # Executar todas as etapas
        for indice, etapa in enumerate(
            etapas,
            start=1
        ):

            print(
                "\n"
                + "=" * 60
            )

            print(
                f"ETAPA {indice}/{len(etapas)}"
            )

            print("=" * 60)

            print(etapa)

            codigo = self.codar(etapa)

            nome = f"etapa_{indice}.py"

            self.salvar_codigo(
                nome,
                codigo
            )

            print(
                f"\n[CÓDIGO DA ETAPA {indice}]\n"
            )

            print(codigo)

        print(
            "\n"
            + "=" * 60
        )

        print("PROJETO CONCLUÍDO")

        print("=" * 60)


# ============================================================
# INTERFACE
# ============================================================

def main():

    aura = AURA()

    while True:

        try:

            user_input = input(
                "\n[Você] > "
            ).strip()

        except KeyboardInterrupt:

            print("\n\nAURA encerrada.")
            break

        if not user_input:
            continue

        if user_input.lower() in [
            "sair",
            "exit",
            "quit"
        ]:

            print("\nAURA encerrada.")
            break

        # Criar projeto
        if any(
            palavra in user_input.lower()
            for palavra in [
                "criar",
                "fazer",
                "desenvolver",
                "construir"
            ]
        ):

            aura.criar_projeto(
                user_input
            )

        else:

            resposta = aura.perguntar(
                user_input
            )

            print(
                "\n[AURA]\n"
            )

            print(resposta)


# ============================================================
# START
# ============================================================

if __name__ == "__main__":
    main()
