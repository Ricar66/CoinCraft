CoinCraft - Pacote de Distribuição

Conteúdo:
- CoinCraft_x64.msix (64-bit) / CoinCraft_x86.msix (32-bit)
- CoinCraft_x64.exe (64-bit) / CoinCraft_x86.exe (32-bit)
- CoinCraft_public_der.cer (público, formato DER)
- CoinCraft_public_base64.cer (público, formato Base64)
- Install-CoinCraft.ps1 (script de instalação)
 - Install-CoinCraft.cmd (instalação em um clique)

Instalação em um clique (recomendado):
- Dê duplo clique em `Install-CoinCraft.cmd`.
- O instalador detecta automaticamente x64/x86, importa o certificado e cria o atalho.

Alternativa (UI):
- Clique direito no CoinCraft_public_der.cer > Instalar certificado
- Selecione "Usuário atual" > Avançar
- Escolha "Colocar todos os certificados no repositório a seguir"
  - Trusted People (Pessoas Confiáveis)
  - Trusted Root Certification Authorities (Raiz confiável)
- Repita para ambos os repositórios
- Abra o .msix correspondente à arquitetura

Executável autônomo (sem instalação)
- Execute diretamente `CoinCraft_x64.exe` ou `CoinCraft_x86.exe`.
- O executável é único (single-file) e já contém todas as dependências.
- Dica: crie um atalho na área de trabalho apontando para o `.exe`.

Se aparecer "arquivo inválido":
- Use o CoinCraft_public_base64.cer
- Ou importe via PowerShell:
  Import-Certificate -FilePath .\CoinCraft_public_der.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
  Import-Certificate -FilePath .\CoinCraft_public_der.cer -CertStoreLocation Cert:\CurrentUser\Root

Observações:
- Use x86 em máquinas 32-bit.
- Não compartilhe o arquivo .pfx.
 - MSIX cria entrada no Menu Iniciar; atalhos na área de trabalho não são criados automaticamente.
