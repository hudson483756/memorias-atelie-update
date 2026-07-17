using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace MemoriasAtelie
{
    // CLASSE AUXILIAR (No nível do namespace para que TODAS as telas, como a de Consulta, possam acessá-la)
    public class ClienteItem
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Whatsapp { get; set; }
        public string Medidas { get; set; }
    }

    public partial class TelaCadastroEncomenda : UserControl
    {
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        private List<string> caminhosFotosAnexadas = new List<string>();
        private LinhaConsultaModel encomendaEdicao = null;
        private bool estaEditando = false;
        private bool manipuladorAtivo = true;

        public TelaCadastroEncomenda()
        {
            InitializeComponent();
            CarregarDadosIniciais();
            AjustarLayoutModo();
        }

        public TelaCadastroEncomenda(LinhaConsultaModel encomenda)
        {
            InitializeComponent();
            CarregarDadosIniciais();
            encomendaEdicao = encomenda;
            estaEditando = true;
            AjustarLayoutModo();
            PreencherCamposEdicao();
        }

        private void CarregarDadosIniciais()
        {
            CarregarClientes();
            CarregarProdutosSugeridos();
        }

        private void AjustarLayoutModo()
        {
            if (estaEditando)
            {
                TxtTituloTela.Text = "Editar Encomenda";
            }
            else
            {
                TxtTituloTela.Text = "Nova Encomenda";
                DpData.SelectedDate = DateTime.Now;
            }
        }

        private void CarregarClientes()
        {
            var lista = new List<ClienteItem>();
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string query = "SELECT Id, Nome, Whatsapp, Medidas FROM Clientes ORDER BY Nome ASC;";
                    using (var cmd = new SqliteCommand(query, conexao))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ClienteItem
                            {
                                Id = reader.GetInt32(0),
                                Nome = reader.GetString(1),
                                Whatsapp = reader["Whatsapp"] != DBNull.Value ? reader["Whatsapp"].ToString() : "",
                                Medidas = reader["Medidas"] != DBNull.Value ? reader["Medidas"].ToString() : ""
                            });
                        }
                    }
                }
                CboClientes.ItemsSource = lista;
                CboClientes.DisplayMemberPath = "Nome";
                CboClientes.SelectedValuePath = "Nome";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar clientes: " + ex.Message);
            }
        }

        private void CarregarProdutosSugeridos()
        {
            var lista = new List<string>();
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string query = "SELECT Nome FROM Produtos ORDER BY Nome ASC;";
                    using (var cmd = new SqliteCommand(query, conexao))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(reader.GetString(0));
                        }
                    }
                }
                CboProdutos.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar produtos: " + ex.Message);
            }
        }

        private void BtnAdicionarProdutoRapido_Click(object sender, RoutedEventArgs e)
        {
            string novoProduto = Microsoft.VisualBasic.Interaction.InputBox(
                "Digite o nome do novo produto:",
                "Cadastrar Produto",
                "").Trim();

            if (string.IsNullOrEmpty(novoProduto)) return;

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    string queryVerifica = "SELECT COUNT(*) FROM Produtos WHERE LOWER(Nome) = LOWER(@Nome);";
                    using (var cmdVerifica = new SqliteCommand(queryVerifica, conexao))
                    {
                        cmdVerifica.Parameters.AddWithValue("@Nome", novoProduto);
                        int existe = Convert.ToInt32(cmdVerifica.ExecuteScalar());

                        if (existe > 0)
                        {
                            MessageBox.Show("Este produto já está cadastrado!", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                            CboProdutos.Text = novoProduto;
                            return;
                        }
                    }

                    string queryInsert = @"INSERT INTO Produtos (Nome, UltimaAtualizacao, DispositivoOrigem) 
                                           VALUES (@Nome, @UltimaAtualizacao, @DispositivoOrigem);";
                    using (var cmdInsert = new SqliteCommand(queryInsert, conexao))
                    {
                        cmdInsert.Parameters.AddWithValue("@Nome", novoProduto);
                        cmdInsert.Parameters.AddWithValue("@UltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdInsert.Parameters.AddWithValue("@DispositivoOrigem", "Windows");
                        cmdInsert.ExecuteNonQuery();
                    }
                }

                CarregarProdutosSugeridos();
                CboProdutos.Text = novoProduto;
                MessageBox.Show($"✨ Produto '{novoProduto}' cadastrado e selecionado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao cadastrar produto: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdicionarClienteRapido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var telaClienteControl = new TelaCadastroCliente();

                var janelaPopup = new Window
                {
                    Title = "Cadastrar Novo Cliente",
                    Content = telaClienteControl,
                    Width = 500,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize
                };

                janelaPopup.ShowDialog();

                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string queryUltimoCliente = "SELECT Nome, Whatsapp, Medidas FROM Clientes ORDER BY Id DESC LIMIT 1;";

                    using (var cmd = new SqliteCommand(queryUltimoCliente, conexao))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string nomeRecente = reader["Nome"].ToString();
                            string whatsappRecente = reader["Whatsapp"] != DBNull.Value ? reader["Whatsapp"].ToString() : "";
                            string medidasRecente = reader["Medidas"] != DBNull.Value ? reader["Medidas"].ToString() : "";

                            CarregarClientes();

                            CboClientes.Text = nomeRecente;

                            TxtClienteWhatsapp.Text = whatsappRecente;
                            TxtClienteMedidas.Text = medidasRecente;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir tela de cadastro de cliente: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!manipuladorAtivo) return;

            if (CboClientes.SelectedItem is ClienteItem clienteSelecionado)
            {
                TxtClienteWhatsapp.Text = clienteSelecionado.Whatsapp;
                TxtClienteMedidas.Text = clienteSelecionado.Medidas;
            }
            else
            {
                TxtClienteWhatsapp.Text = "";
                TxtClienteMedidas.Text = "";
            }
        }

        private void BtnAnexarFotos_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string arquivo in openFileDialog.FileNames)
                {
                    if (!caminhosFotosAnexadas.Contains(arquivo))
                    {
                        caminhosFotosAnexadas.Add(arquivo);
                    }
                }
                AtualizarGaleriaFotos();
            }
        }

        private void AtualizarGaleriaFotos()
        {
            IcFotosAnexadas.ItemsSource = null;
            IcFotosAnexadas.ItemsSource = caminhosFotosAnexadas;
        }

        private void BtnSalvarEncomenda_Click(object sender, RoutedEventArgs e)
        {
            string nomeCliente = CboClientes.Text.Trim();
            string produto = CboProdutos.Text.Trim();
            string descricao = TxtDescricao.Text.Trim();
            string valorTexto = TxtValor.Text.Trim();
            string status = (CboStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Pendente";
            string data = DpData.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

            if (string.IsNullOrEmpty(nomeCliente) || string.IsNullOrEmpty(produto))
            {
                MessageBox.Show("Por favor, preencha o Cliente e o Produto.", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double.TryParse(valorTexto, out double valor);
            string fotosCaminhosStr = string.Join(";", caminhosFotosAnexadas);
            int clienteId = ObterIdClientePorNome(nomeCliente);

            if (clienteId == -1)
            {
                MessageBox.Show("Cliente não encontrado. Certifique-se de que o cliente inserido está devidamente cadastrado no sistema.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string query;

                    if (estaEditando)
                    {
                        query = @"UPDATE Encomendas 
                                  SET ClienteId = @ClienteId, Produto = @Produto, Descricao = @Descricao, 
                                      FotosCaminhos = @FotosCaminhos, Valor = @Valor, Status = @Status, Data = @Data,
                                      UltimaAtualizacao = @UltimaAtualizacao, DispositivoOrigem = @DispositivoOrigem
                                  WHERE Id = @Id;";
                    }
                    else
                    {
                        query = @"INSERT INTO Encomendas (ClienteId, Produto, Descricao, FotosCaminhos, Valor, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                                  VALUES (@ClienteId, @Produto, @Descricao, @FotosCaminhos, @Valor, @Status, @Data, @UltimaAtualizacao, @DispositivoOrigem);";
                    }

                    using (var cmd = new SqliteCommand(query, conexao))
                    {
                        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                        cmd.Parameters.AddWithValue("@Produto", produto);
                        cmd.Parameters.AddWithValue("@Descricao", descricao);
                        cmd.Parameters.AddWithValue("@FotosCaminhos", fotosCaminhosStr);
                        cmd.Parameters.AddWithValue("@Valor", valor);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Data", data);

                        cmd.Parameters.AddWithValue("@UltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@DispositivoOrigem", "Windows");

                        if (estaEditando)
                        {
                            cmd.Parameters.AddWithValue("@Id", encomendaEdicao.Id);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(estaEditando ? "Encomenda atualizada com sucesso!" : "Encomenda salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                LimparCampos();

                var mainWindow = Window.GetWindow(this);
                if (mainWindow != null)
                {
                    var btnConsulta = mainWindow.FindName("BtnConsulta") as Button;
                    btnConsulta?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar encomenda: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimparCampos()
        {
            manipuladorAtivo = false;
            CboClientes.SelectedIndex = -1;
            CboClientes.Text = "";
            CboProdutos.SelectedIndex = -1;
            CboProdutos.Text = "";
            TxtClienteWhatsapp.Text = "";
            TxtClienteMedidas.Text = "";
            TxtDescricao.Text = "";
            TxtValor.Text = "";
            CboStatus.SelectedIndex = 0;
            DpData.SelectedDate = DateTime.Now;
            caminhosFotosAnexadas.Clear();
            AtualizarGaleriaFotos();
            encomendaEdicao = null;
            estaEditando = false;
            AjustarLayoutModo();
            manipuladorAtivo = true;
        }

        private void PreencherCamposEdicao()
        {
            if (encomendaEdicao == null) return;

            manipuladorAtivo = false;
            CboClientes.Text = encomendaEdicao.NomeCliente;
            CboProdutos.Text = encomendaEdicao.Produto;
            TxtDescricao.Text = encomendaEdicao.Descricao;
            TxtValor.Text = encomendaEdicao.Valor.ToString();

            foreach (var item in CboStatus.Items)
            {
                if (item is ComboBoxItem cbItem && cbItem.Content.ToString() == encomendaEdicao.Status)
                {
                    cbItem.IsSelected = true;
                    break;
                }
            }

            if (DateTime.TryParse(encomendaEdicao.DataFormatada, out DateTime dataEnc))
            {
                DpData.SelectedDate = dataEnc;
            }

            if (!string.IsNullOrEmpty(encomendaEdicao.FotosCaminhos))
            {
                caminhosFotosAnexadas = encomendaEdicao.FotosCaminhos.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                AtualizarGaleriaFotos();
            }

            using (var conexao = new SqliteConnection(stringConexao))
            {
                conexao.Open();
                string query = "SELECT Whatsapp, Medidas FROM Clientes WHERE Nome = @Nome;";
                using (var cmd = new SqliteCommand(query, conexao))
                {
                    cmd.Parameters.AddWithValue("@Nome", encomendaEdicao.NomeCliente);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            TxtClienteWhatsapp.Text = r["Whatsapp"].ToString();
                            TxtClienteMedidas.Text = r["Medidas"] != DBNull.Value ? r["Medidas"].ToString() : "";
                        }
                    }
                }
            }
            manipuladorAtivo = true;
        }

        private int ObterIdClientePorNome(string nome)
        {
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    using (var cmd = new SqliteCommand("SELECT Id FROM Clientes WHERE Nome = @Nome;", conexao))
                    {
                        cmd.Parameters.AddWithValue("@Nome", nome);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return -1; }
        }

        private void TxtValor_TextChanged(object sender, TextChangedEventArgs e) { }
    }
}