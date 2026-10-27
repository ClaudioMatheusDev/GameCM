# GameCM

Beat'em up 2D lateral feito na Unity 6 (6000.4.8f1) com URP e Input System. O personagem atravessa 4 fases e enfrenta zumbis que surgem a cada fase.

## Como jogar

Abra o projeto na Unity, abra `Assets/Scenes/CharacterDemoScene.unity` e aperte Play.

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
| `Assets/Editor/BuildCharacterDemoAssets.cs` | Menu `Tools > Character Demo > Build All Deliverables`: importa sprites, gera spritesheet, clips, Animator e a cena |
| `Assets/Tests/EditMode` | Testes de edicao (Test Runner da Unity) |
| `Assets/Sprites` | Frames do personagem por animacao |
| `Assets/Zombie` | Frames do zumbi (`walk_left`, `hurt/up`, `Death`) |
| `Assets/Backgrounds` | Fundos `Stage1` a `Stage4` |
| `Assets/CharacterDemo/Audio` | Musica (`AudioBackground.mp3`) e efeitos em `Sons/` |

### Scripts

- `CharacterDemoController`: raiz da cena. Guarda as referencias (fundos, sprites, audio) e as configuracoes serializadas, e liga os demais componentes.
- `PlayerController`: movimento, pulo, corrida, ataques (soco/poder com hitbox via `Physics2D`), dano recebido e morte.
- `ZombieController` / `ZombieSettings`: perseguicao, dano por contato, reacao a golpes, morte e fade.
- `StageManager`: fases, fundos, geracao de zumbis com object pool e estado da partida (jogando, vitoria, derrota).
- `Health`: vida e invulnerabilidade temporaria.
- `GameInput`: entrada (teclado e gamepad) montada em codigo.
- `SpriteFrameAnimator`: animacao simples por troca de sprites (usada nos zumbis).
- `GameHud`: HUD de jogo em IMGUI (vida, fase, pontos, combo, tempo).
- `GameMenus` / `UiFactory`: telas em uGUI criadas em codigo.
- `ScoreTracker` / `ScoreCalculator`: pontuacao, combo e estrelas.
- `ZombieVariant` / `ZombieSpawnTable`: tipos de zumbi (normal, rapido, resistente, chefe) e sorteio por fase.
- `CameraShake` / `HitStop` / `ImpactFx`: tremor de camera e pausa curta ao acertar golpes, matar zumbis, levar dano e nas investidas do chefe.
- `HealthPickup` / `PixelSprites`: item de cura (sprite gerado em codigo).
- `ParticleFx`: particulas (faiscas, sangue, poeira, cura, explosao do chefe) em um unico `ParticleSystem` compartilhado.
- `SfxPlayer`: efeitos sonoros (soco, poder, pulo, dano, morte e menus) com cortes por duracao maxima.
- `GameProgress` / `GameSession`: dados salvos e estado entre recarregamentos de cena.

## Testes

`Window > General > Test Runner > EditMode > Run All`.
