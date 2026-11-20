# Stage1_4 の仕掛けの流れ（自動）

Tools > 手順書 > 仕掛けの流れ図を作り直す で作る。直接直さない。四角＝物（きっかけ）、丸＝フラグ、六角＝持ち物、平行四辺形＝仕掛けの状態。赤＝条件がそろわない。

## 注意
- なし

## 図
```mermaid
flowchart LR
  classDef ng fill:#fdd,stroke:#c00
  n74["E1 入ったとき（1回だけ）<br/>（着いたとき）"]
  f0(["Flag_TC_EnterSeen"])
  n75["シグル（NPC）<br/>（調べる）"]
  f1(["Flag_TC_DoorEventDone"])
  f2(["Flag_TC_WestPipeOn"])
  f3(["Flag_TC_EastPipeOn"])
  n76["シグル（NPC） #2<br/>（調べる）"]
  n77["シグル（NPC） #3<br/>（調べる）"]
  n78["E2 床の震えの範囲（仮 y=700）<br/>（入ると）"]
  f4(["Flag_TC_Rumble"])
  n79["E3 開始ライン（y=650）<br/>（入ると）"]
  f5(["Flag_TC_NoteGlowed"])
  n80["扉を調べる（前に立って E）<br/>（調べる）"]
  n81["扉を調べる（前に立って E） #2<br/>（調べる）"]
  n82["扉を調べる（前に立って E） #3<br/>（調べる）"]
  n83["西の管を調べる（壁の前に立って E）<br/>（調べる）"]
  n84["西の管を調べる（壁の前に立って E） #2<br/>（調べる）"]
  n85["東の管を調べる（壁の前に立って E）<br/>（調べる）"]
  n86["東の管を調べる（壁の前に立って E） #2<br/>（調べる）"]
  n87["南の出口（書斎へ戻る）<br/>（入ると）"]
  n88["西の出口（西の部屋はまだない：受け口）<br/>（入ると）"]
  n89["東の出口（東の部屋はまだない：受け口）<br/>（入ると）"]
  n90["音符の光：E3 で触れたら表示したまま<br/>（見た目）"]
  n91["西のランプ：西の管に伝わったら点く<br/>（見た目）"]
  n92["東のランプ：東の管に伝わったら点く<br/>（見た目）"]
  n93["西の管の震え<br/>（見た目）"]
  n94["東の管の震え<br/>（見た目）"]
  n95["シグル：E3 から南向き<br/>（見た目）"]
  f0 -.->|OFF のとき| n74
  n74 -->|ON| f0
  f1 --> n75
  f2 -.->|OFF のとき| n75
  f3 -.->|OFF のとき| n75
  f1 --> n76
  f2 --> n76
  f3 -.->|OFF のとき| n76
  f1 --> n77
  f2 -.->|OFF のとき| n77
  f3 --> n77
  f4 -.->|OFF のとき| n78
  f1 -.->|OFF のとき| n78
  n78 -->|ON| f4
  f1 -.->|OFF のとき| n79
  n79 -->|ON| f5
  n79 -->|ON| f1
  f1 --> n80
  f2 -.->|OFF のとき| n80
  f3 -.->|OFF のとき| n80
  f1 --> n81
  f2 --> n81
  f3 -.->|OFF のとき| n81
  f1 --> n82
  f2 -.->|OFF のとき| n82
  f3 --> n82
  f2 -.->|OFF のとき| n83
  f2 --> n84
  f3 -.->|OFF のとき| n85
  f3 --> n86
  n87 --> mv87>"→ Stage1_3"]
  f1 --> n88
  f1 --> n89
  f5 --> n90
  f2 --> n91
  f3 --> n92
  f2 --> n93
  f3 --> n94
  f1 --> n95
```

## 表
| 物 | きっかけ | 条件 | 結果 |
|---|---|---|---|
| E1 入ったとき（1回だけ） | 着いたとき | Flag_TC_EnterSeen が OFF | Flag_TC_EnterSeen ON |
| シグル（NPC） | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn が OFF、Flag_TC_EastPipeOn が OFF | （会話・演出だけ） |
| シグル（NPC） #2 | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn、Flag_TC_EastPipeOn が OFF | （会話・演出だけ） |
| シグル（NPC） #3 | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn が OFF、Flag_TC_EastPipeOn | （会話・演出だけ） |
| E2 床の震えの範囲（仮 y=700） | 入ると | Flag_TC_Rumble が OFF、Flag_TC_DoorEventDone が OFF | Flag_TC_Rumble ON |
| E3 開始ライン（y=650） | 入ると | Flag_TC_DoorEventDone が OFF | Flag_TC_NoteGlowed ON、Flag_TC_DoorEventDone ON |
| 扉を調べる（前に立って E） | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn が OFF、Flag_TC_EastPipeOn が OFF | （会話・演出だけ） |
| 扉を調べる（前に立って E） #2 | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn、Flag_TC_EastPipeOn が OFF | （会話・演出だけ） |
| 扉を調べる（前に立って E） #3 | 調べる | Flag_TC_DoorEventDone、Flag_TC_WestPipeOn が OFF、Flag_TC_EastPipeOn | （会話・演出だけ） |
| 西の管を調べる（壁の前に立って E） | 調べる | Flag_TC_WestPipeOn が OFF | （会話・演出だけ） |
| 西の管を調べる（壁の前に立って E） #2 | 調べる | Flag_TC_WestPipeOn | （会話・演出だけ） |
| 東の管を調べる（壁の前に立って E） | 調べる | Flag_TC_EastPipeOn が OFF | （会話・演出だけ） |
| 東の管を調べる（壁の前に立って E） #2 | 調べる | Flag_TC_EastPipeOn | （会話・演出だけ） |
| 南の出口（書斎へ戻る） | 入ると | なし | → Stage1_3 |
| 西の出口（西の部屋はまだない：受け口） | 入ると | Flag_TC_DoorEventDone | （会話・演出だけ） |
| 東の出口（東の部屋はまだない：受け口） | 入ると | Flag_TC_DoorEventDone | （会話・演出だけ） |
| 音符の光：E3 で触れたら表示したまま | 見た目 | Flag_TC_NoteGlowed | （表示が変わる） |
| 西のランプ：西の管に伝わったら点く | 見た目 | Flag_TC_WestPipeOn | （表示が変わる） |
| 東のランプ：東の管に伝わったら点く | 見た目 | Flag_TC_EastPipeOn | （表示が変わる） |
| 西の管の震え | 見た目 | Flag_TC_WestPipeOn | （表示が変わる） |
| 東の管の震え | 見た目 | Flag_TC_EastPipeOn | （表示が変わる） |
| シグル：E3 から南向き | 見た目 | Flag_TC_DoorEventDone | （表示が変わる） |
