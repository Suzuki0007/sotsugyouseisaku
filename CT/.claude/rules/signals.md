---
paths:
  - "Assets/Script/Signal/**"
  - "Assets/Data/Signals/**"
  - "Assets/Script/Party/Bark/**"
  - "Assets/Script/Timpani/TimpaniBarks.cs"
---

# 合図（提案 C。合図・合図を受けたら・標準の合図）

手順書は `Docs/合図の使い方.md`。ユーザーと合意した設計（2026-11-14）：合図はアセット（GameSignal）で指定、ティンパニの一言は合図＋一言データに移して専用の部品（TimpaniBarks）を外す。

## 段階
1. **C1 合図の土台（完了）**：
   - `Signal/GameSignal`（ScriptableObject。ID〔空ならアセット名〕・メモ。Create > 合図 > 合図。`Assets/Data/Signals/`）。
   - `Signal/Signals`（static）：`Send(GameSignal|string id, SignalContext)`・`Sent` イベント（ID と情報）・最近 40 件の記録 `Log`（時刻・ID・出した物・反応した数〔受けた側が `MarkReceived`〕）・合図の中で合図が 8 段より深く続いたら止める。`SignalContext`（送り元の GameObject・場所・数）。`SubsystemRegistration` でリセット。
   - `Signal/SendSignalEventAction`（部品「合図を出す」。送り元＝部品のオブジェクト。EventActionCatalog「進行／合図を出す」）。
   - `Signal/SignalListener`（「合図を受けたら」。反応＝合図・送り元〔どこでも／この物と子／決めた物と子〕・GameCondition・何回まで・次に反応できるまで・遅らせる → EventSequence・フラグ・BarkData〔Bark.Play、条件を見る〕。表示中だけ受ける。回数はセーブしない）。Hierarchy 右クリック > 合図 > 合図を受けたら（`SignalMenu`）。
   - デバッグウィンドウ「合図（記録・試しに送る）」。
   - 確かめ：何回まで・送り元の絞り込み・部品から送る・記録（eval で確認）。
2. **C2 標準の合図**（このあと）：今あるプログラムの通知（TimpaniPeg・TimpaniUnit・TimpaniRoom・GuardGauge・InstantPickup・Enemy.Defeated・GameSession.AutoSaved・BattleArea・プレイヤーの Health）から合図を出す橋渡し。標準の合図のアセットを `Assets/Data/Signals/標準/` に作るメニュー。BarkData のきっかけに「合図」（`BarkTrigger` の**末尾**に足す）。
3. **C3 ティンパニの一言の置き換え**：TimpaniBarks の中身を、合図をきっかけにした一言データに変換し、部品を外す。シーンチェックに合図の点検。

## 約束
- 合図の ID を変えない（標準の合図は決まった ID。受ける側は ID で比べる）。
- 合図はセーブしない。状態を残すならフラグ。
- 新しく「出来事」をコードに足したら、static event だけでなく標準の合図も出す（C2 の橋渡しに足す）。
