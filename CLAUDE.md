# GameCM

Projeto Unity 6 (6000.4.8f1), 2D/URP, Input System. Leia o `README.md` para controles e estrutura.

- Codigo de jogo fica em `Assets/Scripts` (assembly `GameCM.Runtime`, sem namespace). Testes em `Assets/Tests/EditMode`.
- A cena `Assets/Scenes/CharacterDemoScene.unity` e gerada por `Tools > Character Demo > Build All Deliverables`; qualquer campo novo serializado em `CharacterDemoController` deve ser atribuido tambem em `BuildCharacterDemoAssets.CreateDemoScene`.
- Mover/renomear assets sempre com o `.meta` junto (use `git mv`) para preservar GUIDs.
- A entrada e definida em `GameInput` (codigo); `InputSystem_Actions.inputactions` e o asset padrao do template e nao e usado pelo jogo.
- Idioma do projeto: portugues (sem acentos em strings de log/codigo).
