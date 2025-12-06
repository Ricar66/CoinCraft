# Identidade Visual - CoinCraft

## Ícone do Aplicativo (Moeda Dourada)

O ícone do CoinCraft representa prosperidade e solidez, utilizando uma moeda dourada estilizada com o símbolo monetário.

### Especificações de Cores

| Elemento | Cor (Nome) | HEX | RGB |
|----------|------------|-----|-----|
| **Gradiente (Centro)** | Light Goldenrod | `#FFF096` | `255, 240, 150` |
| **Gradiente (Borda)** | Goldenrod | `#DAA520` | `218, 165, 32` |
| **Borda Externa** | Dark Goldenrod | `#B8860B` | `184, 134, 11` |
| **Símbolo ($)** | Dark Green | `#006400` | `0, 100, 0` |
| **Brilho Interno** | White (Transparent) | `#64FFFFFF` | `255, 255, 255` (Alpha ~40%) |
| **Sombra do Texto** | Black (Transparent) | `#32000000` | `0, 0, 0` (Alpha ~20%) |

### Tipografia

- **Fonte do Símbolo**: Segoe UI Bold
- **Tamanho da Fonte**: 60% do tamanho total do ícone

### Dimensões e Proporções

O ícone é gerado programaticamente a partir de um canvas base de 512x512 pixels.

- **Círculo Principal**: Diâmetro de 510px (centralizado).
- **Borda**: Espessura de ~3% do tamanho total.
- **Margem de Segurança**: O conteúdo principal está contido dentro de um círculo seguro para evitar cortes em máscaras circulares (comum em Android/iOS/Web).

### Arquivos Gerados

Os seguintes arquivos estão disponíveis em `src/CoinCraft.App/`:

- `coincraft.ico`: Arquivo de ícone do Windows contendo os tamanhos:
  - 16x16, 32x32, 48x48, 64x64, 128x128, 256x256, 512x512.
  - Inclui versões PNG comprimidas para tamanhos grandes (256+).
- `coincraft.png`: Versão rasterizada em alta resolução (512x512) com transparência.
- `coincraft.svg`: Versão vetorial escalável para uso em web ou impressão.
