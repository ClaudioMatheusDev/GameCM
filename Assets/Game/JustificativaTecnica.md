# Justificativa técnica da produção audiovisual

## Personagem e animações

O personagem é um sprite 2D animado por troca sequencial de quadros. A sprite sheet consolidada (`CharacterSpriteSheet.png`) organiza oito conjuntos de movimentação: Idle, Walk, Run, Jump, Punch, Power, Hurt e Dead. Cada linha representa uma ação e cada coluna um quadro. Os quadros originais continuam separados em `Assets/Sprites` para facilitar a importação como sprites individuais e a montagem dos Animation Clips.

No Animator, cada movimentação é um estado próprio. Idle, Walk e Run são ciclos em loop, pois representam ações contínuas. Jump, Punch, Power, Hurt e Dead são ações pontuais, com início, desenvolvimento e fim bem definidos. As transições usam o parâmetro inteiro `State`, definido pelo `PlayerController` conforme a entrada do jogador e a situação do personagem (parado, andando, correndo, pulando, atacando, ferido ou morto).

## Inimigos

Os zumbis usam quadros de caminhada, dano e morte (`Assets/Zombie`) e são animados por um componente leve (`SpriteFrameAnimator`). Existem quatro tipos: normal, rápido, resistente e um chefe na última fase. A variedade vem de multiplicadores de vida, velocidade, tamanho, dano e pontuação, além de uma cor de destaque para cada tipo. Antes de atacar, todo zumbi fica amarelo por um instante, o que dá ao jogador a chance de reagir.

## Áudio

A trilha de fundo (`AudioBackground.mp3`) toca em loop durante a partida. Os efeitos sonoros (soco, poder, pulo, dano, morte e chefe) ficam em `Assets/Game/Audio/Sons`; os sons de menu e de coleta de item são sinais curtos gerados por código. O `SfxPlayer` reproduz os efeitos com um conjunto fixo de fontes de áudio, varia levemente o tom para evitar repetição e corta cada efeito por uma duração máxima, já que os arquivos originais são longos. O volume geral é ajustável nas opções e fica salvo.

## Integração no motor

Na cena `GameScene`, o personagem recebe `SpriteRenderer`, `Animator`, `AudioSource` e `GameBootstrap`; em execução, este último adiciona `PlayerController`, `Health`, `StageManager`, `ScoreTracker`, `GameHud` e `GameMenus`. O jogador controla o personagem por teclado ou gamepad (mover, correr, pular, soco em combo e poder). Os ataques usam hitboxes (`Physics2D`) e geram tremor de câmera, pausa curta, partículas e sons, o que reforça a sensação de impacto. As telas (menu, seleção de fase, ranking, opções, créditos, pausa e resultado) são montadas em código com uGUI.

## Critérios atendidos

- Mais de seis movimentações distintas (oito estados no Animator).
- Trilha sonora em loop e efeitos sonoros integrados às ações.
- Integração completa dentro da Unity, pronta para gravar o vídeo demonstrativo com áudio.
