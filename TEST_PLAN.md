# Plano de Testes - CoinCraft

Este documento descreve a estratégia de testes para validar o funcionamento, estabilidade e qualidade do aplicativo CoinCraft.

## 1. Escopo
O plano abrange testes unitários, integração, sistema, usabilidade, desempenho e segurança para a versão Desktop (WPF) do CoinCraft.

## 2. Estratégia de Testes

### 2.1 Testes Automatizados (Unitários e Integração)
**Ferramentas:** xUnit, Moq, FluentAssertions.
**Execução:** `scripts/Run-Tests.ps1` ou `dotnet test`.

| Componente | Tipo | Cobertura |
|------------|------|-----------|
| **Services** | Unitário | Validação de licenciamento, chamadas API (mockadas). |
| **ViewModels** | Unitário | Lógica de apresentação, comandos, validação de dados. |
| **Infrastructure** | Integração | Persistência de dados (SQLite), Migrations, Relacionamentos EF Core. |

### 2.2 Testes de Sistema (Manual/Exploratório)
**Objetivo:** Validar fluxos completos do usuário final.

**Cenários Principais:**
1.  **Instalação Limpa:** Instalar em máquina virgem. Verificar criação de atalhos e DB.
2.  **Ativação:**
    *   Fluxo Online (E-mail + ID).
    *   Fluxo Offline (Chave manual).
    *   Tentativa de ativação com chave inválida/expirada.
3.  **Dashboard:**
    *   Verificar carregamento de KPIs.
    *   Verificar gráficos (Pizza, Barras, Patrimônio).
    *   Filtro de datas.
4.  **CRUDs:**
    *   Criar Conta, Categoria, Lançamento.
    *   Editar e Excluir itens.
    *   Verificar reflexo no Dashboard.
5.  **Importação:**
    *   Importar CSV/OFX.
    *   Mapeamento de colunas.

### 2.3 Testes de Usabilidade
**Público:** Usuários finais (beta testers).
**Métricas:**
*   Tempo para completar "Adicionar Lançamento".
*   Facilidade em encontrar "Relatórios".
*   Clareza das mensagens de erro.

### 2.4 Testes de Desempenho
*   **Carga de Dados:** Inserir 10.000 lançamentos e verificar tempo de carregamento do Dashboard.
*   **Startup:** Tempo até a tela de login/dashboard aparecer (< 3s).

### 2.5 Testes de Segurança
*   **Banco de Dados:** Verificar se o arquivo `.db` está em local protegido (`AppData`).
*   **Licença:** Verificar se o arquivo `license.dat` está encriptado/obfuscado.
*   **Tráfego:** Verificar se comunicação com API é HTTPS.

## 3. Procedimentos de Execução

1.  **Preparação:**
    *   Compilar versão `Release`.
    *   Limpar `AppData/CoinCraft` (backup se necessário).
2.  **Execução Automática:**
    *   Rodar `scripts/Run-Tests.ps1`.
    *   Verificar relatório de cobertura e falhas.
3.  **Execução Manual:**
    *   Seguir checklist de Cenários Principais.
    *   Registrar bugs no GitHub Issues.

## 4. Critérios de Aceite
*   **Unitários/Integração:** 100% de sucesso. Cobertura > 70%.
*   **Críticos:** Zero bugs bloqueantes (Crash, Perda de Dados).
*   **Visual:** Layout responsivo sem quebras em 1366x768 e 1920x1080.

## 5. Relatórios
Os resultados dos testes automatizados são gerados na pasta `tests/results`.
