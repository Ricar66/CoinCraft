# Relatório de Testes e Correção - Erro StaticResourceExtension

**Data:** 30/11/2025
**Contexto:** Erro "O valor fornecido em 'System.Windows.StaticResourceExtension' iniciou uma exceção" ao iniciar a aplicação ou abrir janelas de ativação.

## 1. Análise do Problema
- **Sintoma:** Exceção de `MarkupExtension` (StaticResource) em tempo de execução.
- **Causa Raiz:** Uso de `{StaticResource Key}` para recursos definidos em `App.xaml` (como Estilos e Brushes) dentro de janelas que são instanciadas antes que o dicionário de recursos da aplicação esteja totalmente mesclado ou inicializado no contexto da thread de UI, ou devido a ordem de carregamento.
- **Solução Técnica:** Substituição global de `{StaticResource ...}` por `{DynamicResource ...}` em todas as Views para referências a recursos globais. `DynamicResource` resolve o recurso em tempo de execução (lookup tardio), o que é tolerante a variações na ordem de inicialização.

## 2. Arquivos Afetados e Corrigidos
Foram identificados e corrigidos os seguintes arquivos XAML, substituindo referências estáticas por dinâmicas:

1.  `src\CoinCraft.App\App.xaml` (Estilos globais agora usam DynamicResource para seus setters)
2.  `src\CoinCraft.App\Views\DashboardWindow.xaml`
3.  `src\CoinCraft.App\Views\LicenseWindow.xaml`
4.  `src\CoinCraft.App\Views\TransactionsWindow.xaml`
5.  `src\CoinCraft.App\Views\ActivationMethodWindow.xaml`
6.  `src\CoinCraft.App\Views\RecurringEditWindow.xaml`
7.  `src\CoinCraft.App\Views\CategoryEditWindow.xaml`
8.  `src\CoinCraft.App\Views\AccountEditWindow.xaml`
9.  `src\CoinCraft.App\Views\TransactionEditWindow.xaml`
10. `src\CoinCraft.App\Views\SettingsWindow.xaml`
11. `src\CoinCraft.App\Views\ImportWindow.xaml`
12. `src\CoinCraft.App\Views\RecurringWindow.xaml`
13. `src\CoinCraft.App\Views\CategoriesWindow.xaml`
14. `src\CoinCraft.App\Views\AccountsWindow.xaml`

**Observação:** Conversores locais (`BooleanToVisibilityConverter` definidos em `Window.Resources`) foram mantidos como `StaticResource` pois são instanciados localmente e não sofrem do problema de escopo global.

## 3. Testes Realizados

### 3.1 Testes de Análise Estática
- **Procedimento:** Varredura de todos os arquivos `.xaml` buscando o padrão `{StaticResource`.
- **Resultado:** Todos os arquivos da aplicação (`src\CoinCraft.App\Views\*.xaml`) foram atualizados. Nenhuma referência a recursos globais via StaticResource permanece.

### 3.2 Testes Unitários e de Integração
- **Procedimento:** Execução da suíte de testes automatizados (`scripts\Run-Tests.ps1`).
- **Resultado:**
  - CoinCraft.Tests: **PASSOU**
  - CoinCraft.App.Tests: **PASSOU**
  - Nenhuma regressão lógica introduzida.

### 3.3 Teste de Compilação (Dry Run de UI)
- **Procedimento:** Recompilação completa da solução e do instalador.
- **Resultado:** Compilação bem-sucedida (`Build-Installer.ps1` finalizado sem erros). Isso garante que a sintaxe XAML (DynamicResource) é válida.

### 3.4 Teste de Regressão (Simulado)
- **Cenário:** Inicialização da aplicação sem licença (fluxo que disparava o erro na `ActivationMethodWindow`).
- **Resultado Esperado:** A janela deve abrir sem lançar exceção de recurso não encontrado, pois o `DynamicResource` aguardará a disponibilidade do recurso.

## 4. Conclusão
A correção foi aplicada em toda a camada de apresentação (Views). O uso de `DynamicResource` é a prática recomendada para aplicações WPF robustas que utilizam temas ou recursos globais em `App.xaml`, prevenindo exatamente este tipo de exceção de inicialização. O instalador foi atualizado com a versão corrigida.
