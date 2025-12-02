# CoinCraft - Sistema de Gestão Financeira Pessoal

## 1. Visão Geral e Objetivos
O **CoinCraft** é uma solução robusta e intuitiva para controle financeiro pessoal, desenvolvida para ajudar usuários a monitorarem suas receitas, despesas, metas e patrimônio de forma eficiente. O sistema opera localmente (offline), garantindo total privacidade e segurança dos dados.

**Objetivos:**
- Centralizar todas as informações financeiras em um único lugar.
- Fornecer visualizações claras através de dashboards e gráficos interativos.
- Automatizar lançamentos recorrentes para facilitar a gestão.
- Permitir a importação de dados bancários para agilizar o registro.

## 2. Requisitos do Sistema
Para executar o CoinCraft, seu computador deve atender aos seguintes requisitos mínimos:
- **Sistema Operacional:** Windows 10 (versão 19041 ou superior) ou Windows 11.
- **Arquitetura:** x64.
- **Processador:** Intel Core i3 ou equivalente (Recomendado: i5 ou superior).
- **Memória RAM:** 4 GB (Recomendado: 8 GB).
- **Espaço em Disco:** 100 MB livres.
- **Framework:** .NET 8.0 (já incluído no instalador).

## 3. Instalação e Configuração
1.  **Baixe o Instalador:** Utilize o arquivo `CoinCraftSetup.exe` fornecido.
2.  **Execute a Instalação:** Dê um duplo clique no instalador e siga as instruções na tela.
3.  **Primeiro Acesso:**
    - Ao abrir o sistema pela primeira vez, o banco de dados local será criado automaticamente.
    - Você poderá cadastrar suas contas iniciais e categorias.
4.  **Licenciamento:**
    - Se solicitado, insira sua chave de ativação ou utilize o modo de avaliação, se disponível.

## 4. Guia Rápido de Utilização
-   **Dashboard:** Sua tela inicial com resumos mensais, gráficos de despesas por categoria e acompanhamento de metas.
-   **Lançamentos:** Registre receitas, despesas e transferências. Anexe comprovantes se desejar.
-   **Contas:** Gerencie suas contas bancárias e carteiras físicas. Mantenha os saldos atualizados.
-   **Recorrentes:** Cadastre contas fixas (aluguel, assinaturas) para lançamento automático.
-   **Importar:** Traga dados de extratos bancários (OFX/CSV) para o sistema.
-   **Manual do Usuário:** Acesse o botão "Ajuda" no menu principal para um guia detalhado dentro do aplicativo.

## 5. Fluxo de Atualização do Instalador (Desenvolvimento)
Para manter a consistência e evitar duplicidade de versões, o processo de geração de novas versões do instalador foi padronizado:

1.  **Localização Única:** O executável final do instalador será sempre gerado em `installer\CoinCraftSetup.exe`.
2.  **Prevenção de Duplicatas:** O script de build remove automaticamente versões anteriores e arquivos com sufixos de versão (ex: `_v4`).
3.  **Geração Automatizada:** Utilize o script PowerShell `installer\build_installer.ps1` para gerar uma nova versão. Este script:
    - Compila o projeto em modo Release.
    - Executa o compilador Inno Setup.
    - Garante que apenas o arquivo `CoinCraftSetup.exe` permaneça na pasta.
4.  **Controle de Versão:** A versão interna do software é definida no arquivo `.iss` e no projeto `.csproj`, mas o nome do arquivo do instalador permanece constante para facilitar a distribuição e links de download.

## 6. Atualizações Futuras
Estamos trabalhando constantemente para melhorar o CoinCraft. Próximas funcionalidades planejadas incluem:
-   App mobile companheiro para lançamentos rápidos.
-   Integração direta com APIs bancárias (Open Finance).
-   Módulo de investimentos avançado (Ações, FIIs, Cripto).
-   Temas personalizados.

## 7. Suporte Técnico
Para reportar bugs, sugerir melhorias ou tirar dúvidas que não estejam no FAQ:

-   **E-mail:** suporte@coincraft.com
-   **Site:** www.coincraft.com/suporte
-   **Horário de Atendimento:** Segunda a Sexta, das 9h às 18h.

## 8. Ferramentas de Desenvolvimento
- **Gerador de Licenças:** Utilize o script `generate_license.ps1` na raiz do projeto para criar chaves de licença válidas para testes offline.
  - Uso: `.\generate_license.ps1 -Fingerprint "SEU_FINGERPRINT"`

---
*CoinCraft © 2025 - Todos os direitos reservados.*
