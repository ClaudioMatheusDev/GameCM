# GameCM

Projeto Unity 6 (6000.4.8f1), 2D/URP, Input System. Leia o `README.md` para controles e estrutura.

- Codigo de jogo fica em `Assets/Scripts` (assembly `GameCM.Runtime`, sem namespace). Testes em `Assets/Tests/EditMode`.
- A cena `Assets/Scenes/GameScene.unity` e gerada por `Tools > GameCM > Build Sprites, Animations and Scene`; qualquer campo novo serializado em `GameBootstrap` deve ser atribuido tambem em `BuildGameAssets.CreateGameScene`.
- Mover/renomear assets sempre com o `.meta` junto (use `git mv`) para preservar GUIDs.
- A entrada e definida em `GameInput` (codigo); `InputSystem_Actions.inputactions` e o asset padrao do template e nao e usado pelo jogo.
- Idioma do projeto: portugues. Textos de interface (UI/HUD) usam acentos (a fonte embutida suporta); mensagens de log e comentarios ficam sem acentos.
- As telas (`GameMenus`) sao montadas em codigo com `UiFactory`; para uma tela nova, adicione um valor em `MenuScreen` e um `Build...` correspondente. Nao ha cena de menu: o menu e um overlay na `GameScene` com `Time.timeScale = 0`.
- Efeitos sonoros: toque sempre via `SfxPlayer.Play(Sfx.X)`; os clips ficam em `SoundBank` (campo `sounds` do `GameBootstrap`, atribuido tambem em `BuildGameAssets`). Os mp3 em `Sons/` sao longos, por isso `SfxPlayer` corta cada efeito por duracao maxima.
- Efeitos de impacto sempre via `ImpactFx.Play(Fx.X)`; ao pausar, `GameMenus` cancela o `HitStop` antes de mexer em `Time.timeScale`.
- Novos tipos de zumbi: adicione em `ZombieType`/`ZombieVariant.Get` e, se aparecer por sorteio, em `ZombieSpawnTable.Roll`; o chefe e gerado a parte em `StageManager`.
- Particulas sempre via `ParticleFx.X(...)` (sao seguras sem a instancia). Itens de cura nascem em `StageManager.TryDropPickup`; a chance e o valor de cura ficam em `ZombieVariant`.
- Dificuldade: `DifficultyProfile.Current` e lido uma vez por partida (em `StageManager.Configure`); ranking e estrelas ficam em `GameProgress`/`ScoreBoard`.
- Combate do jogador: combo de 3 socos em `PlayerController.StartPunch`; zumbis (exceto a investida do chefe) avisam o golpe com `attackWindup`.
- Camera: `CameraRig` segue o jogador (limites do cenario) e aplica o tremor; novos efeitos de camera passam por ele. O fundo largo e `ScrollingBackground`; o cenario tem `StageSettings.stageHalfWidth` de cada lado do centro.
- Modos: `GameSession.Mode` (campanha ou infinito) e fixado antes de recarregar a cena; em `StageManager`, no modo infinito `StageIndex` e o indice da onda (regras em `EndlessWaves`).
- Power-ups: `PowerUpState` guarda os tempos no `PlayerController`; novos tipos entram em `PowerUpType`, `PowerUpState.Label/Tint`, `PixelSprites.PowerUp` e nos efeitos do `PlayerController`.
- Teclas: `GameInput` le `KeyBindings`; ao remapear, chame `PlayerController.ReloadInput()`. Testes que mexem nelas devem trocar `GameProgress.KeyPrefix` e chamar `KeyBindings.Reload()`.
- Testes que usam `GameProgress` devem trocar `GameProgress.KeyPrefix` para nao apagar o progresso real.
