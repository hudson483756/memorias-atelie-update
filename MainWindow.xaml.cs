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

            // CHAMADA ADICIONADA: Configura as pastas e faz o backup inicial ao abrir o programa
            ConfigurarEstruturaEBackup();

            // 1º Garante que o arquivo do banco existe e tem as tabelas necessárias
            MemoriasAtelie.GerenciadorBanco.InicializarEstruturaPadrao();

            
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

                string caminhoBancoOriginal = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memorias.db");

                if (System.IO.File.Exists(caminhoBancoOriginal))
                {
                    string nomeBackup = $"memorias_{DateTime.Now:yyyy_MM_dd}.db";
                    string caminhoDestinoBackup = System.IO.Path.Combine(pastaBackups, nomeBackup);

                    System.IO.File.Copy(caminhoBancoOriginal, caminhoDestinoBackup, true);

                    // OPCIONAL: Se quiser que avise logo na abertura, descomente a linha abaixo:
                    // MessageBox.Show($"Backup diário de inicialização salvo em:\n{caminhoDestinoBackup}", "Segurança Ativada", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        // AGORA CHAMA A FUNÇÃO QUE ADICIONA OS DADOS FALSOS
                        GerenciadorBanco.CriarBancoTeste();

                        // Dica: Se a TelaConsultaEncomendas estiver aberta atrás, 
                        // vale a pena recarregar a tela para os dados aparecerem na hora.
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

                string caminhoBancoOriginal = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memorias.db");

                if (System.IO.File.Exists(caminhoBancoOriginal))
                {
                    string nomeBackupFechamento = $"memorias_backup_fechamento_{DateTime.Now:yyyy_MM_dd_HHmmss}.db";
                    string caminhoDestino = System.IO.Path.Combine(pastaBackups, nomeBackupFechamento);

                    System.IO.File.Copy(caminhoBancoOriginal, caminhoDestino, true);

                    // ATUALIZAÇÃO IMPORTANTE: Copia automaticamente o caminho completo para a Área de Transferência (Ctrl+V)
                    Clipboard.SetText(caminhoDestino);

                    // Mensagem avisando que a cópia já foi feita
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

        /// <summary>
        /// Torna o Zoom visível preenchendo 100% da aplicação
        /// </summary>
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

        // Evento que esconde a camada de Zoom e limpa o cache de imagem da tela
        private void FecharZoomGlobal_Click(object sender, MouseButtonEventArgs e)
        {
            GridZoomGlobal.Visibility = Visibility.Collapsed;
            ImgZoomGlobalPreview.Source = null;
        }

        // Sobrecarga para capturar também o clique do botão físico de fechar
        private void FecharZoomGlobal_Click(object sender, RoutedEventArgs e)
        {
            GridZoomGlobal.Visibility = Visibility.Collapsed;
            ImgZoomGlobalPreview.Source = null;
        }

        // =========================================================================
        // EVENTOS DE NAVEGAÇÃO INTERNA E MENUS
        // =========================================================================

        // Evento para abrir o menu de hambúrguer ao clicar nele
        private void BotaoMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuSuspenso.IsOpen = true;
        }

        // Evento para carregar a tela de Cadastro de Cliente
        private void MenuCadastroCliente_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaCadastroCliente();
        }

        // Evento para carregar a tela de encomendas
        private void MenuNovaEncomenda_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaCadastroEncomenda();
        }

        // Evento adicionado para carregar a tela de Gestão Financeira
        private void MenuGestaoFinanceira_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaGestaoFinanceira();
        }

        // Evento que abre a tela de meses a partir do menu
        private void MenuAgendaAnual_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaAgendaAnual();
        }

        // Evento para limpar a tela e voltar a ver apenas a logo
        private void MenuInicio_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = null;
        }

        // Evento para carregar a tela de restauração de backups
        private void MenuRestaurarBackup_Click(object sender, RoutedEventArgs e)
        {
            AreaConteudo.Content = new TelaRestaurarBackup();
        }

        // =========================================================================
        // BOTÕES DE CONTROLE DA JANELA PRINCIPAL
        // =========================================================================

        // Evento para Minimizar a Janela
        private void BtnMinimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Evento para Maximizar ou Restaurar o tamanho da Janela
        private void BtnMaximizar_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                TxtIconeMaximizar.Text = "\uE922"; // Ícone de um quadrado (Maximizar)
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                TxtIconeMaximizar.Text = "\uE923"; // Ícone de dois quadrados sobrepostos (Restaurar)
            }
        }

        private void BotaoSair_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Fecha a aplicação
        }
    }
}