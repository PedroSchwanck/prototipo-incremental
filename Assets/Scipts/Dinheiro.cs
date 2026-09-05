using System;
using UnityEngine;

// Essa classe gerencia o dinheiro do jogador. Ela é um "singleton": existe
// só uma instância dela na cena inteira, e qualquer outro script acessa
// essa instância através de Dinheiro.Instancia, sem precisar de uma
// referência direta (tipo arrastar ela num campo do Inspector) — isso
// evita ter que conectar manualmente cada dado, cada loja, cada upgrade a
// esse gerenciador.
public class Dinheiro : MonoBehaviour
{
    // "static" significa que essa propriedade pertence à CLASSE, não a um
    // objeto específico — por isso dá pra acessar como Dinheiro.Instancia
    // de qualquer lugar do código, sem precisar ter uma referência a um
    // GameObject. O "private set" garante que só esta própria classe pode
    // decidir qual é a instância (protege contra outro script mudar isso
    // por engano).
    public static Dinheiro Instancia { get; private set; }

    // Valor inicial de dinheiro que o jogador começa tendo. Fica no
    // Inspector porque, diferente dos parâmetros dos dados, esse é um
    // valor de configuração do jogo que faz sentido poder ajustar sem
    // mexer em código (por exemplo, pra testes).
    [SerializeField] private double dinheiroInicial = 0;

    // Guarda o saldo atual do jogador. É "private" (sem [SerializeField])
    // porque ninguém de fora deve poder alterar isso diretamente — toda
    // alteração tem que passar pelos métodos AdicionarDinheiro ou
    // GastarDinheiro, que cuidam de avisar quem precisa saber que o saldo
    // mudou.
    private double dinheiroAtual;

    // double (em vez de int ou float) porque em jogo incremental os
    // números de dinheiro crescem muito rápido e podem ficar enormes —
    // double consegue guardar valores bem maiores sem estourar.
    public double DinheiroAtual => dinheiroAtual;

    // Evento que outros scripts podem assinar pra saber quando o saldo
    // muda. Action<double> significa "um evento que não retorna nada, mas
    // carrega um valor double junto" — nesse caso, o novo saldo total.
    // Exemplo de uso em outro script:
    // Dinheiro.Instancia.OnDinheiroAlterado += AtualizarTexto;
    public event Action<double> OnDinheiroAlterado;

    private void Awake()
    {
        // Proteção do padrão singleton: se já existe uma instância desse
        // gerenciador (por exemplo, porque essa cena carregou de novo e já
        // tinha um Dinheiro sobrevivendo de antes), destruímos essa cópia
        // nova e paramos aqui, pra nunca ter dois gerenciadores de dinheiro
        // ativos ao mesmo tempo.
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;

        // Faz esse objeto sobreviver a trocas de cena, já que o saldo do
        // jogador precisa continuar existindo mesmo trocando de tela.
        DontDestroyOnLoad(gameObject);

        dinheiroAtual = dinheiroInicial;
    }

    // Soma dinheiro ao saldo atual. Também aceita valores negativos, o que
    // funciona como "remover" dinheiro, caso algum sistema futuro precise.
    public void AdicionarDinheiro(double valor)
    {
        // Se o valor for 0, não faz sentido disparar o evento de "saldo
        // mudou" — nada realmente mudou.
        if (valor == 0) return;

        dinheiroAtual += valor;
        OnDinheiroAlterado?.Invoke(dinheiroAtual);
    }

    // Tenta gastar uma quantidade de dinheiro (por exemplo, ao comprar um
    // upgrade no futuro). Retorna bool: true se conseguiu gastar (tinha
    // saldo suficiente), false se não conseguiu — assim quem chamou esse
    // método sabe se a compra deve ou não acontecer.
    public bool GastarDinheiro(double valor)
    {
        if (valor < 0 || dinheiroAtual < valor) return false;

        dinheiroAtual -= valor;
        OnDinheiroAlterado?.Invoke(dinheiroAtual);
        return true;
    }
}