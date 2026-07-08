using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public partial class TelaRestaurarBackup : UserControl
    {
        private string pastaBackups;

        public TelaRestaurarBackup()
        {
            InitializeComponent();

            string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            pastaBackups = Path.Combine(pastaDocumentos, "MemoriasAtelie", "Backups");

            CarregarListaBackups();
        }

        private void CarregarListaBackups()
        {
            try
            {
                ListBackupsDisponiveis.Items.Clear();

                if (Directory.Exists(pastaBackups))
                {
                    // Busca todos os arquivos .db na pasta de backups
                    string[] arquivos = Directory.GetFiles(pastaBackups, "*.db");

                    // Ordena do mais recente para o mais antigo
                    Array.Sort(arquivos);
                    Array.Reverse(arquivos);

                    foreach (string arquivo in arquivos)
                    {
                        // Exibe apenas o nome do arquivo para o usuário (ex: memorias_2026_06_20.db)
                        ListBackupsDisponiveis.Items.Add(Path.GetFileName(arquivo));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar lista de backups: " + ex.Message);
            }
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            CarregarListaBackups();
        }

        private void BtnRestaurar_Click(object sender, RoutedEventArgs e)
        {
            if (ListBackupsDisponiveis.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecione um arquivo de backup na lista antes de restaurar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nomeArquivoSelecionado = ListBackupsDisponiveis.SelectedItem.ToString();
            string caminhoBackupOrigem = Path.Combine(pastaBackups, nomeArquivoSelecionado);

            var resultado = MessageBox.Show($"Deseja realmente restaurar o backup '{nomeArquivoSelecionado}'?\nIsso reescreverá a base de dados atual.",
                                            "Confirmar Restauração", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    // Fecha conexões ativas do SQLite para liberar o arquivo para substituição
                    SqliteConnection.ClearAllPools();

                    string caminhoBancoAtual = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memorias.db");

                    // 1. Cria um mini backup temporário de segurança do banco que ia ser destruído
                    if (File.Exists(caminhoBancoAtual))
                    {
                        string caminhoPreRestauração = Path.Combine(pastaBackups, $"Seguranca_Antes_De_Restaurar_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                        File.Copy(caminhoBancoAtual, caminhoPreRestauração, true);
                    }

                    // 2. Sobrescreve o banco original com o arquivo de backup selecionado
                    File.Copy(caminhoBackupOrigem, caminhoBancoAtual, true);

                    MessageBox.Show("Banco de dados restaurado com sucesso!\nO aplicativo aplicou os dados selecionados.",
                                    "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro crítico ao restaurar banco de dados. Certifique-se de que nenhum outro processo está usando o banco de dados.\nDetalhes: " + ex.Message,
                                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}