CoinCraft – Instruções de Uso (Teste)

1) Conteúdo do pacote
- CoinCraft.App.exe: executável principal (self-contained, Windows x64).
- SkipLicense.cmd: inicia o app com a licença temporariamente ignorada.
- DLLs e arquivos de suporte: necessários para rodar sem instalar .NET.

2) Como executar
- Descompacte o .zip para uma pasta local (não execute de dentro do .zip).
- Duas opções:
  a) Teste sem licença: execute "SkipLicense.cmd".
     - Ele define a variável de ambiente COINCRAFT_SKIP_LICENSE=1 e inicia o app.
     - Use esta opção para validar a UI e fluxos sem bloqueio de ativação.
  b) Execução normal: execute "CoinCraft.App.exe" diretamente.
     - Se não houver licença válida, a janela de licença será exibida.

3) Ativação de licença (execução normal)
- Cole sua chave de licença na janela de licença e clique em Ativar.
- Primeira ativação requer internet para validar e registrar a instalação.
- O app armazena a licença localmente de forma segura.

4) Problemas comuns
- SmartScreen: se o Windows avisar que o app é de editor desconhecido,
  clique em "Mais informações" → "Executar mesmo assim".
- Sem internet na primeira ativação: a ativação pode falhar; use SkipLicense.cmd
  para testes ou tente novamente quando a conexão estiver disponível.

5) MSIX (opcional)
- Para instalar via MSIX com assinatura de certificado, siga docs\msix-packaging.md.
- O Publisher do manifesto está alinhado com o certificado de desenvolvimento (CN=localhost).

6) Suporte
- Em caso de dúvidas ou necessidade de reenvio de licença, entre em contato
  pelo canal informado no e-mail de compra.