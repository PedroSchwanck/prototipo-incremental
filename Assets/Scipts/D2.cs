using UnityEngine;

// D2 é um dado de duas faces (0 e 1) — tipo cara/coroa. Ele herda de Dado,
// então toda a lógica de girar, animar, sortear e revelar já vem pronta.
// Aqui a gente só sobrescreve ("override") as propriedades que definem o
// que é diferente NESSE tipo de dado específico.
public class D2 : Dado
{
    // Tabela de dinheiro por resultado: resultado 0 vale 0, resultado 1
    // vale 1. A posição no array corresponde ao resultado (com o ajuste de
    // ResultadoMinimo, que aqui é 0, então resultado e índice são iguais).
    protected override double[] Recompensas => new double[] { 0, 1 };

    // Esse D2 demora 1 segundo girando antes de revelar o resultado.
    protected override float TempoGirando => 1f;

    // Se ninguém clicar nele por 8 segundos, ele gira sozinho.
    protected override float TempoParaGirarSozinho => 8f;

    // Diferente da maioria dos dados (que começam em 1, como um dado de
    // verdade), o D2 representa algo tipo cara/coroa e por isso começa em
    // 0. É por isso que precisamos sobrescrever ResultadoMinimo aqui: o
    // padrão da classe mãe é 1, e esse dado é a exceção.
    protected override int ResultadoMinimo => 0;

    // Não precisamos sobrescrever NumeroFaces, ObterSpriteResultado nem
    // SortearResultado aqui: como esse dado usa o array "spritesResultados"
    // padrão (você preenche com 2 sprites no Inspector), a implementação
    // da classe mãe já resolve tudo sozinha, olhando o tamanho desse array.
}       