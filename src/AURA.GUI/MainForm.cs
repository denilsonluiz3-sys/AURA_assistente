using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AURA.Core.Bootstrap;
using AURA.Modules;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.GUI
{
    /// <summary>
    /// The AURA Genesis Core main window. A menu on the left/top drives
    /// which view is shown in the content panel: diagnostics, network
    /// status, the module catalog, settings and history.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly AuraBootstrap _bootstrap;
        private readonly SystemAnalyzer _systemAnalyzer;
        private readonly NetworkManager _networkManager;
        private readonly List<string> _activityLog;

        public MainForm(AuraBootstrap bootstrap)
        {
            _bootstrap = bootstrap ?? throw new ArgumentNullException("bootstrap");
            _systemAnalyzer = new SystemAnalyzer();
            _networkManager = new NetworkManager();
            _activityLog = new List<string>();

            InitializeComponent();
            WireMenuEvents();

            Load += (s, e) => ShowWelcome();
        }

        private void WireMenuEvents()
        {
            _menuDiagnostico.Click += (s, e) => ShowDiagnostics();
            _menuInternet.Click += (s, e) => ShowNetwork();
            _menuModulos.Click += (s, e) => ShowModules();
            _menuConfiguracoes.Click += (s, e) => ShowSettings();
            _menuHistorico.Click += (s, e) => ShowHistory();
            _menuAjuda.Click += (s, e) => ShowHelp();
        }

        private void Log(string message)
        {
            _activityLog.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            _bootstrap.Logger.Info(message);
            _statusLabel.Text = message;
        }

        private Panel ResetContent()
        {
            _contentPanel.Controls.Clear();
            var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _contentPanel.Controls.Add(host);
            return host;
        }

        // ---------------------------------------------------------------
        // Welcome / first-run sequence
        // ---------------------------------------------------------------

        private void ShowWelcome()
        {
            Panel host = ResetContent();

            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10F)
            };
            host.Controls.Add(box);

            box.AppendText("================================\n");
            box.AppendText(" AURA GENESIS CORE\n");
            box.AppendText("================================\n\n");
            box.AppendText("Bem-vindo ao assistente AURA.\n");
            box.AppendText("Vou ajudá-lo a configurar e evoluir seu sistema.\n\n");
            box.AppendText("Verificando ambiente...\n");
            Application.DoEvents();

            SystemDiagnosticsResult diagnostics = _systemAnalyzer.Analyze();
            box.AppendText("✓ Sistema operacional (" + diagnostics.OperatingSystem + ")\n");
            box.AppendText("✓ Processador (" + diagnostics.ProcessorCount + " núcleos)\n");
            box.AppendText("✓ Memória (" + diagnostics.AvailableMemoryGb + " GB livres de " + diagnostics.TotalMemoryGb + " GB)\n");
            box.AppendText("✓ Espaço em disco (" + diagnostics.FreeDiskSpaceGb + " GB livres em " + diagnostics.SystemDrive + ")\n");

            NetworkStatus network = _networkManager.CheckConnection();
            box.AppendText((network.HasInternetAccess ? "✓" : "✗") + " Conexão Internet\n\n");

            if (!diagnostics.MeetsMinimumRequirements)
            {
                box.SelectionColor = Color.Firebrick;
                box.AppendText("Atenção: esta versão do Windows está abaixo do mínimo suportado (Windows 7 SP1).\n\n");
                box.SelectionColor = box.ForeColor;
            }

            box.AppendText("Ambiente preparado.\n");
            box.AppendText("Próximo passo: escolha, no menu AURA, quais capacidades deseja adicionar.\n");

            Log("Ambiente verificado.");
        }

        // ---------------------------------------------------------------
        // Diagnóstico do Computador
        // ---------------------------------------------------------------

        private void ShowDiagnostics()
        {
            Panel host = ResetContent();
            SystemDiagnosticsResult diagnostics = _systemAnalyzer.Analyze();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddRow(layout, "Sistema operacional:", diagnostics.OperatingSystem);
            AddRow(layout, "Arquitetura:", diagnostics.Architecture);
            AddRow(layout, "Processador (núcleos lógicos):", diagnostics.ProcessorCount.ToString());
            AddRow(layout, "Memória total:", diagnostics.TotalMemoryGb + " GB");
            AddRow(layout, "Memória disponível:", diagnostics.AvailableMemoryGb + " GB");
            AddRow(layout, "Unidade do sistema:", diagnostics.SystemDrive);
            AddRow(layout, "Espaço livre em disco:", diagnostics.FreeDiskSpaceGb + " GB de " + diagnostics.TotalDiskSpaceGb + " GB");
            AddRow(layout, "Requisitos mínimos atendidos:", diagnostics.MeetsMinimumRequirements ? "Sim" : "Não");

            host.Controls.Add(layout);
            Log("Diagnóstico do computador exibido.");
        }

        private static void AddRow(TableLayoutPanel layout, string label, string value)
        {
            int row = layout.RowCount;
            layout.RowCount = row + 1;
            layout.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = true, Margin = new Padding(3, 6, 12, 6) }, 0, row);
            layout.Controls.Add(new Label { Text = value, AutoSize = true, Margin = new Padding(3, 6, 3, 6) }, 1, row);
        }

        // ---------------------------------------------------------------
        // Conexão Internet
        // ---------------------------------------------------------------

        private void ShowNetwork()
        {
            Panel host = ResetContent();
            NetworkStatus status = _networkManager.CheckConnection();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddRow(layout, "Rede local ativa:", status.IsConnected ? "Sim" : "Não");
            AddRow(layout, "Acesso à Internet:", status.HasInternetAccess ? "Sim" : "Não");
            AddRow(layout, "Endereço IP local:", status.LocalIpAddress);
            AddRow(layout, "Latência:", status.LatencyMilliseconds.HasValue ? status.LatencyMilliseconds + " ms" : "-");
            AddRow(layout, "Status:", status.Message);

            var refreshButton = new Button { Text = "Testar novamente", AutoSize = true, Margin = new Padding(3, 12, 3, 3) };
            refreshButton.Click += (s, e) => ShowNetwork();

            host.Controls.Add(layout);
            host.Controls.Add(refreshButton);
            refreshButton.Top = layout.Bottom + 8;

            Log("Status de conexão verificado.");
        }

        // ---------------------------------------------------------------
        // Módulos Disponíveis
        // ---------------------------------------------------------------

        private void ShowModules()
        {
            Panel host = ResetContent();

            var intro = new Label
            {
                Text = "Escolha os recursos que deseja implementar",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            foreach (ModuleInfo module in ModuleCatalog.GetAll())
            {
                flow.Controls.Add(BuildModuleCard(module));
            }

            host.Controls.Add(flow);
            host.Controls.Add(intro);
            flow.Top = intro.Bottom + 8;

            Log("Catálogo de módulos exibido.");
        }

        private Panel BuildModuleCard(ModuleInfo module)
        {
            var card = new Panel
            {
                Width = 250,
                Height = 190,
                Margin = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            var title = new Label
            {
                Text = module.Icon + "  " + module.DisplayName,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 8)
            };

            var description = new Label
            {
                Text = module.ShortDescription,
                AutoSize = false,
                Size = new Size(225, 60),
                Location = new Point(10, 36)
            };

            var includes = new Label
            {
                Text = "Inclui: " + string.Join(", ", module.Includes),
                AutoSize = false,
                Size = new Size(225, 48),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 96)
            };

            var button = new Button
            {
                Text = "Conhecer recurso",
                Location = new Point(10, 150),
                AutoSize = true
            };
            button.Click += (s, e) => OpenModuleDetail(module);

            card.Controls.Add(title);
            card.Controls.Add(description);
            card.Controls.Add(includes);
            card.Controls.Add(button);

            return card;
        }

        private void OpenModuleDetail(ModuleInfo module)
        {
            using (var detail = new ModuleDetailForm(module, IsModuleEnabled(module.Id)))
            {
                if (detail.ShowDialog(this) == DialogResult.Yes)
                {
                    SetModuleEnabled(module.Id, true);
                    Log("Módulo preparado: " + module.DisplayName);
                    MessageBox.Show(
                        this,
                        module.DisplayName + " foi marcado para implementação em uma próxima versão do AURA.",
                        "AURA Genesis Core",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private bool IsModuleEnabled(string moduleId)
        {
            switch (moduleId)
            {
                case "Windows": return _bootstrap.Modules.Modules.Windows;
                case "AI": return _bootstrap.Modules.Modules.AI;
                case "Automation": return _bootstrap.Modules.Modules.Automation;
                case "Memory": return _bootstrap.Modules.Modules.Memory;
                case "Plugins": return _bootstrap.Modules.Modules.Plugins;
                default: return false;
            }
        }

        private void SetModuleEnabled(string moduleId, bool enabled)
        {
            switch (moduleId)
            {
                case "Windows": _bootstrap.Modules.Modules.Windows = enabled; break;
                case "AI": _bootstrap.Modules.Modules.AI = enabled; break;
                case "Automation": _bootstrap.Modules.Modules.Automation = enabled; break;
                case "Memory": _bootstrap.Modules.Modules.Memory = enabled; break;
                case "Plugins": _bootstrap.Modules.Modules.Plugins = enabled; break;
            }

            _bootstrap.SaveModules();
        }

        // ---------------------------------------------------------------
        // Configurações
        // ---------------------------------------------------------------

        private void ShowSettings()
        {
            Panel host = ResetContent();

            var internetCheck = new CheckBox
            {
                Text = "Habilitar verificação de Internet ao iniciar",
                Checked = _bootstrap.Settings.Internet,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var themeLabel = new Label { Text = "Tema:", AutoSize = true, Location = new Point(0, 32) };
            var themeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(50, 28),
                Width = 120
            };
            themeCombo.Items.AddRange(new object[] { "Light", "Dark" });
            themeCombo.SelectedItem = string.IsNullOrEmpty(_bootstrap.Settings.Theme) ? "Light" : _bootstrap.Settings.Theme;

            var saveButton = new Button { Text = "Salvar", Location = new Point(0, 68), AutoSize = true };
            saveButton.Click += (s, e) =>
            {
                _bootstrap.Settings.Internet = internetCheck.Checked;
                _bootstrap.Settings.Theme = themeCombo.SelectedItem?.ToString() ?? "Light";
                _bootstrap.SaveSettings();
                Log("Configurações salvas.");
                MessageBox.Show(this, "Configurações salvas com sucesso.", "AURA Genesis Core",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            host.Controls.Add(internetCheck);
            host.Controls.Add(themeLabel);
            host.Controls.Add(themeCombo);
            host.Controls.Add(saveButton);

            Log("Tela de configurações exibida.");
        }

        // ---------------------------------------------------------------
        // Histórico
        // ---------------------------------------------------------------

        private void ShowHistory()
        {
            Panel host = ResetContent();

            var list = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.5F)
            };

            if (_activityLog.Count == 0)
            {
                list.Items.Add("Nenhuma atividade registrada ainda nesta sessão.");
            }
            else
            {
                foreach (string entry in _activityLog)
                {
                    list.Items.Add(entry);
                }
            }

            host.Controls.Add(list);
        }

        // ---------------------------------------------------------------
        // Ajuda
        // ---------------------------------------------------------------

        private void ShowHelp()
        {
            MessageBox.Show(
                this,
                "AURA Genesis Core é a base modular do assistente AURA.\n\n" +
                "Use o menu AURA para verificar seu computador, checar a conexão " +
                "com a Internet e escolher quais capacidades futuras deseja preparar. " +
                "Nenhuma funcionalidade avançada é instalada nesta versão - o objetivo " +
                "é construir uma base sólida e expansível.",
                "Ajuda - AURA Genesis Core",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
