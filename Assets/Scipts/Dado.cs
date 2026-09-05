using System.Collections;
using UnityEngine;

// Esta é a classe mãe de todos os dados do jogo. Ela é "abstract" porque não
// faz sentido ter um "Dado" genérico sozinho na cena — sempre vai ser um
// tipo específico (D2, D4...) que herda dela. É essa classe que sabe COMO
// um dado gira, sorteia e revela; a classe filha só diz O QUE é diferente
// nela (quantas faces, quanto tempo demora, quanto dinheiro cada face dá).
//
// RequireComponent obriga qualquer GameObject que use um script filho desta
// classe a ter também um SpriteRenderer (pra desenhar a face do dado) e um
// BoxCollider2D (pra detectar o clique do jogador). Sem isso a Unity nem
// deixa adicionar o script no objeto.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public abstract class Dado : MonoBehaviour
{
    // Sprite (imagem) mostrado enquanto o dado está no meio do giro, ou
    // seja, enquanto ainda não sabemos o resultado. É do tipo Sprite porque
    // é literalmente uma imagem que a Unity sabe desenhar num SpriteRenderer.
    [SerializeField] protected Sprite spriteGirando;

    // Um sprite para cada resultado possível do dado. É um array (Sprite[])
    // porque a quantidade de resultados muda de dado pra dado: um D2 usa 2
    // posições, um D4 usa 4, e assim por diante. A posição dentro do array
    // representa um resultado (com o ajuste de ResultadoMinimo, explicado
    // mais abaixo).
    [SerializeField] protected Sprite[] spritesResultados;

    // Referência ao componente SpriteRenderer do próprio objeto. Guardamos
    // essa referência numa variável em vez de chamar GetComponent toda hora
    // porque GetComponent é uma busca (mais lenta) — melhor buscar uma vez
    // só, no Awake, e reaproveitar depois.
    protected SpriteRenderer spriteRenderer;

    // Guarda qual foi o último resultado sorteado. Usamos -1 pra representar
    // "não tem resultado ainda / está girando", já que nenhum dado de
    // verdade tem uma face "-1" — isso evita precisar de uma variável bool
    // separada só pra saber se o resultado é válido.
    protected int resultadoAtual = -1;

    // Guarda se o dado está no meio da animação de giro agora. Serve pra
    // bloquear cliques repetidos e pra parar o timer de inatividade
    // enquanto ele já está girando.
    protected bool estaGirando = false;

    // Conta quantos segundos se passaram desde o último giro. É zerado
    // toda vez que o dado gira (seja por clique ou sozinho) e cresce a
    // cada frame no Update(). Quando bate no tempo limite, o dado gira
    // sozinho.
    protected float timerInatividade = 0f;

    // Guarda a referência da corrotina de giro que está rodando no momento.
    // Corrotinas em Unity não param sozinhas se você chamar Girar() de novo
    // enquanto uma já está rodando — por isso guardamos essa referência,
    // pra poder cancelar a antiga (StopCoroutine) antes de iniciar outra.
    protected Coroutine rotinaAtual;

    // Guarda a posição onde o dado está "parado" no momento. É atualizada
    // toda vez que ele termina de saltar pra um novo lugar. Usamos Vector3
    // (e não Vector2) porque é o tipo que o Transform da Unity usa pra
    // posição, mesmo em jogos 2D (o eixo Z só fica sempre em 0).
    protected Vector3 posicaoOriginal;

    // Propriedades públicas (só de leitura, sem "set") que deixam outros
    // scripts consultarem o estado do dado sem poderem alterá-lo por fora.
    // Só quem pode mudar resultadoAtual e estaGirando é a própria classe.
    public int ResultadoAtual => resultadoAtual;
    public bool EstaGirando => estaGirando;

    // Evento que qualquer outro script pode assinar pra saber quando esse
    // dado deu dinheiro, e quanto. É do tipo Action<double> porque avisa
    // "aconteceu algo" (Action) e carrega um valor double junto (o valor
    // ganho). Serve, por exemplo, pra uma UI futura mostrar um "+1"
    // flutuando ao lado do dado no momento em que ele paga.
    public event System.Action<double> OnRecompensaConcedida;

    // Quantos resultados diferentes esse dado pode sortear. Por padrão é
    // simplesmente o tamanho do array de sprites — se você colocou 4
    // sprites em spritesResultados, o dado tem 4 faces. É "virtual" (ou
    // seja, pode ser reescrita por uma classe filha) caso algum dado
    // futuro precise calcular isso de outro jeito.
    protected virtual int NumeroFaces => spritesResultados != null ? spritesResultados.Length : 0;

    // Qual é o menor número que esse dado pode sortear. Um dado de verdade
    // (D4, D6, D20...) nunca mostra "0" — começa em 1. Por isso o padrão
    // aqui é 1. O D2 é diferente (representa cara/coroa) e sobrescreve isso
    // pra 0. Cada classe filha decide esse valor sozinha, em código.
    protected virtual int ResultadoMinimo => 1;

    // Quantos segundos o dado fica "girando" (sem resultado definido) antes
    // de revelar a face sorteada. Cada tipo de dado define o próprio valor
    // sobrescrevendo essa propriedade — não é um campo do Inspector porque
    // é uma característica fixa daquele tipo de dado, não algo que deveria
    // ser ajustado manualmente objeto por objeto.
    protected virtual float TempoGirando => 1.5f;

    // Quantos segundos o dado pode ficar parado, sem ninguém clicar nele,
    // antes de começar a girar sozinho. Mesma lógica de TempoGirando: cada
    // dado define o próprio valor em código.
    protected virtual float TempoParaGirarSozinho => 10f;

    // Altura (em unidades do mundo da Unity) que o dado sobe durante o
    // giro, simulando ele sendo jogado pra cima. "Unidades do mundo" e não
    // "pixels" porque em 2D a Unity trabalha com uma grade de unidades que
    // depende do tamanho do sprite e da câmera, não com pixels da tela.
    protected virtual float AlturaSalto => 0.4f;

    // Distância máxima (pra qualquer lado, esquerda ou direita) que o dado
    // pode se deslocar horizontalmente durante um giro. A cada giro é
    // sorteado um valor novo dentro desse limite, então o dado "anda" pela
    // cena de um jeito diferente a cada clique.
    protected virtual float DistanciaHorizontalMaxima => 0.3f;

    // Tabela que traduz "resultado sorteado" em "quanto dinheiro ganhar".
    // É um array de double (não int ou float) porque em jogo incremental
    // os números costumam crescer muito e o double aguenta valores bem
    // maiores sem perder precisão. O padrão aqui é null, ou seja, "esse
    // dado não dá dinheiro nenhum" — cada dado filho define sua própria
    // tabela sobrescrevendo essa propriedade.
    protected virtual double[] Recompensas => null;

    // Consulta a tabela Recompensas pra saber quanto dinheiro um resultado
    // específico vale. Recebemos "resultado" (que pode começar em 0 ou em
    // 1, dependendo do ResultadoMinimo do dado) e convertemos pra um índice
    // de array válido (que sempre começa em 0) subtraindo ResultadoMinimo.
    // É "virtual" pra permitir, no futuro, uma lógica mais complexa (tipo
    // multiplicar a recompensa por um upgrade que o jogador comprou).
    protected virtual double ObterRecompensa(int resultado)
    {
        int indice = resultado - ResultadoMinimo;

        // Se a tabela não existe, ou o índice calculado está fora dos
        // limites do array, não tem recompensa — retornamos 0 em vez de
        // deixar o jogo travar com um erro de "índice fora do array".
        if (Recompensas == null || indice < 0 || indice >= Recompensas.Length)
        {
            return 0;
        }

        return Recompensas[indice];
    }

    // Awake roda uma vez, assim que o objeto é criado, antes de qualquer
    // Start ou Update. É o lugar certo pra buscar componentes (como o
    // SpriteRenderer) e guardar valores iniciais, porque garantidamente já
    // rodou antes de qualquer outra lógica do jogo tentar usar esse dado.
    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        posicaoOriginal = transform.position;
    }

    // Start roda depois do Awake de todos os objetos da cena já terem
    // rodado. Aqui a gente já sorteia um resultado inicial (pra o dado não
    // começar "girando" sem motivo) e atualiza o sprite pra mostrar esse
    // resultado assim que o jogo começa.
    protected virtual void Start()
    {
        resultadoAtual = SortearResultado();
        AtualizarSprite();
        ResetarTimerInatividade();
    }

    // Update roda uma vez por frame, o tempo todo, enquanto o jogo está
    // rodando. Aqui só usamos ele pra contar o tempo parado do dado: se
    // ele não está girando, soma o tempo do frame no timer, e se o timer
    // passar do limite (TempoParaGirarSozinho), o dado gira sozinho.
    protected virtual void Update()
    {
        if (estaGirando) return;

        timerInatividade += Time.deltaTime;
        if (timerInatividade >= TempoParaGirarSozinho)
        {
            Girar();
        }
    }

    // OnMouseDown é uma função especial da Unity: ela é chamada
    // automaticamente quando o jogador clica em cima de um Collider2D
    // (nesse caso, o BoxCollider2D do dado), sem precisar escrever nenhum
    // código de detecção de clique manualmente.
    protected virtual void OnMouseDown()
    {
        if (estaGirando) return;
        Girar();
    }

    // Função pública que qualquer coisa pode chamar pra fazer o dado girar
    // (seja o clique do jogador, seja o timer de inatividade). É "virtual"
    // porque um dado filho pode querer fazer algo a mais quando gira (por
    // exemplo, um efeito visual extra), chamando base.Girar() no final pra
    // não perder o comportamento padrão.
    public virtual void Girar()
    {
        if (estaGirando) return;

        ResetarTimerInatividade();

        // Se já existia uma corrotina de giro rodando (não deveria
        // acontecer, já que bloqueamos clique durante o giro, mas é uma
        // proteção extra), cancelamos ela antes de começar outra.
        if (rotinaAtual != null)
        {
            StopCoroutine(rotinaAtual);
        }

        rotinaAtual = StartCoroutine(RotinaGirar());
    }

    // Uma corrotina (IEnumerator + yield) é a forma que a Unity usa pra
    // rodar um código "espalhado" ao longo de vários frames, sem travar o
    // jogo esperando. Aqui a corrotina: entra no estado "girando", espera
    // (através da animação de salto) o tempo de giro passar, sorteia um
    // resultado novo, revela ele, e se aquele resultado dá dinheiro, avisa
    // o gerenciador de dinheiro.
    protected virtual IEnumerator RotinaGirar()
    {
        estaGirando = true;
        resultadoAtual = -1; // -1 = ainda não tem resultado, está girando
        AtualizarSprite();

        // Essa linha SUBSTITUI o antigo "esperar X segundos": em vez de só
        // esperar parado, a gente espera rodando a animação de salto, que
        // já demora exatamente TempoGirando segundos pra terminar.
        yield return StartCoroutine(AnimarSalto());

        resultadoAtual = SortearResultado();
        estaGirando = false;
        AtualizarSprite();

        // Depois de revelar o resultado, consultamos quanto dinheiro ele
        // vale e, se for diferente de zero, avisamos o Dinheiro (o
        // gerenciador de saldo) pra somar esse valor.
        double recompensa = ObterRecompensa(resultadoAtual);
        if (recompensa != 0 && Dinheiro.Instancia != null)
        {
            Dinheiro.Instancia.AdicionarDinheiro(recompensa);
            OnRecompensaConcedida?.Invoke(recompensa);
        }

        rotinaAtual = null;
    }

    // Move o dado como se ele tivesse sido jogado: sobe e desce (fazendo um
    // arco, tipo uma bolinha jogada pra cima) enquanto desliza pro lado, e
    // fica pousado na nova posição — ele NÃO volta pro lugar de onde saiu.
    // A cada giro, a direção e a distância são sorteadas de novo a partir
    // de onde o dado está agora, então ele vai "andando" pela cena aos
    // poucos, de um jeito diferente a cada clique.
    protected virtual IEnumerator AnimarSalto()
    {
        Vector3 posicaoInicial = posicaoOriginal;

        // Sorteia um deslocamento horizontal entre -DistanciaHorizontalMaxima
        // e +DistanciaHorizontalMaxima. Negativo desloca pra esquerda,
        // positivo desloca pra direita.
        float direcaoX = UnityEngine.Random.Range(-DistanciaHorizontalMaxima, DistanciaHorizontalMaxima);
        Vector3 posicaoDestino = posicaoInicial + new Vector3(direcaoX, 0f, 0f);

        float tempoDecorrido = 0f;

        // Esse laço roda um frame por vez (por causa do "yield return null"
        // no final), até o tempo decorrido alcançar TempoGirando. É assim
        // que se faz uma animação suave em Unity sem travar o resto do jogo.
        while (tempoDecorrido < TempoGirando)
        {
            tempoDecorrido += Time.deltaTime;

            // progresso vai de 0 (começo da animação) até 1 (fim). Clamp01
            // garante que nunca passa de 1, mesmo se o frame demorar mais
            // que o esperado.
            float progresso = Mathf.Clamp01(tempoDecorrido / TempoGirando);

            // O eixo X desliza de forma linear (Lerp) do ponto inicial até
            // o destino sorteado, conforme o progresso avança.
            float x = Mathf.Lerp(posicaoInicial.x, posicaoDestino.x, progresso);

            // O eixo Y usa Mathf.Sin(progresso * PI), que começa em 0,
            // sobe até 1 na metade da animação, e volta pra 0 no final —
            // ou seja, faz o dado subir e descer, terminando na mesma
            // altura de onde começou (como uma jogada de verdade, que
            // sempre acaba caindo no chão).
            float curvaAltura = Mathf.Sin(progresso * Mathf.PI);
            float y = posicaoInicial.y + AlturaSalto * curvaAltura;

            transform.position = new Vector3(x, y, posicaoInicial.z);

            yield return null; // espera o próximo frame antes de continuar o laço
        }

        // Ao terminar, fixamos a posição exatamente no destino (evita
        // pequenos erros de arredondamento acumulados) e atualizamos
        // posicaoOriginal, que passa a ser o novo ponto de partida do
        // próximo giro.
        posicaoOriginal = posicaoDestino;
        transform.position = posicaoOriginal;
    }

    // Sorteia um número inteiro dentro do espaço amostral do dado.
    // Random.Range(min, max) com inteiros sorteia entre min (incluso) e
    // max (EXCLUÍDO) — por isso somamos NumeroFaces ao ResultadoMinimo:
    // isso faz o sorteio cobrir exatamente as faces válidas do dado.
    // Exemplo: D4 com ResultadoMinimo = 1 e NumeroFaces = 4 sorteia entre
    // 1 (incluso) e 5 (excluído), ou seja: 1, 2, 3 ou 4.
    protected virtual int SortearResultado()
    {
        return UnityEngine.Random.Range(ResultadoMinimo, ResultadoMinimo + NumeroFaces);
    }

    // Decide qual sprite mostrar de acordo com o estado atual do dado: se
    // está girando (ou ainda não tem resultado), mostra o sprite de
    // "girando"; senão, busca o sprite correspondente ao resultado atual.
    protected virtual void AtualizarSprite()
    {
        if (spriteRenderer == null) return;

        if (estaGirando || resultadoAtual < 0)
        {
            spriteRenderer.sprite = spriteGirando;
        }
        else
        {
            spriteRenderer.sprite = ObterSpriteResultado(resultadoAtual);
        }
    }

    // Traduz um número de resultado pro sprite correspondente dentro do
    // array spritesResultados. Igual ObterRecompensa, precisamos subtrair
    // ResultadoMinimo pra transformar o "resultado real" (que pode começar
    // em 1) num índice de array válido (que sempre começa em 0).
    protected virtual Sprite ObterSpriteResultado(int resultado)
    {
        int indice = resultado - ResultadoMinimo;

        if (spritesResultados == null || indice < 0 || indice >= spritesResultados.Length)
        {
            return spriteGirando;
        }

        return spritesResultados[indice];
    }

    // Zera o contador de inatividade. Chamada toda vez que o dado gira,
    // seja por clique do jogador ou porque o próprio timer estourou.
    protected void ResetarTimerInatividade()
    {
        timerInatividade = 0f;
    }
}