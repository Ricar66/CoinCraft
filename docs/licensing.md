# Sistema de Licenças e Instalação

Este documento descreve o design e a implementação do sistema de compra/licenciamento integrado ao CoinCraft.

## 1. Mecanismo de compra
- Fluxo: usuário autenticado → chama `PurchaseLicenseAsync` → servidor gera `LicenseKey` e estado `Active` com `RemainingInstallations = 1`.
- Cliente: `LicenseWindow` expõe comando `Comprar` e armazena a chave retornada para ativação.

## 2. Proteção contra instalações múltiplas
- Coleta segura de identificadores: `MachineIdProvider` lê `MAC`, `Win32_BaseBoard.SerialNumber`, `Win32_DiskDrive.SerialNumber`.
- Fingerprint: concatena e gera SHA-256 (`CryptoHelper.ComputeSha256`).
- Vinculação: instalação registrada no servidor com `licenseKey + fingerprint`.

## 3. Validação de licença
- No startup: `LicensingService.ValidateExistingAsync`; se inválida, abre `LicenseWindow` para ativação.
- Ativação: `LicensingService.EnsureLicensedAsync` → `ValidateLicenseAsync` → `RegisterInstallationAsync` (server impede reuse).
- Persistência local: arquivo `%AppData%/CoinCraft/license.dat` criptografado com DPAPI (`CryptoHelper.Protect`).

## 4. Compras múltiplas
- Cada compra retorna nova chave independente.
- Cada chave permite 1 instalação (ou política definida pelo servidor).
- Registro central: servidor mantém `Licenses` e `Installations` (com `fingerprint`, `installedAt`).

## 5. Interface do usuário
- `LicenseWindow`: mostra `Status`, `Instalações disponíveis`, campo `Chave de licença`, botões `Ativar` e `Comprar`.
- Após ativação bem-sucedida, janela fecha e app continua.
- Gestão avançada via conta do usuário (no servidor): visualização e transferências.

## 6. Medidas de segurança
- Criptografia local: DPAPI (escopo `CurrentUser`).
- Anti-reversing: usar ofuscação no build (ex.: Dotfuscator, ConfuserEx) e integridade de binários.
- Validação periódica: `LicensingService` agenda verificação a cada 24h.
- MSIX: assinatura de pacote e isolamento ajudam contra adulteração.

## 7. Tratamento de exceções
- Hardware substituído: `TransferLicenseAsync(fromFingerprint, toFingerprint)` via servidor.
- Transferência: exige invalidação da instalação anterior e registro da nova.
- Reinstalações: servidor define limite de reativações para mesmo fingerprint (ex.: 3 vezes).

## 8. Contrato do servidor (exemplo)
- `POST /api/licenses/purchase { purchaserUserId } -> { licenseKey, remainingInstallations }`
- `POST /api/licenses/validate { licenseKey, machineFingerprint } -> { isValid, license }`
- `POST /api/licenses/register { licenseKey, machineFingerprint } -> 200/409`
- `POST /api/licenses/deactivate { licenseKey, machineFingerprint } -> 200`
- `POST /api/licenses/transfer { licenseKey, fromFingerprint, toFingerprint } -> 200/403`

## 9. Configuração
- Base URL do servidor: ajustar em `App.xaml.cs` (TODO: mover para config/appsettings).
- Certificados e MSIX: seguir `docs/msix-packaging.md`.