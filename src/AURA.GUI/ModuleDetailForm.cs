using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AURA.Modules;

namespace AURA.GUI
{
    /// <summary>
    /// The "Assistente de implementação" screen: explains step by step what
    /// a module will do before the user opts to prepare it, per the master
    /// spec. This never installs anything in the MVP - it only records the
    /// user's intent via ModulesConfiguration.
    /// </summary>
    public sealed class ModuleDetailForm : Form
    {
        public ModuleDetailForm(ModuleInfo module, bool alreadyPrepared)
        {
            if (module == null) throw new ArgumentNullException("module");

            Text = "Você escolheu: " + module.DisplayName;
            ClientSize = new Size(460, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };

            var heading = new Label
            {
                Text = module.Icon + "  " + module.DisplayName,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var steps = new Label
            {
                Text = "O que será implementado:\n" +
                       string.Join("\n", module.ImplementationSteps.Select((s, i) => (i + 1) + ". " + s)),
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Location = new Point(0, heading.Bottom + 16)
            };

            var capabilities = new Label
            {
                Text = "Recursos adquiridos:\n" +
                       string.Join("\n", module.AcquiredCapabilities.Select(c => "✓ " + c)),
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Location = new Point(0, steps.Bottom + 16)
            };

            var meta = new Label
            {
                Text = "Nível de dificuldade: " + FormatDifficulty(module.Difficulty) + "\n" +
                       "Tempo estimado: " + module.EstimatedTime,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Location = new Point(0, capabilities.Bottom + 16)
            };

            var statusLabel = new Label
            {
                Text = alreadyPrepared ? "Este módulo já está marcado para implementação." : "",
                ForeColor = Color.SeaGreen,
                AutoSize = true,
                Location = new Point(0, meta.Bottom + 12)
            };

            var prompt = new Label
            {
                Text = "Deseja preparar este módulo?",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, statusLabel.Bottom + 20)
            };

            var yesButton = new Button { Text = "SIM", DialogResult = DialogResult.Yes, Location = new Point(0, prompt.Bottom + 10), Width = 90 };
            var noButton = new Button { Text = "NÃO", DialogResult = DialogResult.No, Location = new Point(100, prompt.Bottom + 10), Width = 90 };

            scroll.Controls.Add(heading);
            scroll.Controls.Add(steps);
            scroll.Controls.Add(capabilities);
            scroll.Controls.Add(meta);
            scroll.Controls.Add(statusLabel);
            scroll.Controls.Add(prompt);
            scroll.Controls.Add(yesButton);
            scroll.Controls.Add(noButton);

            Controls.Add(scroll);
            AcceptButton = yesButton;
            CancelButton = noButton;
        }

        private static string FormatDifficulty(ModuleDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ModuleDifficulty.Basico: return "Básico";
                case ModuleDifficulty.Intermediario: return "Intermediário";
                case ModuleDifficulty.Avancado: return "Avançado";
                default: return difficulty.ToString();
            }
        }
    }
}
