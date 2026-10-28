# GameCM

Beat'em up 2D lateral feito na Unity 6 (6000.4.8f1) com URP e Input System. O personagem atravessa 4 fases e enfrenta zumbis que surgem a cada fase.

## Como jogar

Abra o projeto na Unity, abra `Assets/Scenes/GameScene.unity` e aperte Play.

| Acao | Teclado | Gamepad |
|---|---|---|
| Mover | A / D ou setas | Analogico esquerdo / D-pad |
| Correr | Shift | Gatilho direito |
| Pular | Espaco | Botao sul (A) |
| Soco | J | Botao oeste (X) |
| Poder (recarga de 2 s) | K | Botao norte (Y) |
| Pausar / voltar | Esc | Start ou botao leste (B) |
| Reiniciar (tela de resultado) | R | Select |

**Inimigos:** zumbis normais, rapidos (fase 2 em diante), resistentes (fase 3 em diante, causam 2 de dano) e, na fase 4, um **chefe** com barra de vida que faz investidas (o corpo fica vermelho antes de avancar). Zumbis mais fortes valem mais pontos.

**Combate:** aperte J de novo logo apos o impacto do soco para encadear um combo de 3 socos; o terceiro e um finalizador (mais dano, alcance e empurrao). Todo zumbi fica amarelo por um instante antes de atacar, dando tempo de pular ou golpear primeiro.

**Dificuldade (Opcoes):** Facil (mais vida, zumbis mais lentos e em menor numero), Normal e Dificil (menos vida, zumbis mais rapidos e numerosos). Vale a partir da proxima partida e altera a pontuacao.

**Modos:** *Campanha* (4 fases, chefe na ultima) e *Modo infinito* (ondas sem fim, cada vez maiores; chefe a cada 5 ondas, zumbis mais fortes com o tempo e bonus por onda vencida). Os dois tem ranking proprio.

**Camera:** o cenario e largo e a camera segue o jogador; os zumbis entram pelas bordas da tela. O fundo se repete espelhado, sem emendas.

**Power-ups (8 s):** zumbis tambem podem soltar um item colorido: raio amarelo (velocidade), seta laranja (forca, dano em dobro) ou escudo azul (invulnerabilidade). O personagem muda de cor enquanto o efeito dura e o tempo aparece no HUD.

**Controles configuraveis:** em Opcoes > Controles voce troca as teclas de esquerda, direita, pular, soco, poder e correr. Se a tecla ja estiver em uso, as duas acoes trocam de tecla. Esc fica reservado, e setas e gamepad continuam valendo.

**Itens de cura:** zumbis podem soltar um coracao (mais chance nos resistentes e quando voce esta com pouca vida). Encoste para recuperar vida; com a vida cheia o item espera no chao e some depois de alguns segundos.

**Objetivo:** elimine todos os zumbis da fase e va ate a borda direita da tela para avancar. Venca a fase 4 para ganhar. Voce tem 5 pontos de vida; zumbis causam dano ao encostar (pular ajuda a desviar) e voce fica invulneravel por um instante depois de ser atingido.

## Telas

Todas sao criadas em codigo (uGUI) por `GameMenus`, sem prefabs: **menu principal**, **selecao de fase** (fases liberam ao concluir a anterior), **como jogar**, **opcoes** (volume e tela cheia), **creditos**, **pausa**, **introducao de fase** e **resultado** (pontuacao, recorde, combo, estrelas e tempo). Volume, tela cheia, recorde e fases liberadas ficam salvos em `PlayerPrefs` (`GameProgress`).

## Pontuacao

- 100 pontos por zumbi, +25 por nivel de combo (maximo +200). Levar dano zera o combo.
- Bonus de tempo por fase (tempo-alvo = 15 s + 12 s por zumbi) e bonus de 200 pontos por ponto de vida restante na vitoria.
- Estrelas por fase: 1 por concluir, +1 sem dano, +1 dentro do tempo-alvo.

## Estrutura

| Caminho | Conteudo |
|---|---|
| `Assets/Scripts` | Codigo de jogo (assembly `GameCM.Runtime`) |
| `Assets/Editor/BuildGameAssets.cs` | Menu `Tools > GameCM > Build Sprites, Animations and Scene`: importa sprites, gera spritesheet, clips, Animator e a cena |
| `Assets/Tests/EditMode` | Testes de edicao (Test Runner da Unity) |
| `Assets/Sprites` | Frames do personagem por animacao |
| `Assets/Zombie` | Frames do zumbi (`walk_left`, `hurt/up`, `Death`) |
| `Assets/Backgrounds` | Fundos `Stage1` a `Stage4` |
| `Assets/Game/Audio` | Musica (`AudioBackground.mp3`) e efeitos em `Sons/` |

### Scripts

- `GameBootstrap`: raiz da cena. Guarda as referencias (fundos, sprites, audio) e as configuracoes serializadas, e liga os demais componentes.
- `PlayerController`: movimento, pulo, corrida, ataques (soco/poder com hitbox via `Physics2D`), dano recebido e morte.
- `ZombieController` / `ZombieSettings`: perseguicao, dano por contato, reacao a golpes, morte e fade.
- `StageManager`: fases, fundos, geracao de zumbis com object pool e estado da partida (jogando, vitoria, derrota).
- `Health`: vida e invulnerabilidade temporaria.
- `GameInput`: entrada (teclado e gamepad) montada em codigo.
- `SpriteFrameAnimator`: animacao simples por troca de sprites (usada nos zumbis).
- `GameHud`: HUD de jogo em IMGUI (vida, fase, pontos, combo, tempo).
- `GameMenus` / `UiFactory`: telas em uGUI criadas em codigo.
- `ScoreTracker` / `ScoreCalculator`: pontuacao, combo e estrelas.
- `DifficultyProfile`: ajustes de dificuldade (vida do jogador, velocidade e quantidade de zumbis, pontuacao).
- `ScoreBoard`: ranking local (top 5) em logica pura; `ScreenFader`: fade entre fases.
- `ZombieVariant` / `ZombieSpawnTable`: tipos de zumbi (normal, rapido, resistente, chefe) e sorteio por fase.
- `CameraRig` / `HitStop` / `ImpactFx`: camera que segue o jogador, tremor e pausa curta ao acertar golpes, matar zumbis, levar dano e nas investidas do chefe.
- `ScrollingBackground`: repete o fundo espelhado para cobrir o cenario largo.
- `PowerUpState` / `PowerUpPickup`: power-ups temporarios (velocidade, forca, escudo).
- `EndlessWaves`: regras das ondas do modo infinito.
- `KeyBindings`: teclas configuraveis (salvas em PlayerPrefs).
- `HealthPickup` / `PixelSprites`: item de cura (sprite gerado em codigo).
- `ParticleFx`: particulas (faiscas, sangue, poeira, cura, explosao do chefe) em um unico `ParticleSystem` compartilhado.
- `SfxPlayer`: efeitos sonoros (soco, poder, pulo, dano, morte e menus) com cortes por duracao maxima.
- `GameProgress` / `GameSession`: dados salvos e estado entre recarregamentos de cena.

## Testes

`Window > General > Test Runner > EditMode > Run All`.
