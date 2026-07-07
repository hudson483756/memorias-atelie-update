using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public partial class TelaCadastroCliente : UserControl
    {
        private bool _isUpdating = false;
        private string stringConexao = "Data Source=memorias.db";

        public TelaCadastroCliente()
        {
            InitializeComponent();
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            string nome = TxtNome.Text.Trim();
            string telefone = TxtTelefone.Text.Trim();
            string medidas = TxtMedidas.Text.Trim();

            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(telefone))
            {
                MessageBox.Show("Por favor, preencha o Nome e o WhatsApp!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string comandoInserir = "INSERT INTO Clientes (Nome, Whatsapp, Medidas) VALUES (@Nome, @Whatsapp, @Medidas);";

                    using (var comando = new SqliteCommand(comandoInserir, conexao))
                    {
                        comando.Parameters.AddWithValue("@Nome", nome);
                        comando.Parameters.AddWithValue("@Whatsapp", telefone);
                        comando.Parameters.AddWithValue("@Medidas", medidas);
                        comando.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✨ Cliente cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtNome.Clear();
                TxtTelefone.Clear();
                TxtMedidas.Clear();

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.AreaConteudo.Content = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar cliente: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtTelefone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string apenasNumeros = Regex.Replace(textBox.Text, @"[^\d]", "");
                string textoFormatado = "";

                if (apenasNumeros.Length > 0)
                {
                    textoFormatado += "(";
                    if (apenasNumeros.Length <= 2)
                    {
                        textoFormatado += apenasNumeros;
                    }
                    else
                    {
                        textoFormatado += apenasNumeros.Substring(0, 2) + ") ";
                        if (apenasNumeros.Length <= 7)
                        {
                            textoFormatado += apenasNumeros.Substring(2);
                        }
                        else
                        {
                            textoFormatado += apenasNumeros.Substring(2, 5) + "-" + apenasNumeros.Substring(7);
                        }
                    }
                }

                textBox.Text = textoFormatado;
                textBox.CaretIndex = textBox.Text.Length;
            }
            _isUpdating = false;
        }
    }
}