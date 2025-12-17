# Guia de Configuração em Novo Ambiente

Este guia descreve os passos para configurar o projeto **CoinCraft** em um novo computador (notebook/desktop) para fins de desenvolvimento.

## 1. Pré-requisitos

Antes de clonar o projeto, instale os seguintes softwares no novo computador:

1.  **Git**: [Baixar Git](https://git-scm.com/downloads)
2.  **SDK do .NET 8.0**: [Baixar .NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (necessário para compilar o código).
3.  **IDE (Editor de Código)**:
    - Recomendado: [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (Community é gratuito) ou [Visual Studio Code](https://code.visualstudio.com/).
    - Se usar VS Code, instale a extensão "C# Dev Kit".

## 2. Clonando o Repositório

Abra o terminal (PowerShell ou CMD) na pasta onde deseja salvar o projeto e execute:

```powershell
git clone https://github.com/Ricar66/CoinCraft.git
cd CoinCraft
```

## 3. Compilando o Projeto

Para baixar as dependências e compilar a solução, execute:

```powershell
dotnet build CoinCraft.sln
```

Se o comando finalizar sem erros, o ambiente está pronto.

## 4. Executando a Aplicação

Para rodar o CoinCraft em modo de depuração (Debug):

```powershell
dotnet run --project src/CoinCraft.App/CoinCraft.App.csproj
```

**Nota sobre o Banco de Dados:**
Ao rodar pela primeira vez, o sistema criará automaticamente o banco de dados SQLite em:
`%APPDATA%\CoinCraft\coincraft.db`
(Geralmente `C:\Users\SEU_USUARIO\AppData\Roaming\CoinCraft\coincraft.db`)

## 5. Licenciamento (Desenvolvimento)

O projeto já contém a chave pública (`public.xml`) configurada para validar licenças.
- **Se você for apenas desenvolver/testar o app:** Não é necessário fazer nada extra. Use as chaves de teste ou o modo offline se tiver os arquivos de licença.
- **Se você precisar GERAR novas licenças (Admin):** Você precisará copiar manualmente o arquivo `private.xml` (chave privada) do computador antigo para a pasta `admin_publish/` ou raiz do projeto, pois ele **não** é salvo no Git por segurança.

## 6. Gerando o Instalador (Opcional)

Se quiser gerar o executável de instalação (`SetupCoinCraft.exe`) na nova máquina, você precisará instalar o **Inno Setup**:
- [Baixar Inno Setup](https://jrsoftware.org/isdl.php)

Depois de instalado, execute o script de build:
```powershell
./installer/build_installer.ps1
```
