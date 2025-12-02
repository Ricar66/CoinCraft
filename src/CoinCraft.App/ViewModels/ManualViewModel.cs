using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CoinCraft.App.ViewModels;

public class ManualSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImagePlaceholder { get; set; } // Descrição do que seria a imagem
}

public partial class ManualViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ManualSection> _sections;

    [ObservableProperty]
    private ManualSection? _selectedSection;

    public ManualViewModel()
    {
        _sections = new ObservableCollection<ManualSection>
        {
            new ManualSection 
            { 
                Title = "1. Visão Geral", 
                Content = "Bem-vindo ao CoinCraft!\n\nO CoinCraft é um sistema completo para gestão financeira pessoal. " +
                          "Com ele, você pode controlar suas receitas, despesas, contas bancárias, cartões de crédito e planejar seu futuro financeiro.\n\n" +
                          "Navegue pelos tópicos ao lado para aprender como utilizar cada funcionalidade do sistema.",
                ImagePlaceholder = "Imagem: Tela Inicial do Dashboard com gráficos e resumos"
            },
            new ManualSection 
            { 
                Title = "2. Dashboard", 
                Content = "O Dashboard é a sua central de comando. Nele você encontra:\n\n" +
                          "- Resumo do Mês: Total de Receitas vs. Despesas.\n" +
                          "- Gráfico de Despesas por Categoria: Veja onde você está gastando mais.\n" +
                          "- Saldos das Contas: Visão rápida do saldo atual de cada conta cadastrada.\n" +
                          "- Histórico de Patrimônio: Acompanhe a evolução do seu patrimônio nos últimos 12 meses.\n" +
                          "- Metas do Mês: Acompanhe o progresso das suas metas de gastos.\n\n" +
                          "Dica: Use os filtros de data no topo da tela para visualizar períodos específicos.",
                ImagePlaceholder = "Imagem: Gráficos de pizza e barras do Dashboard"
            },
            new ManualSection 
            { 
                Title = "3. Lançamentos", 
                Content = "A tela de Lançamentos é onde você registra suas movimentações financeiras.\n\n" +
                          "Como adicionar um lançamento:\n" +
                          "1. Clique em '+ Novo Lançamento'.\n" +
                          "2. Preencha a Descrição (ex: 'Supermercado').\n" +
                          "3. Informe o Valor.\n" +
                          "4. Selecione a Categoria (ex: 'Alimentação').\n" +
                          "5. Escolha a Conta de onde saiu ou entrou o dinheiro.\n" +
                          "6. Defina o Tipo (Receita, Despesa ou Transferência).\n" +
                          "7. Clique em 'Salvar'.\n\n" +
                          "Você também pode editar ou excluir lançamentos selecionando-os na lista e usando os botões correspondentes.",
                ImagePlaceholder = "Imagem: Formulário de cadastro de transação"
            },
            new ManualSection 
            { 
                Title = "4. Contas", 
                Content = "Gerencie suas contas bancárias, carteiras e cartões.\n\n" +
                          "- Adicionar Conta: Clique em '+ Nova Conta', dê um nome (ex: 'Nubank', 'Carteira') e defina um saldo inicial.\n" +
                          "- Saldo Inicial: É o valor que você tinha na conta antes de começar a usar o CoinCraft.\n\n" +
                          "Manter suas contas atualizadas é fundamental para que o saldo do sistema bata com a realidade.",
                ImagePlaceholder = "Imagem: Lista de contas cadastradas"
            },
            new ManualSection 
            { 
                Title = "5. Categorias", 
                Content = "Organize seus gastos em categorias para melhor análise.\n\n" +
                          "- O sistema já vem com categorias padrão (Alimentação, Transporte, Lazer, etc.).\n" +
                          "- Você pode criar novas categorias clicando em 'Nova Categoria'.\n" +
                          "- Defina limites mensais para cada categoria para ajudar no controle do orçamento (aparecerá no Dashboard).",
                ImagePlaceholder = "Imagem: Lista de categorias com ícones e cores"
            },
            new ManualSection 
            { 
                Title = "6. Transações Recorrentes", 
                Content = "Automatize lançamentos que se repetem todo mês (ex: Aluguel, Netflix, Salário).\n\n" +
                          "Como configurar:\n" +
                          "1. Vá em 'Recorrentes' e clique em 'Nova'.\n" +
                          "2. Defina a frequência (Mensal, Semanal, etc.).\n" +
                          "3. Escolha se o lançamento deve ser automático (o sistema cria sozinho no dia) ou manual (o sistema avisa e você confirma).\n" +
                          "4. Defina o dia de vencimento.",
                ImagePlaceholder = "Imagem: Tela de configuração de transação recorrente"
            },
            new ManualSection 
            { 
                Title = "7. Importação", 
                Content = "Importe extratos bancários ou arquivos CSV/OFX de outros sistemas.\n\n" +
                          "1. Clique em 'Importar'.\n" +
                          "2. Selecione o arquivo no seu computador.\n" +
                          "3. O sistema tentará identificar as colunas automaticamente.\n" +
                          "4. Revise os lançamentos antes de confirmar a importação.",
                ImagePlaceholder = "Imagem: Tela de pré-visualização de importação"
            },
            new ManualSection 
            { 
                Title = "8. Exemplos Práticos", 
                Content = "Cenário 1: Registrando Salário\n" +
                          "- Vá em Lançamentos > Novo.\n" +
                          "- Tipo: Receita.\n" +
                          "- Valor: R$ 3.000,00.\n" +
                          "- Categoria: Salário.\n" +
                          "- Conta: Conta Corrente.\n\n" +
                          "Cenário 2: Pagamento de Cartão de Crédito\n" +
                          "- Se você controla o cartão como uma conta separada, faça uma Transferência da sua Conta Corrente para a Conta Cartão.\n" +
                          "- Se não, lance como Despesa na Categoria 'Pagamento de Cartão'.",
                ImagePlaceholder = "Imagem: Exemplo de lançamento preenchido"
            },
            new ManualSection 
            { 
                Title = "9. FAQ (Perguntas Frequentes)", 
                Content = "P: Como faço backup dos meus dados?\n" +
                          "R: O CoinCraft utiliza um banco de dados local (SQLite). O arquivo 'coincraft.db' fica na pasta de instalação ou na pasta de dados do usuário. Basta copiar esse arquivo para um local seguro.\n\n" +
                          "P: O sistema funciona sem internet?\n" +
                          "R: Sim! O CoinCraft é 100% offline e local. Seus dados ficam apenas no seu computador.\n\n" +
                          "P: Posso usar em mais de um computador?\n" +
                          "R: Atualmente o sistema é local. Para usar em outro PC, você precisaria copiar o arquivo de banco de dados manualmente.",
                ImagePlaceholder = "Imagem: Ícone de dúvida ou suporte"
            },
            new ManualSection 
            { 
                Title = "10. Glossário", 
                Content = "- Receita: Dinheiro que entra (salário, vendas, rendimentos).\n" +
                          "- Despesa: Dinheiro que sai (contas, compras, lazer).\n" +
                          "- Transferência: Movimentação entre duas contas suas (ex: sacar dinheiro, pagar fatura de cartão).\n" +
                          "- Recorrente: Lançamento que se repete periodicamente.\n" +
                          "- Conciliação: Ato de conferir se o saldo no CoinCraft bate com o saldo real do banco.",
                ImagePlaceholder = "Imagem: Lista de termos"
            }
        };
        
        SelectedSection = _sections.FirstOrDefault();
    }
}
