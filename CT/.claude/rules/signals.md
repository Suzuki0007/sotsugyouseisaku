---
paths:
  - "Assets/Script/Signal/**"
  - "Assets/Data/Signals/**"
  - "Assets/Script/Party/Bark/**"
  - "Assets/Resources/Barks/Timpani/**"
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
2. **C2 標準の合図（完了）**：
   - `Signal/StandardSignals`：ID の定数と一覧 `All`（ID・アセット名・説明。メニュー・早見表・チェックが使う）。`BeforeSceneLoad` で static イベント（TimpaniPeg・TimpaniUnit・TimpaniRoom・GuardGauge・InstantPickup・Enemy.Defeated・GameSession.AutoSaved）に登録して合図を出す（**各クラスの static イベントは SubsystemRegistration で空になるので、つなぐのは BeforeSceneLoad**）。
   - 直接出す所：`BattleArea`（Begin／Win／Lose → battle/started・won・lost）、`BarkWatcher.OnDamaged`（player/low_health〔最大の 0.34 以下〕を先に・player/damaged、同じフレームは1回）、`TimpaniRoom.Update`（戦闘中にペグが「ヒントの合図を出すまでの時間」25 秒進まない → timpani/stalled）。
   - 合図の中身は TimpaniBarks の条件に合わせた：peg_hit は締まっていないときだけ、peg_tightened はそのティンパニの最後のペグを除く、calmed は最後の1台を除く（ユニットを自分で数える。部屋の数え直しより先に呼ばれるため）、アイテムは守り／回復で分ける。
   - アセット：`Assets/Data/Signals/標準/`（18 個。Tools > 合図 > 標準の合図を作る〔足りない分〕＝`SignalMenu.CreateStandardSignalsSilently`。ID で既存を探し、メモだけ合わせ直す）。
   - 一言：`BarkTrigger.Signal = 18`（「合図が来た」）・`BarkData.signal`・`BarkContext.signal`。`Bark.HookSignals`（BeforeSceneLoad で `Signals.Sent` に登録 → `Fire(Signal)`、出せたら `MarkReceived`。subject は送り元がノア・シグルならそのキャラ、ほかは操作中のキャラ）。CSV のきっかけ名「合図」（どの合図かは Inspector で）。シーンチェック：合図が空の「合図が来た」。
3. **C3 ティンパニの一言の置き換え（完了）**：
   - TimpaniBarks の中身を一言データ `Resources/Barks/Timpani/Bark_Timpani_*`（12 個）に移し、`戦闘中の一言` オブジェクトとスクリプトを削除（レシピ `scene-recipe/recipes/stage1_3_6_barks.cs`、Stage_1_3 を保存）。条件は新しいフラグ `Flag_Timpani_InBattle`（ティンパニ戦の戦闘エリアの「戦闘中フラグ」）。
   - `BarkData.ignoreGap`（一言どうしの間を待たない。TimpaniBarks の「大事なできごと」の代わり）。`Bark.Fire` は間の途中でも ignoreGap の一言だけ選ぶ。
   - シーンチェック `CheckSignals`（合図を受けたら：合図が空・何もしない・送り元「決めた物」が空・イベントが非表示、部品「合図を出す」の合図が空）、`CheckSignalAssets`（同じ ID・標準の合図が足りない）、`CheckBarkAssets` に「合図が来た」で合図が空。
   - テストシナリオ `timpani` に、戦闘中フラグ・戦闘が始まったの一言・全部調律の合図の確認を追加。

## 約束
- 合図の ID を変えない（標準の合図は決まった ID。受ける側は ID で比べる）。
- 合図はセーブしない。状態を残すならフラグ。
- 新しく「出来事」をコードに足したら、static event だけでなく標準の合図も出す（C2 の橋渡しに足す）。
