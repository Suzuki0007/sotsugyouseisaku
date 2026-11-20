# Stage1_0 の仕掛けの流れ（自動）

Tools > 手順書 > 仕掛けの流れ図を作り直す で作る。直接直さない。四角＝物（きっかけ）、丸＝フラグ、六角＝持ち物、平行四辺形＝仕掛けの状態。赤＝条件がそろわない。

## 注意
- なし

## 図
```mermaid
flowchart LR
  classDef ng fill:#fdd,stroke:#c00
  n103["DialogueEventZone<br/>（入ると）"]
  n104["OP_はじめ（暗転 → 小舟の上で手紙 → 上陸）<br/>（着いたとき）"]
  f0(["Flag_OP_Done"])
  f1(["Flag_Land_LetterRead"])
  f2(["Flag_Land_VibeS"])
  m3[["Note_KairosLetter"]]
  n105["OP_途中（鐘の言い伝え）<br/>（入ると）"]
  n106["OP_塔の入口（F1_01）<br/>（入ると）"]
  n107["歩ける範囲（砂浜だけ）<br/>（FlagSwitch）"]
  n108["残骸1（西。調べる）<br/>（調べる）"]
  f4(["Flag_Land_Wreck1"])
  f5(["Flag_Land_WreckAny"])
  n109["残骸2（東。調べる）<br/>（調べる）"]
  f6(["Flag_Land_Wreck2"])
  n110["残骸3（奥。調べる）<br/>（調べる）"]
  f7(["Flag_Land_Wreck3"])
  n111["足跡（範囲に入ると気づく）<br/>（入ると）"]
  f8(["Flag_Land_Footprints"])
  n112["振動（中）→ 砂漠へ（奥）<br/>（入ると）"]
  f9(["Flag_Land_VibeM"])
  f0 -.->|OFF のとき| n104
  n104 -->|ON| f1
  n104 -->|ON| f2
  n104 -->|ON| f0
  n104 -->|メモ| m3
  f1 --> n107
  n108 -->|ON| f4
  n108 -->|ON| f5
  n109 -->|ON| f6
  n109 -->|ON| f5
  n110 -->|ON| f7
  n110 -->|ON| f5
  n111 -->|ON| f8
  n112 -->|ON| f9
  n112 --> mv112>"→ Stage1_1"]
```

## 表
| 物 | きっかけ | 条件 | 結果 |
|---|---|---|---|
| DialogueEventZone | 入ると | なし | （会話・演出だけ） |
| OP_はじめ（暗転 → 小舟の上で手紙 → 上陸） | 着いたとき | Flag_OP_Done が OFF | Flag_Land_LetterRead ON、Flag_Land_VibeS ON、Flag_OP_Done ON、メモ Note_KairosLetter |
| OP_途中（鐘の言い伝え） | 入ると | なし | （会話・演出だけ） |
| OP_塔の入口（F1_01） | 入ると | なし | （会話・演出だけ） |
| 歩ける範囲（砂浜だけ） | FlagSwitch | Flag_Land_LetterRead | （表示が変わる） |
| 残骸1（西。調べる） | 調べる | なし | Flag_Land_Wreck1 ON、Flag_Land_WreckAny ON |
| 残骸2（東。調べる） | 調べる | なし | Flag_Land_Wreck2 ON、Flag_Land_WreckAny ON |
| 残骸3（奥。調べる） | 調べる | なし | Flag_Land_Wreck3 ON、Flag_Land_WreckAny ON |
| 足跡（範囲に入ると気づく） | 入ると | なし | Flag_Land_Footprints ON |
| 振動（中）→ 砂漠へ（奥） | 入ると | なし | Flag_Land_VibeM ON、→ Stage1_1 |
