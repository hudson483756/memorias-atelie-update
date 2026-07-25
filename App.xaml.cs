using System.Windows;

namespace MemoriasAtelie
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Captura erros globais de interface e impede o app de fechar do nada
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    $"Ocorreu um erro na interface:\n\n{args.Exception.Message}\n\nDetalhes:\n{args.Exception.InnerException?.Message}",
                    "Erro Não Tratado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Marca o erro como resolvido para NÃO fechar o aplicativo
                args.Handled = true;
            };
        }
    }
}