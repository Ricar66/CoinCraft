# Empacotamento MSIX do CoinCraft

Este documento descreve como publicar o `CoinCraft.App` como pacote MSIX usando o projeto `CoinCraft.Package` criado no diretório `src`.

## Pré-requisitos
- Windows 10/11 com Windows 10 SDK (>= 17763).
- Visual Studio 2022 com workloads: .NET Desktop e MSIX Packaging Tools.
- Certificado de assinatura (autoassinado para testes ou comercial para produção).

## Estrutura do projeto
- `src/CoinCraft.Package/CoinCraft.Package.wapproj`: projeto de empacotamento MSIX (Windows Application Packaging Project).
- `src/CoinCraft.Package/Package.appxmanifest`: manifest MSIX configurado para app desktop (FullTrust).
- `src/CoinCraft.Package/Assets/`: coloque aqui os ícones exigidos pelo manifest.

## Ícones necessários
Crie os arquivos de imagem PNG (cores sólidas ou transparentes) e coloque-os em `src/CoinCraft.Package/Assets/`:
- `Square44x44Logo.png` (mín. 44x44)
- `Square150x150Logo.png` (mín. 150x150)
- `StoreLogo.png` (mín. 50x50)

Você pode usar tamanhos maiores mantendo proporções; Visual Studio valida formatos e dimensões.

### Geração automática a partir do seu logo
Se preferir, gere os três ícones automaticamente a partir de um único arquivo do seu logo (PNG com fundo transparente recomendado):

1. Salve seu arquivo de logo em algum caminho, por exemplo: `assets\codecraft-logo.png`.
2. Execute o script:
   ```powershell
   # Execute na raiz do repositório
   .\scripts\Generate-AppIcons.ps1 -SourceLogo "assets\codecraft-logo.png"
   ```
3. O script criará `Square44x44Logo.png`, `Square150x150Logo.png` e `StoreLogo.png` em `src\CoinCraft.Package\Assets` mantendo proporções e centrando o logo.

Recomendações:
- Use imagens quadradas ou com área útil central para evitar cortes.
- Para melhor nitidez, forneça um logo base de ao menos 512×512.

## Publicar (sideloading)
1. Abra a solução no Visual Studio e selecione o projeto `CoinCraft.Package`.
2. Menu `Project → Publish → Create App Packages`.
3. Escolha `Sideloading`, arquitetura `x64`.
4. Assinatura:
   - Se possuir `.pfx`, selecione e informe a senha.
   - Para testes, gere um autoassinado no PowerShell:
     ```powershell
     New-SelfSignedCertificate -Type CodeSigning -Subject "CN=CoinCraft" -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(2)
     Get-ChildItem Cert:\CurrentUser\My | Where-Object {$_.Subject -eq "CN=CoinCraft"} | Export-PfxCertificate -FilePath "$env:USERPROFILE\Desktop\CoinCraft-CodeSigning.pfx" -Password (ConvertTo-SecureString "SenhaForteAqui" -AsPlainText -Force)
     ```
5. Gere o pacote `.msix` (ou `.msixbundle`). Opcionalmente, gere `.appinstaller` para atualizações.

## Instalação em outras máquinas
- Instale o certificado (se autoassinado) em `Usuário atual → Pessoal`. Para reduzir avisos, também em `Autoridades de Certificação Raiz Confiáveis`.
- Clique com botão direito no `.msix` → `Propriedades` → marque `Desbloquear` (se necessário) → instale.

## Observações
- O manifest usa `Windows.FullTrustApplication` + `runFullTrust` para empacotar o app WPF.
- O pacote inclui o executável do `CoinCraft.App` automaticamente via referência no projeto de empacotamento.
- Atualizações automáticas podem ser configuradas com `.appinstaller` hospedado em um servidor acessível.