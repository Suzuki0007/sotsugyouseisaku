---
paths:
  - "Assets/Script/Attack/AreaAttack.cs"
  - "Assets/Script/Attack/AttackController.cs"
  - "Assets/Script/Attack/Player/AreaAttackController.cs"
  - "Assets/Script/Attack/Player/TimingGaugeAttackController.cs"
  - "Assets/Script/Bullet/**"
  - "Assets/Script/Camera/FocusCamera.cs"
  - "Assets/Script/CharaBase/**"
  - "Assets/Script/CheckPoint/CheckPoint.cs"
  - "Assets/Script/ComponentBase/**"
  - "Assets/Script/Data/NoahData.cs"
  - "Assets/Script/Debug/CircleDebugRenderer.cs"
  - "Assets/Script/Enemy/EnemyDead.cs"
  - "Assets/Script/Enemy/EnemyMovement.cs"
  - "Assets/Script/Enemy/EnemyShooter.cs"
  - "Assets/Script/Event/EventProcess/EventAction.cs"
  - "Assets/Script/Event/EventProcess/EventSequence.cs"
  - "Assets/Script/Event/EventZone.cs"
  - "Assets/Script/Fps/Fps.cs"
  - "Assets/Script/Health/Health.cs"
  - "Assets/Script/Intaract/Intaractable.cs"
  - "Assets/Script/kari/**"
  - "Assets/Script/Key/KeyItem.cs"
  - "Assets/Script/Manager/InventoryManager.cs"
  - "Assets/Script/Manager/PlayerSwitchManager.cs"
  - "Assets/Script/Map/MapBounds.cs"
  - "Assets/Script/ObjectBase/**"
  - "Assets/Script/Player/FocusPlayer.cs"
  - "Assets/Script/Player/Player_Noah/**"
  - "Assets/Script/Player/Player_Sigle/**"
  - "Assets/Script/Player/PlayerFollower.cs"
  - "Assets/Script/Player/PlayerHistory.cs"
  - "Assets/Script/Player/PlayerInput.cs"
  - "Assets/Script/Player/PlayerMapLimit.cs"
  - "Assets/Script/Player/PlayerMovement.cs"
  - "Assets/Script/Player/PlayerRuntimeInitializer.cs"
  - "Assets/Script/Player/PlayerSceneContext.cs"
  - "Assets/Script/Pool/**"
  - "Assets/Script/Save/AutoSave.cs"
  - "Assets/Script/Save/Save.cs"
  - "Assets/Script/Scene/SceneChange.cs"
  - "Assets/Script/Singleton/**"
  - "Assets/Script/Spawner/Enemy/EnemySpawner.cs"
  - "Assets/Script/Spawner/ObjectSpawner.cs"
  - "Assets/Script/Spawner/Player/PlayerPartySpawner.cs"
  - "Assets/Script/Spawner/SpawnedObject.cs"
  - "Assets/Script/SpriteAnimation/SpriteAnimation.cs"
  - "Assets/Script/StateMachine/**"
  - "Assets/Script/Talk/Dialogue.cs"
  - "Assets/Script/Talk/DialogueEventAction.cs"
  - "Assets/Script/Treasure/Treasure.cs"
  - "Assets/Script/UI/AttackArcGauge/TimingGaugeUI.cs"
  - "Assets/Script/UI/Life/LifeUIController.cs"
---

# ほかの人が書いたコード（これらを触るとき）

ここに並んだファイルは、ファイル先頭の `\author` が **成田悠真さん**（約 55 本）か **鈴木裕稀さん**（約 12 本）、または2人の共同。ユーザー（uh）のファイルは `\author` が空。
（2026-11-14 に `\author` から作った一覧。ファイルが増えたら下の「一覧の作り直し」で直す。）

## 約束
- **勝手に直さない**。問題を見つけたら報告し、`Docs/code-map.md` の「4. 問題点」に書く（頼まれたら直す）。
- 直したときは、**コミットメッセージにその人への説明**を書く：どのファイルの何を・なぜ・その人の作業への影響。`changes.ps1` が作者を表示する（`finish-task` スキル）。
- `\author` は「最初に書いた人」。ユーザーが大きく手を入れたファイルも多い（code-map の ◎）。誰が変えたかは `changes.ps1 -Log -Path <ファイル>` か `git log -- <ファイル>` で確かめる。
- 名前や引数を変えると、その人のブランチ（`narita` など）とぶつかりやすい。**公開しているメソッド・SerializeField の名前は変えない**（Inspector の値が消える）。足すほうで直す。
- 使われていない古いもの（`StateMachine/*`・`ComponentBase`・`FocusPlayer`・`kari/*`）を消すのは提出前の整理で、ユーザーが決める。

## よく触るもの
- `EventSequence`・`EventAction`（イベントの部品の土台。成田さん）：部品を足すのは自由（`event-part` スキル）。土台を変えるときは先に方針を見せる。
- `PlayerMovement`・`PlayerFollower`・`PlayerHistory`・`PlayerMapLimit`（成田さん）：LateUpdate の順番が決めてある（`rules/player-combat.md`）。
- `Save.cs`・`AutoSave.cs`（鈴木さん）：セーブのキーの一覧は `rules/save-ui.md`。

## 一覧の作り直し
```powershell
Get-ChildItem Assets/Script -Recurse -Filter *.cs | ForEach-Object { $l = Get-Content $_.FullName -TotalCount 12 -Encoding UTF8 | Where-Object { $_ -match '\\author' } | Select-Object -First 1; if ($l -and ($l -replace '^.*\\author','').Trim()) { $_.FullName } }
```
