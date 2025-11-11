# Padrão de Implementação: Botão "Adicionar" e Formulários de Inclusão

Este documento descreve o padrão adotado para o comportamento do botão **Adicionar** em cada página e para os formulários de inclusão (Add) no CoinCraft.

## Diretrizes Gerais

- O botão deve ter `Content="Adicionar"` e chamar um handler `OnAddClick` no code-behind da janela.
- Ao abrir o formulário, definir `Owner = this` para centralizar e herdar o contexto da janela pai.
- Ao salvar no formulário, definir `DialogResult = true` e `Close()` apenas quando os dados forem válidos.
- Após retorno `true`, executar a operação de inclusão no `ViewModel` correspondente e recarregar a lista (`LoadAsync`).

## Padrão por Página

- Contas (`AccountsWindow`)
  - Cria um `Account` novo e abre `AccountEditWindow(account)`.
  - Se `ShowDialog() == true`, chama `AccountsViewModel.AddAsync(account)` e depois `LoadAsync()`.

- Categorias (`CategoriesWindow`)
  - Cria um `Category` novo e abre `CategoryEditWindow(cat)`.
  - Se `ShowDialog() == true`, chama `CategoriesViewModel.AddAsync(cat)` e depois `LoadAsync()`.

- Lançamentos (`TransactionsWindow`)
  - Abre `TransactionEditWindow(_vm)` com `Accounts` e `Categories` já carregadas.
  - Se `ShowDialog() == true`, usa `editor.ResultTransaction` e chama `TransactionsViewModel.AddAsync(tx)`, depois `LoadAsync()`.

## Validações Obrigatórias nos Formulários

- Campos obrigatórios devem ser verificados e bloquear o `Save` com mensagem clara:
  - Conta: nome, tipo, saldo inicial válido.
  - Categoria: nome.
  - Lançamento: valor (> 0), conta selecionada; para transferência, conta destino distinta da origem.

## Boas Práticas

- Preencher controles com valores padrão ao adicionar (ex.: data = hoje; tipo despesa).
- Usar `DisplayMemberPath="Nome"` e `SelectedValuePath="Id"` em `ComboBox` para listas.
- Encapsular logs e mensagens de erro nos `ViewModel.Add/Update/Delete`.

## Exemplo de Handler

```csharp
private async void OnAddClick(object sender, RoutedEventArgs e)
{
    var entity = new Account { Ativa = true };
    var editor = new AccountEditWindow(entity) { Owner = this };
    if (editor.ShowDialog() == true)
    {
        await _vm.AddAsync(entity);
        await _vm.LoadAsync();
    }
}
```

Seguindo este padrão, garantimos consistência visual e funcional na abertura dos formulários e na inclusão de novos itens em todas as seções.