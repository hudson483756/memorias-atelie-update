using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MemoriasAtelie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Configura as pastas e faz o backup inicial ao abrir o programa
            ConfigurarEstruturaEBackup();

            // 1º Garante que o arquivo do banco existe e tem as tabelas necessárias
            MemoriasAtelie.GerenciadorBanco.InicializarEstruturaPadrao();

            // 2º Sincroniza e mescla automaticamente as novidades vindas do banco do Android
            MemoriasAtelie.SincronizadorLocal.SincronizarComAndroid();
        }

        // Adicione este método dentro da classe MainWindow em MainWindow.xaml.cs
        private void MenuRelatorioEncomendas_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaConsultaEncomendas();
        }

        private void BtnConfigurarBanco_Click(object sender, RoutedEventArgs e)
        {
            // Passa a Window atual como referência para o efeito de sobreposição (Owner)
        }

        // =========================================================================
        // MÉTODOS DE SEGURANÇA, PASTAS E BACKUP AUTOMÁTICO
        // =========================================================================

        private void ConfigurarEstruturaEBackup()
        {
            try
            {
                string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string pastaRaizAtelie = System.IO.Path.Combine(pastaDocumentos, "MemoriasAtelie");
                string pastaFotos = System.IO.Path.Combine(pastaRaizAtelie, "Fotos");
                string pastaBackups = System.IO.Path.Combine(pastaRaizAtelie, "Backups");

                if (!Directory.Exists(pastaRaizAtelie)) Directory.CreateDirectory(pastaRaizAtelie);
                if (!Directory.Exists(pastaFotos)) Directory.CreateDirectory(pastaFotos);
                if (!Directory.Exists(pastaBackups)) Directory.CreateDirectory(pastaBackups);

                string caminhoBancoOriginal = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memoriasWindows.db");

                if (System.IO.File.Exists(caminhoBancoOriginal))
                {
                    string nomeBackup = $"memorias_{DateTime.Now:yyyy_MM_dd}.db";
                    string caminhoDestinoBackup = System.IO.Path.Combine(pastaBackups, nomeBackup);

                    System.IO.File.Copy(caminhoBancoOriginal, caminhoDestinoBackup, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao configurar pastas de segurança: " + ex.Message);
            }
        }

        private void MenuGerenciarBanco_Click(object sender, RoutedEventArgs e)
        {
            JanelaGerenciarBanco janelaBanco = new JanelaGerenciarBanco { Owner = this };

            if (janelaBanco.ShowDialog() == true)
            {
                switch (janelaBanco.Resultado)
                {
                    case JanelaGerenciarBanco.OpcaoBanco.Teste:
                        GerenciadorBanco.CriarBancoTeste();
                        break;

                    case JanelaGerenciarBanco.OpcaoBanco.Vazio:
                        GerenciadorBanco.CriarBancoVazio();
                        break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string pastaBackups = System.IO.Path.Combine(pastaDocumentos, "MemoriasAtelie", "Backups");

                string caminhoBancoOriginal = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memoriasWindows.db");

                if (System.IO.File.Exists(caminhoBancoOriginal))
                {
                    string nomeBackupFechamento = $"memorias_backup_fechamento_{DateTime.Now:yyyy_MM_dd_HHmmss}.db";
                    string caminhoDestino = System.IO.Path.Combine(pastaBackups, nomeBackupFechamento);

                    System.IO.File.Copy(caminhoBancoOriginal, caminhoDestino, true);

                    Clipboard.SetText(caminhoDestino);

                    MessageBox.Show($"Sessão encerrada com segurança!\n\n" +
                                    $"O backup foi salvo em:\n{caminhoDestino}\n\n" +
                                    $"✨ O caminho foi copiado automaticamente! Basta usar o Ctrl+V no Google Drive para encontrá-lo.",
                                    "Backup de Fechamento Concluído",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aviso: Não foi possível gerar o backup de fechamento: " + ex.Message);
            }
        }

        // =========================================================================
        // MÉTODOS DE ZOOM GLOBAL
        // =========================================================================

        public void AbrirZoom(string caminhoImagem)
        {
            if (!string.IsNullOrWhiteSpace(caminhoImagem))
            {
                try
                {
                    ImgZoomGlobalPreview.Source = new BitmapImage(new Uri(caminhoImagem, UriKind.RelativeOrAbsolute));
                    GridZoomGlobal.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Não foi possível ampliar esta imagem: " + ex.Message);
                }
            }
        }

        private void FecharZoomGlobal_Click(object sender, MouseButtonEventArgs e)
        {
            GridZoomGlobal.Visibility = Visibility.Collapsed;
            ImgZoomGlobalPreview.Source = null;
        }

        private void FecharZoomGlobal_Click(object sender, RoutedEventArgs e)
        {
            GridZoomGlobal.Visibility = Visibility.Collapsed;
            ImgZoomGlobalPreview.Source = null;
        }

        // =========================================================================
        // EVENTOS DE NAVEGAÇÃO INTERNA E MENUS
        // =========================================================================

        private void BotaoMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuSuspenso.IsOpen = true;
        }

        private void MenuCadastroCliente_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaCadastroCliente();
        }

        private void MenuNovaEncomenda_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaCadastroEncomenda();
        }

        private void MenuGestaoFinanceira_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaGestaoFinanceira();
        }

        private void MenuAgendaAnual_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaAgendaAnual();
        }

        private void MenuInicio_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = null;
        }

        private void MenuRestaurarBackup_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaRestaurarBackup();
        }

        // =========================================================================
        // BOTÕES DE CONTROLE DA JANELA PRINCIPAL
        // =========================================================================

        private void BtnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximizar_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                TxtIconeMaximizar.Text = "\uE922";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                TxtIconeMaximizar.Text = "\uE923";
            }
        }

        private void BotaoSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}