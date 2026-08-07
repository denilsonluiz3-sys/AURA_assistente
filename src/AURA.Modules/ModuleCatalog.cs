using System;
using System.Collections.Generic;
using System.Linq;

namespace AURA.Modules
{
    /// <summary>
    /// Static catalog of the five optional AURA capability modules. Returns
    /// copies so callers cannot mutate the catalog.
    /// </summary>
    public static class ModuleCatalog
    {
        private static readonly List<ModuleInfo> Modules = new List<ModuleInfo>
        {
            new ModuleInfo
            {
                Id = "Windows",
                DisplayName = "Assistente Windows",
                Icon = "[W]",
                ShortDescription = "Automatiza tarefas do Windows: WMI, Registro, Serviços e PowerShell.",
                Includes = new List<string> { "WMI", "Registro", "Serviços", "PowerShell" },
                ImplementationSteps = new List<string>
                {
                    "Mapear os comandos de administração mais úteis",
                    "Integrar execução de PowerShell com saída capturada",
                    "Criar automações prontas (limpeza, otimização, diagnóstico)"
                },
                AcquiredCapabilities = new List<string>
                {
                    "Controle de serviços e processos",
                    "Automação de tarefas administrativas"
                },
                Difficulty = ModuleDifficulty.Avancado,
                EstimatedTime = "2 semanas",
                Status = ModuleStatus.Planejado
            },
            new ModuleInfo
            {
                Id = "AI",
                DisplayName = "IA",
                Icon = "[AI]",
                ShortDescription = "Assistente inteligente para conversar e resolver problemas com o sistema.",
                Includes = new List<string> { "Modelo de linguagem", "Chat local", "Contexto do sistema" },
                ImplementationSteps = new List<string>
                {
                    "Escolher e integrar um modelo de linguagem local",
                    "Conectar o chat ao diagnóstico do sistema",
                    "Treinar respostas para o perfil do usuário"
                },
                AcquiredCapabilities = new List<string>
                {
                    "Conversa natural em português",
                    "Sugestões baseadas no estado real do PC"
                },
                Difficulty = ModuleDifficulty.Intermediario,
                EstimatedTime = "3 semanas",
                Status = ModuleStatus.Implementado
            },
            new ModuleInfo
            {
                Id = "Automation",
                DisplayName = "Automação",
                Icon = "[A]",
                ShortDescription = "Cria rotinas e macros para repetir tarefas do dia a dia automaticamente.",
                Includes = new List<string> { "Rotinas", "Macros", "Agendador" },
                ImplementationSteps = new List<string>
                {
                    "Definir um formato de rotina por scripts",
                    "Criar o agendador de tarefas recorrentes",
                    "Adicionar gatilhos por evento do sistema"
                },
                AcquiredCapabilities = new List<string>
                {
                    "Execução automática de tarefas",
                    "Rotinas agendadas sem intervenção"
                },
                Difficulty = ModuleDifficulty.Intermediario,
                EstimatedTime = "2 semanas",
                Status = ModuleStatus.Planejado
            },
            new ModuleInfo
            {
                Id = "Memory",
                DisplayName = "Memória",
                Icon = "[M]",
                ShortDescription = "Guarda preferências e histórico para a AURA lembrar do contexto entre sessões.",
                Includes = new List<string> { "Preferências", "Histórico", "Perfil do usuário" },
                ImplementationSteps = new List<string>
                {
                    "Definir o formato persistente de memória",
                    "Integrar leitura/escrita nos fluxos da IA",
                    "Permitir editar e limpar a memória pelo usuário"
                },
                AcquiredCapabilities = new List<string>
                {
                    "Continuidade de contexto entre sessões",
                    "Perfil personalizado do usuário"
                },
                Difficulty = ModuleDifficulty.Basico,
                EstimatedTime = "1 semana",
                Status = ModuleStatus.Implementado
            },
            new ModuleInfo
            {
                Id = "Plugins",
                DisplayName = "Plugins",
                Icon = "[P]",
                ShortDescription = "Permite estender a AURA com novos recursos desenvolvidos pela comunidade.",
                Includes = new List<string> { "Carregador de plugins", "API de extensão", "Repositório" },
                ImplementationSteps = new List<string>
                {
                    "Definir a API pública de plugins (IPlugin)",
                    "Implementar o carregamento dinâmico de assemblies",
                    "Criar um repositório local de plugins instaláveis"
                },
                AcquiredCapabilities = new List<string>
                {
                    "Extensibilidade pela comunidade",
                    "Instalação de recursos sem recompilar a AURA"
                },
                Difficulty = ModuleDifficulty.Avancado,
                EstimatedTime = "4 semanas",
                Status = ModuleStatus.Implementado
            }
        };

        public static List<ModuleInfo> GetAll()
        {
            return Modules.ToList();
        }

        public static ModuleInfo GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return Modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
