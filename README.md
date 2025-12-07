# 💰 CoinCraft - Sistema de Gestão Financeira Pessoal

![Version](https://img.shields.io/badge/version-1.0.3-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20x64%2F86-lightgrey.svg)
![License](https://img.shields.io/badge/license-Proprietary-red.svg)
![Status](https://img.shields.io/badge/status-Stable-green.svg)

**CoinCraft** é uma solução robusta, intuitiva e segura para o controle financeiro pessoal. Desenvolvido para operar 100% offline, ele garante a privacidade dos seus dados enquanto oferece ferramentas poderosas para monitorar receitas, despesas, metas e patrimônio.

---

## 🚀 Funcionalidades Principais

- **📊 Dashboard Interativo:** Visão geral da saúde financeira com gráficos de fluxo de caixa, composição de despesas e evolução patrimonial.
- **💸 Gestão de Lançamentos:** Registro rápido de receitas, despesas e transferências com suporte a anexos.
- **🏦 Controle de Contas:** Gerenciamento de múltiplas contas bancárias e carteiras.
- **🔄 Automação:** Lançamentos recorrentes automáticos para contas fixas.
- **📥 Importação Inteligente:** Suporte a arquivos OFX e CSV para conciliação bancária.
- **🎯 Metas de Orçamento:** Definição de limites de gastos por categoria.
- **🔒 Segurança e Privacidade:** Dados armazenados localmente (SQLite) sem dependência de nuvem.

---

## 📚 Documentação

Para obter ajuda detalhada sobre como utilizar o sistema, consulte a documentação oficial:

- **[Manual do Usuário](docs/USER_MANUAL.md)**: Guia completo de uso.
- **[Requisitos do Sistema](docs/requisitos.md)**: Especificações técnicas.
- **[Roadmap](docs/roadmap.md)**: Planejamento de futuras atualizações.

---

## 💻 Instalação

### Pré-requisitos
- **SO:** Windows 10 (v19041+) ou Windows 11.
- **Runtime:** .NET Desktop Runtime 8.0.

### Como Instalar
1. Baixe o instalador mais recente (`SetupCoinCraft.exe`) na pasta de releases ou output.
2. Execute o instalador. Ele detectará automaticamente se seu sistema é x64 ou x86.
3. Se o .NET 8.0 não estiver instalado, o instalador irá sugerir o download.
4. Após a instalação, o ícone do CoinCraft aparecerá na sua Área de Trabalho.

---

## 🛠️ Desenvolvimento e Build

Para desenvolvedores que desejam compilar o projeto:

### Estrutura do Projeto
- `src/CoinCraft.App`: Aplicação WPF principal.
- `src/CoinCraft.Domain`: Entidades e regras de negócio.
- `src/CoinCraft.Infrastructure`: Acesso a dados (EF Core) e serviços de infra.
- `installer/`: Scripts Inno Setup e PowerShell para geração do instalador.

### Gerando uma nova versão (Build)
Utilize o script automatizado para limpar, compilar, publicar e gerar o instalador:

```powershell
./installer/build_installer.ps1
```

O instalador final será gerado em: `installer/Output/SetupCoinCraft.exe`.

---

## 📝 Histórico de Versões

- **v1.0.3** (Atual)
  - Correção: Encerramento completo do app ao fechar janela principal.
  - Melhoria: Sistema de instância única para janelas (impede múltiplas aberturas).
  - Unificação do instalador x86/x64.
- **v1.0.2**
  - Implementação de importação OFX.
  - Ajustes no Dashboard.
- **v1.0.0**
  - Lançamento inicial.

---

**CoinCraft** © 2025 CodeCraftGenz - Todos os direitos reservados.
