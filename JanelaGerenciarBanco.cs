using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MemoriasAtelie
{
    public class JanelaGerenciarBanco : Window
    {
        public enum OpcaoBanco { Cancelado, Teste, Vazio }
        public OpcaoBanco Resultado { get; private set; } = OpcaoBanco.Cancelado;

        public JanelaGerenciarBanco()
        {
            // Configurações da Janela
            Title = "Gerenciar Banco de Dados";
            Height = 180;
            Width = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9FB"));

            // Criamos uma Border para fazer o papel do Padding que o Grid não tem
            Border bordaPrincipal = new Border { Padding = new Thickness(20) };

            // Grid Principal (Agora sem a propriedade Padding)
            Grid gridPrincipal = new Grid();
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Mensagem
            TextBlock txtMensagem = new TextBlock
            {
                Text = "Selecione o modo de inicialização do banco de dados:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444")),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(txtMensagem, 0);
            gridPrincipal.Children.Add(txtMensagem);

            // Painel de Botões
            StackPanel painelBotoes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(painelBotoes, 1);

            // Botão Banco Teste
            Button btnTeste = new Button
            {
                Content = "Banco Teste",
                Width = 110,
                Height = 35,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnTeste.Click += (s, e) => { Resultado = OpcaoBanco.Teste; DialogResult = true; Close(); };

            // Botão Banco Vazio
            Button btnVazio = new Button
            {
                Content = "Banco Vazio",
                Width = 110,
                Height = 35,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnVazio.Click += (s, e) => { Resultado = OpcaoBanco.Vazio; DialogResult = true; Close(); };

            // Botão Cancelar
            Button btnCancelar = new Button
            {
                Content = "Cancelar",
                Width = 90,
                Height = 35,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancelar.Click += (s, e) => { Resultado = OpcaoBanco.Cancelado; DialogResult = false; Close(); };

            // Adiciona os botões ao painel
            painelBotoes.Children.Add(btnTeste);
            painelBotoes.Children.Add(btnVazio);
            painelBotoes.Children.Add(btnCancelar);

            gridPrincipal.Children.Add(painelBotoes);

            // Vincula o Grid dentro da Border, e a Border como conteúdo da Janela
            bordaPrincipal.Child = gridPrincipal;
            Content = bordaPrincipal;
        }
    }
}