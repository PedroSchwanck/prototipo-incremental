using UnityEngine;

// D4 é um dado de quatro faces, indo de 1 a 4 (como um dado de verdade).
// Assim como o D2, ele herda toda a lógica de Dado e só define, em código,
// os valores que são específicos desse tipo de dado.
public class D4 : Dado
{
    // Tabela de dinheiro por resultado. Como ResultadoMinimo (herdado da
    // classe mãe, sem sobrescrever aqui) já vale 1 por padrão, esse dado
    // nunca sorteia 0 — vai de 1 a 4. A posição 0 do array corresponde ao
    // resultado 1, a posição 1 corresponde ao resultado 2, e assim por
    // diante (a classe mãe faz essa conta pra gente). Aqui, o valor pago
    // em dinheiro é igual ao número que caiu no dado.
    protected override double[] Recompensas => new double[] { 1, 2, 3, 4 };

    // Esse D4 demora 1.2 segundos girando antes de revelar o resultado.
    protected override float TempoGirando => 1.2f;

    // Se ninguém clicar nele por 8 segundos, ele gira sozinho.
    protected override float TempoParaGirarSozinho => 8f;

    // Não precisamos sobrescrever ResultadoMinimo, NumeroFaces,
    // ObterSpriteResultado nem SortearResultado aqui: o padrão da classe
    // mãe (ResultadoMinimo = 1, NumeroFaces = tamanho do array de sprites)
    // já é exatamente o que um D4 precisa. Você só precisa colocar 4
    // sprites no array "Sprites Resultados" no Inspector.
}