CoinCraft — Distribuição 1.0.0.7

Conteúdo de distribuição:
- Executáveis portáteis para teste rápido:
  - `CoinCraft_win-x64.exe`
  - `CoinCraft_win-x86.exe`
- Pacote MSIX (opcional):
  - `CoinCraft_1.0.0.7.msixbundle`
  - `CoinCraft.msixbundle.map` (mapa de componentes do bundle)

Certificados:
- Os certificados públicos foram centralizados no repositório em `scripts/certs`.
- Utilize `scripts/certs/CoinCraft_public_der.cer` (preferencial) ou `scripts/certs/CoinCraft_public_base64.cer`.

Execução imediata (executável portátil):
- Basta executar `CoinCraft_win-x64.exe` (ou `CoinCraft_win-x86.exe`).
- Não requer instalação nem privilégios elevados; ideal para validação do Dashboard.

Instalação MSIX (um clique):
- Execute `scripts/Install-CoinCraft.cmd` e siga as instruções.
- O script valida o Publisher do pacote e importa o certificado correto a partir de `scripts/certs`.
- Ao final, o app é ativado via `shell:AppsFolder` (Menu Iniciar).

Instalação manual (GUI):
1) Instalar certificado público correspondente ao Publisher do pacote (manifest):
   - Duplo clique em `scripts/certs/CoinCraft_public_der.cer` > "Usuário atual" > Avançar
   - Selecionar repositório: `Trusted People` (Pessoas Confiáveis)
   - Concluir. (Se precisar, repetir também em `Trusted Root`)
2) Abrir `CoinCraft_1.0.0.7.msixbundle` e instalar.
3) Iniciar o app pelo Menu Iniciar (tile do CoinCraft).

Primeiro uso:
- O app cria e migra o banco local, aplica seeds e views; isso pode levar alguns segundos.
- Logs em `%AppData%\CoinCraft\logs\AAAA-MM-DD.log` (diagnóstico de inicialização).

Observações de versão:
- Esta pasta contém artefatos da versão `1.0.0.7`.
- Executáveis portáteis x64/x86 são fornecidos para testes e validação rápida.
- Não incluir `.pfx` na distribuição; assinaturas são gerenciadas por secrets do pipeline.
