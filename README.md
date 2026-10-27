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
| Reiniciar (fim de jogo) | R | Start |

**Objetivo:** elimine todos os zumbis da fase e va ate a borda direita da tela para avancar. Venca a fase 4 para ganhar. Voce tem 5 pontos de vida; zumbis causam dano ao encostar (pular ajuda a desviar) e voce fica invulneravel por um instante depois de ser atingido.

## Estrutura

| Caminho | Conteudo |
|---|---|
| `Assets/Scripts` | Codigo de jogo (assembly `GameCM.Runtime`) |
| `Assets/Editor/BuildCharacterDemoAssets.cs` | Menu `Tools > Character Demo > Build All Deliverables`: importa sprites, gera spritesheet, clips, Animator e a cena |
| `Assets/Tests/EditMode` | Testes de edicao (Test Runner da Unity) |
| `Assets/Sprites` | Frames do personagem por animacao |
| `Assets/Zombie` | Frames do zumbi (`walk_left`, `hurt/up`, `Death`) |
| `Assets/Backgrounds` | Fundos `Stage1` a `Stage4` |

### Scripts

- `CharacterDemoController`: raiz da cena. Guarda as referencias (fundos, sprites, audio) e as configuracoes serializadas, e liga os demais componentes.
- `PlayerController`: movimento, pulo, corrida, ataques (soco/poder com hitbox via `Physics2D`), dano recebido e morte.
- `ZombieController` / `ZombieSettings`: perseguicao, dano por contato, reacao a golpes, morte e fade.
- `StageManager`: fases, fundos, geracao de zumbis com object pool e estado da partida (jogando, vitoria, derrota).
- `Health`: vida e invulnerabilidade temporaria.
- `GameInput`: entrada (teclado e gamepad) montada em codigo.
- `SpriteFrameAnimator`: animacao simples por troca de sprites (usada nos zumbis).
- `GameHud`: HUD em IMGUI.

## Testes

`Window > General > Test Runner > EditMode > Run All`.
