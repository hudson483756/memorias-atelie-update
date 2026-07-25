using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public partial class TelaVisualizarEncomenda : UserControl
    {
        private int idEncomenda;
        private int diaFocado;
        private int mesFocado;
        private int anoFocado;
        private string nomeMesFocado;
        private string origem; // Guarda se veio da "Agenda" ou da "Consulta"

        // Puxa a string de conexão dinâmica do banco
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        // Construtor
        public TelaVisualizarEncomenda(int id, int dia, int mes, int ano, string nomeMes, string telaOrigem)
        {
            InitializeComponent();
            this.idEncomenda = id;
            this.diaFocado = dia;
            this.mesFocado = mes;
            this.anoFocado = ano;
            this.nomeMesFocado = nomeMes;
            this.origem = telaOrigem;

            CarregarDadosCompletos();
        }

        private void CarregarDadosCompletos()
        {
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    string query = @"SELECT c.Nome, e.Produto, e.Descricao, e.FotosCaminhos, e.Valor 
                             FROM Encomendas e
                             INNER JOIN Clientes c ON e.ClienteId = c.Id 
                             WHERE e.Id = @Id";

                    List<string> fotos = new List<string>();

                    using (var comando = new SqliteCommand(query, conexao))
                    {
                        comando.Parameters.AddWithValue("@Id", idEncomenda);

                        using (var reader = comando.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nomeCliente = reader.IsDBNull(0) ? "Cliente não encontrado" : reader.GetString(0);
                                string produto = reader.IsDBNull(1) ? "Produto não especificado" : reader.GetString(1);
                                string descricao = reader.IsDBNull(2) ? "Nenhuma observação." : reader.GetString(2);
                                string fotosBanco = reader.IsDBNull(3) ? "" : reader.GetString(3);

                                double valor = 0.0;
                                if (reader.FieldCount > 4 && !reader.IsDBNull(4))
                                {
                                    valor = reader.GetDouble(4);
                                }

                                TxtCliente.Text = nomeCliente;
                                TxtProduto.Text = produto;
                                TxtObservacoes.Text = descricao;

                                TxtValor.Text = valor > 0 ? valor.ToString("C2") : "R$ 0,00";

                                TxtStatus.Text = "Pendente";
                                BorderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));

                                // RESOLUÇÃO DOS CAMINHOS DAS FOTOS
                                if (!string.IsNullOrWhiteSpace(fotosBanco))
                                {
                                    // Diretorio padrão onde o sistema guarda as imagens no computador
                                    string pastaFotosSistema = Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                        "MemoriasAtelie", "Fotos", "Memorias"
                                    );

                                    string[] caminhos = fotosBanco.Split(';');
                                    foreach (var item in caminhos)
                                    {
                                        if (string.IsNullOrWhiteSpace(item)) continue;

                                        string nomeOuCaminho = item.Trim();
                                        string caminhoFinal = nomeOuCaminho;

                                        // Se não for um caminho absoluto válido, monta a rota completa no disco
                                        if (!File.Exists(nomeOuCaminho))
                                        {
                                            caminhoFinal = Path.Combine(pastaFotosSistema, Path.GetFileName(nomeOuCaminho));
                                        }

                                        // Adiciona à lista de exibição se o arquivo de foto for encontrado
                                        if (File.Exists(caminhoFinal))
                                        {
                                            fotos.Add(caminhoFinal);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (fotos.Count > 0)
                    {
                        ListaFotosGerais.ItemsSource = fotos;
                        ListaFotosGerais.Visibility = Visibility.Visible;
                        TxtAvisoSemFoto.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ListaFotosGerais.Visibility = Visibility.Collapsed;
                        TxtAvisoSemFoto.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar detalhes: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FotoGaleria_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao?.DataContext is string caminhoFotoSelecionada)
            {
                MainWindow principal = Window.GetWindow(this) as MainWindow;
                if (principal != null)
                {
                    principal.AbrirZoom(caminhoFotoSelecionada);
                }
            }
        }

        private void BtnExcluirFoto_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao?.DataContext is string caminhoFoto)
            {
                var resultado = MessageBox.Show("Deseja realmente remover esta foto desta encomenda?",
                                                "Confirmar Remoção", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado != MessageBoxResult.Yes) return;

                try
                {
                    // 1. Obter a lista atual associada ao ItemsControl
                    var fotosAtuais = ListaFotosGerais.ItemsSource as List<string>;
                    if (fotosAtuais != null)
                    {
                        // Remover a foto selecionada
                        fotosAtuais.Remove(caminhoFoto);

                        // 2. Extrai apenas os nomes dos arquivos para salvar de forma relativa no SQLite
                        var apenasNomes = fotosAtuais.Select(f => Path.GetFileName(f)).ToList();
                        string novaFotosCadeia = string.Join(";", apenasNomes);

                        // 3. Atualizar no Banco de Dados
                        using (var conexao = new SqliteConnection(stringConexao))
                        {
                            conexao.Open();
                            string queryUpdate = "UPDATE Encomendas SET FotosCaminhos = @Fotos WHERE Id = @Id;";

                            using (var cmd = new SqliteCommand(queryUpdate, conexao))
                            {
                                cmd.Parameters.AddWithValue("@Fotos", string.IsNullOrWhiteSpace(novaFotosCadeia) ? (object)DBNull.Value : novaFotosCadeia);
                                cmd.Parameters.AddWithValue("@Id", idEncomenda);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 4. Atualizar a interface gráfica
                        ListaFotosGerais.ItemsSource = null;

                        if (fotosAtuais.Count > 0)
                        {
                            ListaFotosGerais.ItemsSource = fotosAtuais;
                            ListaFotosGerais.Visibility = Visibility.Visible;
                            TxtAvisoSemFoto.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ListaFotosGerais.Visibility = Visibility.Collapsed;
                            TxtAvisoSemFoto.Visibility = Visibility.Visible;
                        }

                        MessageBox.Show("Foto removida com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao excluir foto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                if (origem == "Consulta")
                {
                    mainWindow.AreaConteudo.Content = new TelaConsultaEncomendas();
                }
                else if (origem == "Agenda")
                {
                    mainWindow.AreaConteudo.Content = new TelaAgendaAnual();
                }
                else
                {
                    mainWindow.AreaConteudo.Content = new TelaDetalhesDia(diaFocado, mesFocado, anoFocado, nomeMesFocado);
                }
            }
        }
    }
}