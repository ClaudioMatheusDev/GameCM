# GameCM

Projeto Unity 6 (6000.4.8f1), 2D/URP, Input System. Leia o `README.md` para controles e estrutura.

- Codigo de jogo fica em `Assets/Scripts` (assembly `GameCM.Runtime`, sem namespace). Testes em `Assets/Tests/EditMode`.
- A cena `Assets/Scenes/CharacterDemoScene.unity` e gerada por `Tools > Character Demo > Build All Deliverables`; qualquer campo novo serializado em `CharacterDemoController` deve ser atribuido tambem em `BuildCharacterDemoAssets.CreateDemoScene`.
- Mover/renomear assets sempre com o `.meta` junto (use `git mv`) para preservar GUIDs.
- A entrada e definida em `GameInput` (codigo); `InputSystem_Actions.inputactions` e o asset padrao do template e nao e usado pelo jogo.
- Idioma do projeto: portugues (sem acentos em strings de log/codigo).
- As telas (`GameMenus`) sao montadas em codigo com `UiFactory`; para uma tela nova, adicione um valor em `MenuScreen` e um `Build...` correspondente. Nao ha cena de menu: o menu e um overlay na `CharacterDemoScene` com `Time.timeScale = 0`.
- Efeitos sonoros: toque sempre via `SfxPlayer.Play(Sfx.X)`; os clips ficam em `SoundBank` (campo `sounds` do `CharacterDemoController`, atribuido tambem em `BuildCharacterDemoAssets`). Os mp3 em `Sons/` sao longos, por isso `SfxPlayer` corta cada efeito por duracao maxima.
- Efeitos de impacto sempre via `ImpactFx.Play(Fx.X)`; ao pausar, `GameMenus` cancela o `HitStop` antes de mexer em `Time.timeScale`.
- Novos tipos de zumbi: adicione em `ZombieType`/`ZombieVariant.Get` e, se aparecer por sorteio, em `ZombieSpawnTable.Roll`; o chefe e gerado a parte em `StageManager`.
- Testes que usam `GameProgress` devem trocar `GameProgress.KeyPrefix` para nao apagar o progresso real.
