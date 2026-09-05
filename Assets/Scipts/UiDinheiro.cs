using TMPro;
using UnityEngine;

// Essa classe só tem uma responsabilidade: mostrar o saldo do jogador num
// texto TextMeshPro na tela, atualizando sozinha sempre que o saldo mudar.
public class UIDinheiro : MonoBehaviour
{
    // Referência ao componente de texto que vai exibir o saldo. TMP_Text é
    // o tipo genérico que serve tanto pra texto de UI (TextMeshProUGUI)
    // quanto pra texto no mundo (TextMeshPro), então esse campo aceita os
    // dois. Você arrasta o componente de texto certo pra esse campo no
    // Inspector.
    [SerializeField] private TMP_Text textoDinheiro;

    // Texto opcional que aparece antes do número, tipo "$ " ou "R$ ".
    // Fica no Inspector porque é só uma questão de estilo/idioma do jogo,
    // não uma regra de lógica.
    [SerializeField] private string prefixo = "";

    // Lista de sufixos usados pra abreviar números grandes: 1000 vira
    // "1K", 1000000 vira "1M", e assim por diante. É "static readonly"
    // porque esse array é o mesmo pra qualquer instância desse script e
    // nunca muda depois de criado — não faz sentido cada objeto ter sua
    // própria cópia dessa lista.
    private static readonly string[] Sufixos = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc" };

    private void Start()
    {
        // A gente se inscreve no evento aqui no Start, e não no Awake, por
        // um motivo específico de ordem de execução da Unity: a Unity
        // roda o Awake de TODOS os objetos da cena antes de rodar o Start
        // de qualquer um deles. Isso garante que, quando esse Start rodar,
        // o Awake do Dinheiro já rodou com certeza e Dinheiro.Instancia já
        // está definido — não importa a ordem dos objetos na Hierarchy.
        if (Dinheiro.Instancia != null)
        {
            Dinheiro.Instancia.OnDinheiroAlterado += AtualizarTexto;

            // Atualiza o texto uma vez logo de cara, pra já mostrar o
            // saldo inicial assim que o jogo começa (sem isso, o texto só
            // apareceria depois do primeiro dado pagar alguma coisa).
            AtualizarTexto(Dinheiro.Instancia.DinheiroAtual);
        }
        else
        {
            Debug.LogWarning("UIDinheiro: nenhum Dinheiro encontrado na cena.");
        }
    }

    private void OnDestroy()
    {
        // Sempre que a gente se inscreve num evento (+=), precisa se
        // desinscrever depois (-=) quando o objeto for destruído. Sem
        // isso, o evento continuaria tentando chamar AtualizarTexto num
        // objeto que já não existe mais, o que gera erro.
        if (Dinheiro.Instancia != null)
        {
            Dinheiro.Instancia.OnDinheiroAlterado -= AtualizarTexto;
        }
    }

    // Essa função é chamada automaticamente toda vez que o evento
    // OnDinheiroAlterado dispara (veja o += lá no Start). Recebe o novo
    // saldo e atualiza o texto na tela.
    private void AtualizarTexto(double novoSaldo)
    {
        if (textoDinheiro == null) return;
        textoDinheiro.text = prefixo + FormatarValor(novoSaldo);
    }

    // Transforma um número grande num texto curto usando os sufixos:
    // 1500 -> "1.5K", 2300000 -> "2.3M". É "virtual" pra você poder criar
    // uma classe filha e trocar só essa formatação (por exemplo, usando
    // notação científica pra números gigantes) sem duplicar o resto do
    // script.
    protected virtual string FormatarValor(double valor)
    {
        // Trabalhamos com o valor absoluto (sem sinal) pra não complicar a
        // lógica dos sufixos, e guardamos o sinal separado pra devolver no
        // final, caso o saldo algum dia fique negativo.
        double valorAbsoluto = System.Math.Abs(valor);
        string sinal = valor < 0 ? "-" : "";

        // Enquanto o valor for maior ou igual a 1000, dividimos por 1000 e
        // avançamos pro próximo sufixo (de "" pra "K", de "K" pra "M"...).
        // O limite (Sufixos.Length - 1) evita tentar acessar uma posição
        // que não existe no array, caso o número seja astronomicamente
        // grande.
        int indiceSufixo = 0;
        while (valorAbsoluto >= 1000 && indiceSufixo < Sufixos.Length - 1)
        {
            valorAbsoluto /= 1000;
            indiceSufixo++;
        }

        // Sem sufixo (número pequeno, tipo "850"): mostra como inteiro.
        // Com sufixo (tipo "1.5K"): mostra com até 2 casas decimais.
        string valorFormatado = indiceSufixo == 0
            ? valorAbsoluto.ToString("N0")
            : valorAbsoluto.ToString("0.##");

        return sinal + valorFormatado + Sufixos[indiceSufixo];
    }
}