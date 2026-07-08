public class LinhaConsultaModel
{
    public int Id { get; set; }
    public string DataFormatada { get; set; }
    public string NomeCliente { get; set; }
    public string Produto { get; set; }
    public string Descricao { get; set; }
    public string FotosCaminhos { get; set; }
    public double Valor { get; set; }
    public string Status { get; set; }

    // CORREÇÃO: Propriedade que estava faltando para o binding do Grid de Histórico
    public string ValorFormatado => Valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));

    // MELHORIA: Pega apenas o caminho da primeira imagem cadastrada na cadeia string
    public string PrimeiraFoto
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FotosCaminhos)) return null;

            // Retorna o primeiro item antes do primeiro ';'
            string primeiroCaminho = FotosCaminhos.Split(';').FirstOrDefault();

            // Verifica se o arquivo físico realmente existe no computador para evitar erros de renderização
            return System.IO.File.Exists(primeiroCaminho) ? primeiroCaminho : null;
        }
    }
}