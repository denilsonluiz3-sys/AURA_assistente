using System.Drawing;
using System.Windows.Forms;

namespace AURA.GUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private MenuStrip _menuStrip;
        private ToolStripMenuItem _menuAura;
        private ToolStripMenuItem _menuDiagnostico;
        private ToolStripMenuItem _menuInternet;
        private ToolStripMenuItem _menuModulos;
        private ToolStripMenuItem _menuConfiguracoes;
        private ToolStripMenuItem _menuHistorico;
        private ToolStripMenuItem _menuAjuda;

        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _subtitleLabel;

        private Panel _contentPanel;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _menuStrip = new MenuStrip();
            _menuAura = new ToolStripMenuItem("AURA");
            _menuDiagnostico = new ToolStripMenuItem("Diagnóstico do Computador");
            _menuInternet = new ToolStripMenuItem("Conexão Internet");
            _menuModulos = new ToolStripMenuItem("Módulos Disponíveis");
            _menuConfiguracoes = new ToolStripMenuItem("Configurações");
            _menuHistorico = new ToolStripMenuItem("Histórico");
            _menuAjuda = new ToolStripMenuItem("Ajuda");

            _menuAura.DropDownItems.AddRange(new ToolStripItem[]
            {
                _menuDiagnostico,
                _menuInternet,
                _menuModulos,
                _menuConfiguracoes,
                _menuHistorico,
                _menuAjuda
            });

            _menuStrip.Items.Add(_menuAura);
            _menuStrip.Dock = DockStyle.Top;

            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(32, 45, 64)
            };

            _titleLabel = new Label
            {
                Text = "AURA GENESIS CORE",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 8)
            };

            _subtitleLabel = new Label
            {
                Text = "Bem-vindo ao assistente AURA.",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(18, 42)
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_subtitleLabel);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(16)
            };

            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Pronto.");
            _statusStrip.Items.Add(_statusLabel);

            ClientSize = new Size(900, 620);
            MinimumSize = new Size(760, 480);
            Controls.Add(_contentPanel);
            Controls.Add(_headerPanel);
            Controls.Add(_menuStrip);
            Controls.Add(_statusStrip);
            MainMenuStrip = _menuStrip;
            Text = "AURA Genesis Core";
            StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
