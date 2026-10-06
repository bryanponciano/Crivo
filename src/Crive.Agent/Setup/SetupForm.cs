using Crive.Agent.Communication;
using Crive.Shared.DTOs;

namespace Crive.Agent.Setup;

public class SetupForm : Form
{
    private readonly OfflineCache _cache;
    private readonly EnrollmentService _enrollment;

    private TextBox txtServerUrl;
    private TextBox txtTenant;
    private TextBox txtEmployee;
    private TextBox txtAsset;
    private ComboBox cmbSectors;
    private Button btnLoadSectors;
    private Button btnRegister;
    private Label lblStatus;

    public SetupForm(OfflineCache cache, EnrollmentService enrollment)
    {
        _cache = cache;
        _enrollment = enrollment;
        
        Text = "Configuração do Agente Crivo";
        Size = new Size(400, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        int y = 20;

        Controls.Add(new Label { Text = "URL do Servidor (ex: https://...):", Left = 20, Top = y, Width = 340 });
        y += 25;
        txtServerUrl = new TextBox { Left = 20, Top = y, Width = 340, Text = "http://localhost:8080" };
        Controls.Add(txtServerUrl);
        y += 35;

        Controls.Add(new Label { Text = "Código do Tenant:", Left = 20, Top = y, Width = 340 });
        y += 25;
        txtTenant = new TextBox { Left = 20, Top = y, Width = 230, Text = "exemplo" };
        Controls.Add(txtTenant);

        btnLoadSectors = new Button { Text = "Carregar", Left = 260, Top = y - 1, Width = 100 };
        btnLoadSectors.Click += async (s, e) => await LoadSectorsAsync();
        Controls.Add(btnLoadSectors);
        y += 35;

        Controls.Add(new Label { Text = "Setor:", Left = 20, Top = y, Width = 340 });
        y += 25;
        cmbSectors = new ComboBox { Left = 20, Top = y, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
        Controls.Add(cmbSectors);
        y += 35;

        Controls.Add(new Label { Text = "Nome do Funcionário:", Left = 20, Top = y, Width = 340 });
        y += 25;
        txtEmployee = new TextBox { Left = 20, Top = y, Width = 340 };
        Controls.Add(txtEmployee);
        y += 35;

        Controls.Add(new Label { Text = "Número do Patrimônio/Tag:", Left = 20, Top = y, Width = 340 });
        y += 25;
        txtAsset = new TextBox { Left = 20, Top = y, Width = 340 };
        Controls.Add(txtAsset);
        y += 35;

        btnRegister = new Button { Text = "Registrar Máquina", Left = 20, Top = y, Width = 340, Height = 40 };
        btnRegister.Click += async (s, e) => await RegisterAsync();
        Controls.Add(btnRegister);
        
        y += 45;
        lblStatus = new Label { Left = 20, Top = y, Width = 340, ForeColor = Color.Blue };
        Controls.Add(lblStatus);
    }

    private async Task LoadSectorsAsync()
    {
        btnLoadSectors.Enabled = false;
        lblStatus.Text = "Carregando setores...";
        
        var sectors = await _enrollment.GetSectorsAsync(txtServerUrl.Text, txtTenant.Text);
        
        cmbSectors.Items.Clear();
        foreach (var s in sectors)
        {
            cmbSectors.Items.Add(new ComboBoxItem { Text = s.Name, Value = s.Id });
        }
        
        if (cmbSectors.Items.Count > 0) cmbSectors.SelectedIndex = 0;
        
        lblStatus.Text = sectors.Count > 0 ? "Setores carregados." : "Nenhum setor encontrado.";
        btnLoadSectors.Enabled = true;
    }

    private async Task RegisterAsync()
    {
        if (cmbSectors.SelectedItem == null)
        {
            MessageBox.Show("Carregue e selecione um setor primeiro.");
            return;
        }

        btnRegister.Enabled = false;
        lblStatus.Text = "Registrando agente...";

        var sectorId = ((ComboBoxItem)cmbSectors.SelectedItem).Value;
        var config = await _enrollment.RegisterAsync(txtServerUrl.Text, txtTenant.Text, sectorId, txtEmployee.Text, txtAsset.Text);

        if (config != null)
        {
            _cache.SaveConfig(config);
            MessageBox.Show("Máquina registrada com sucesso!");
            Close();
        }
        else
        {
            lblStatus.Text = "Erro ao registrar. Verifique os logs.";
            MessageBox.Show("Falha no registro. Verifique a URL e o Código do Tenant.");
            btnRegister.Enabled = true;
        }
    }

    private class ComboBoxItem
    {
        public string Text { get; set; } = string.Empty;
        public Guid Value { get; set; }
        public override string ToString() => Text;
    }
}
