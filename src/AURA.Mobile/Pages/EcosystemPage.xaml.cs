using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace AURA.Mobile.Pages
{
    public partial class EcosystemPage : ContentPage
    {
        public List<ModuleInfo> Modules { get; set; }

        public EcosystemPage()
        {
            InitializeComponent();
            
            Modules = new List<ModuleInfo>
            {
                new ModuleInfo { Name = "Chat", Icon = "💬", Description = "Interface de conversa com IA" },
                new ModuleInfo { Name = "Agente", Icon = "🧠", Description = "Orquestrador de decisões e agentes" },
                new ModuleInfo { Name = "Células", Icon = "📊", Description = "Gerenciamento de células autônomas" },
                new ModuleInfo { Name = "Runtime", Icon = "⚡", Description = "Execução de comandos e scripts" },
                new ModuleInfo { Name = "Shell", Icon = "💻", Description = "Terminal integrado ao sistema" },
                new ModuleInfo { Name = "Processo", Icon = "⚖️", Description = "Gerenciamento de processos jurídicos" }
            };
            
            BindingContext = this;
        }
    }

    public class ModuleInfo
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
