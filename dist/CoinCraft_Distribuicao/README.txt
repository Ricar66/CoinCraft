CoinCraft - Pacote de Distribuição

Conteúdo:
- CoinCraft_x64.msix (64-bit) / CoinCraft_x86.msix (32-bit)
- CoinCraft_public_der.cer (público, formato DER)
- CoinCraft_public_base64.cer (público, formato Base64)
- Install-CoinCraft.ps1 (script de instalação)

Como instalar (PowerShell - recomendado):
1) Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
2) .\Install-CoinCraft.ps1 -MsixPath .\CoinCraft_x64.msix -CertPath .\CoinCraft_public_der.cer

Alternativa (UI):
- Clique direito no CoinCraft_public_der.cer > Instalar certificado
- Selecione "Usuário atual" > Avançar
- Escolha "Colocar todos os certificados no repositório a seguir"
  - Trusted People (Pessoas Confiáveis)
  - Trusted Root Certification Authorities (Raiz confiável)
- Repita para ambos os repositórios
- Abra o .msix correspondente à arquitetura

Se aparecer "arquivo inválido":
- Use o CoinCraft_public_base64.cer
- Ou importe via PowerShell:
  Import-Certificate -FilePath .\CoinCraft_public_der.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
  Import-Certificate -FilePath .\CoinCraft_public_der.cer -CertStoreLocation Cert:\CurrentUser\Root

Observações:
- Use x86 em máquinas 32-bit.
- Não compartilhe o arquivo .pfx.
