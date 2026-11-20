# Stage1_3 の仕掛けの流れ（自動）

Tools > 手順書 > 仕掛けの流れ図を作り直す で作る。直接直さない。四角＝物（きっかけ）、丸＝フラグ、六角＝持ち物、平行四辺形＝仕掛けの状態。赤＝条件がそろわない。

## 注意
- なし

## 図
```mermaid
flowchart LR
  classDef ng fill:#fdd,stroke:#c00
  n25["着いたとき（シグルが扉を殴る → ロック）<br/>（着いたとき）"]
  f0(["Flag_Shosai_DoorsLocked"])
  n26["出口の先（この先の廊下はまだない：仮）<br/>（入ると）"]
  n27["出口とランプを調べる（開くまで）<br/>（調べる）"]
  f1(["Flag_Shosai_DoorOpen"])
  n28["T1（32インチ・低いド）<br/>（状態が変わったとき）"]
  n29["T1（32インチ・低いド）<br/>（スイッチ）"]
  i2{{"Item_Mallet"}}
  n30["T2_叩く（調律のあと）<br/>（状態が変わったとき）"]
  n31["T2_叩く（調律のあと）<br/>（スイッチ）"]
  n32["T2_濁った音（キーがない・調律の前）<br/>（ExamineSequence）"]
  n33["T2_調律（キーがある・調律の前。輪＋E を3回）<br/>（ペグ）"]
  f3(["Flag_Shosai_TuneExplained"])
  f4(["Flag_Shosai_T2Tuned"])
  m5[["Note_Shosai_Tuning"]]
  n34["T3（26インチ・ソ）<br/>（状態が変わったとき）"]
  n35["T3（26インチ・ソ）<br/>（スイッチ）"]
  n36["T4（23インチ・高いド）<br/>（状態が変わったとき）"]
  n37["T4（23インチ・高いド）<br/>（スイッチ）"]
  n38["机の楽譜（第1問の手がかり）<br/>（調べる）"]
  f6(["Flag_Shosai_ClueScore"])
  m7[["Note_Shosai_Score"]]
  n39["マレット置き場<br/>（調べる）"]
  m8[["Note_Shosai_TimpaniBasic"]]
  n40["東の本棚<br/>（調べる）"]
  f9(["Flag_Shosai_Q1Clear"])
  f10(["Flag_Shosai_GotKey"])
  i11{{"Item_TuningKey"}}
  n41["東の本棚 #2<br/>（調べる）"]
  n42["東の本棚 #3<br/>（調べる）"]
  n43["西の譜面箱<br/>（調べる）"]
  f12(["Flag_Shosai_ClueCipher"])
  m13[["Note_Shosai_Cipher"]]
  n44["西の譜面箱 #2<br/>（調べる）"]
  n45["西の譜面箱 #3<br/>（調べる）"]
  n46["シグル（NPC）<br/>（ExamineSequence）"]
  n47["本棚の側板の傷を調べる<br/>（調べる）"]
  f14(["Flag_Shosai_ClueWall"])
  m15[["Note_Shosai_Wall"]]
  n48["絨毯の叩き跡を調べる<br/>（調べる）"]
  f16(["Flag_Shosai_ClueFloor"])
  m17[["Note_Shosai_Floor"]]
  n49["扉A を調べる<br/>（調べる）"]
  n50["第1問（3音：T1→T3→T4）<br/>（答え合わせ）"]
  g18[/"第1問（3音：T1→T3→T4）"/]
  g19[/"第1問の失敗回数"/]
  n51["第2問（5音：T1→T3→T2→T3→T4）<br/>（答え合わせ）"]
  f20(["Flag_Sigle_NameKnown"])
  f21(["Flag_Shosai_SigleLeft"])
  g22[/"第2問（5音：T1→T3→T2→T3→T4）"/]
  g23[/"第2問の失敗回数"/]
  n52["T2：調律のあとは「叩く」<br/>（見た目）"]
  n53["T2：キーがあって調律の前は「調律」<br/>（見た目）"]
  n54["T2：キーがなく調律の前は「濁った音」<br/>（見た目）"]
  n55["ランプ1（第1問 1 音目）<br/>（見た目）"]
  g24[/"第1問の進み具合（ランプ1〜3）"/]
  n56["ランプ2（第1問 2 音目）<br/>（見た目）"]
  n57["ランプ3（第1問 3 音目）<br/>（見た目）"]
  n58["ランプ4（第2問 1 音目）<br/>（見た目）"]
  g25[/"第2問の進み具合（ランプ4〜8）"/]
  n59["ランプ5（第2問 2 音目）<br/>（見た目）"]
  n60["ランプ6（第2問 3 音目）<br/>（見た目）"]
  n61["ランプ7（第2問 4 音目）<br/>（見た目）"]
  n62["ランプ8（第2問 5 音目）<br/>（見た目）"]
  n63["扉A：ロックされたら閉める<br/>（見た目）"]
  n64["出口：第2問の正解で開く<br/>（見た目）"]
  n65["シグル：先へ進んだら消す<br/>（見た目）"]
  n66["マレット置き場：取ったら空にする<br/>（見た目）"]
  n67["本棚の格子：第1問で開く<br/>（見た目）"]
  n68["本棚の光とキー：第1問のあと取るまで<br/>（見た目）"]
  n69["譜面箱の鍵：第1問で開く<br/>（見た目）"]
  n70["譜面箱の光とメモ：第1問のあと取るまで<br/>（見た目）"]
  n71["ヒント：第1問で2回失敗<br/>（見た目）"]
  n72["ヒント：第2問で3回失敗<br/>（見た目）"]
  n73["ヒント：第2問で5回失敗<br/>（見た目）"]
  m26[["Note_Shosai_Q2Answer"]]
  f0 -.->|OFF のとき| n25
  n25 -->|ON| f0
  n26 --> mv26>"→ Stage1_4"]
  f1 -.->|OFF のとき| n27
  i2 -->|要る| n29
  i2 -->|要る| n31
  n33 -->|ON| f3
  n33 -->|ON| f4
  n33 -->|メモ| m5
  i2 -->|要る| n35
  i2 -->|要る| n37
  n38 -->|ON| f6
  n38 -->|メモ| m7
  i2 -.->|持っていないとき| n39
  n39 -->|手に入る| i2
  n39 -->|メモ| m8
  f9 --> n40
  f10 -.->|OFF のとき| n40
  n40 -->|ON| f10
  n40 -->|手に入る| i11
  f10 --> n41
  f9 --> n43
  f12 -.->|OFF のとき| n43
  n43 -->|ON| f12
  n43 -->|メモ| m13
  f12 --> n44
  n47 -->|ON| f14
  n47 -->|メモ| m15
  n48 -->|ON| f16
  n48 -->|メモ| m17
  f0 --> n49
  n50 -->|ON| f9
  n50 -->|変える| g18
  n50 -->|変える| g19
  f9 --> n51
  f4 --> n51
  n51 -->|ON| f1
  n51 -->|ON| f20
  n51 -->|ON| f21
  n51 -->|変える| g22
  n51 -->|変える| g23
  f4 --> n52
  f4 -.->|OFF のとき| n53
  i11 -->|要る| n53
  f4 -.->|OFF のとき| n54
  i11 -.->|持っていないとき| n54
  g24 --> n55
  g24 --> n56
  g24 --> n57
  g25 --> n58
  g25 --> n59
  g25 --> n60
  g25 --> n61
  g25 --> n62
  f0 --> n63
  f1 --> n64
  f21 --> n65
  i2 -->|要る| n66
  f9 --> n67
  f9 --> n68
  f10 -.->|OFF のとき| n68
  f9 --> n69
  f9 --> n70
  f12 -.->|OFF のとき| n70
  g19 --> n71
  g23 --> n72
  g23 --> n73
  n73 -->|メモ| m26
```

## 表
| 物 | きっかけ | 条件 | 結果 |
|---|---|---|---|
| 着いたとき（シグルが扉を殴る → ロック） | 着いたとき | Flag_Shosai_DoorsLocked が OFF | Flag_Shosai_DoorsLocked ON |
| 出口の先（この先の廊下はまだない：仮） | 入ると | なし | → Stage1_4 |
| 出口とランプを調べる（開くまで） | 調べる | Flag_Shosai_DoorOpen が OFF | （会話・演出だけ） |
| T1（32インチ・低いド） | 状態が変わったとき | なし | （会話・演出だけ） |
| T1（32インチ・低いド） | スイッチ | 持ち物 Item_Mallet | （会話・演出だけ） |
| T2_叩く（調律のあと） | 状態が変わったとき | なし | （会話・演出だけ） |
| T2_叩く（調律のあと） | スイッチ | 持ち物 Item_Mallet | （会話・演出だけ） |
| T2_濁った音（キーがない・調律の前） | ExamineSequence | なし | （会話・演出だけ） |
| T2_調律（キーがある・調律の前。輪＋E を3回） | ペグ | なし | Flag_Shosai_TuneExplained ON、Flag_Shosai_T2Tuned ON、メモ Note_Shosai_Tuning |
| T3（26インチ・ソ） | 状態が変わったとき | なし | （会話・演出だけ） |
| T3（26インチ・ソ） | スイッチ | 持ち物 Item_Mallet | （会話・演出だけ） |
| T4（23インチ・高いド） | 状態が変わったとき | なし | （会話・演出だけ） |
| T4（23インチ・高いド） | スイッチ | 持ち物 Item_Mallet | （会話・演出だけ） |
| 机の楽譜（第1問の手がかり） | 調べる | なし | Flag_Shosai_ClueScore ON、メモ Note_Shosai_Score |
| マレット置き場 | 調べる | Item_Mallet を持っていない | 手に入る Item_Mallet、メモ Note_Shosai_TimpaniBasic |
| 東の本棚 | 調べる | Flag_Shosai_Q1Clear、Flag_Shosai_GotKey が OFF | Flag_Shosai_GotKey ON、手に入る Item_TuningKey |
| 東の本棚 #2 | 調べる | Flag_Shosai_GotKey | （会話・演出だけ） |
| 東の本棚 #3 | 調べる | なし | （会話・演出だけ） |
| 西の譜面箱 | 調べる | Flag_Shosai_Q1Clear、Flag_Shosai_ClueCipher が OFF | Flag_Shosai_ClueCipher ON、メモ Note_Shosai_Cipher |
| 西の譜面箱 #2 | 調べる | Flag_Shosai_ClueCipher | （会話・演出だけ） |
| 西の譜面箱 #3 | 調べる | なし | （会話・演出だけ） |
| シグル（NPC） | ExamineSequence | なし | （会話・演出だけ） |
| 本棚の側板の傷を調べる | 調べる | なし | Flag_Shosai_ClueWall ON、メモ Note_Shosai_Wall |
| 絨毯の叩き跡を調べる | 調べる | なし | Flag_Shosai_ClueFloor ON、メモ Note_Shosai_Floor |
| 扉A を調べる | 調べる | Flag_Shosai_DoorsLocked | （会話・演出だけ） |
| 第1問（3音：T1→T3→T4） | 答え合わせ | なし | Flag_Shosai_Q1Clear ON、変える 第1問（3音：T1→T3→T4）、変える 第1問の失敗回数 |
| 第2問（5音：T1→T3→T2→T3→T4） | 答え合わせ | Flag_Shosai_Q1Clear、Flag_Shosai_T2Tuned | Flag_Shosai_DoorOpen ON、Flag_Sigle_NameKnown ON、Flag_Shosai_SigleLeft ON、変える 第2問（5音：T1→T3→T2→T3→T4）、変える 第2問の失敗回数 |
| T2：調律のあとは「叩く」 | 見た目 | Flag_Shosai_T2Tuned | （表示が変わる） |
| T2：キーがあって調律の前は「調律」 | 見た目 | Flag_Shosai_T2Tuned が OFF、持ち物 Item_TuningKey | （表示が変わる） |
| T2：キーがなく調律の前は「濁った音」 | 見た目 | Flag_Shosai_T2Tuned が OFF、Item_TuningKey を持っていない | （表示が変わる） |
| ランプ1（第1問 1 音目） | 見た目 | 仕掛け 第1問の進み具合（ランプ1〜3） | （表示が変わる） |
| ランプ2（第1問 2 音目） | 見た目 | 仕掛け 第1問の進み具合（ランプ1〜3） | （表示が変わる） |
| ランプ3（第1問 3 音目） | 見た目 | 仕掛け 第1問の進み具合（ランプ1〜3） | （表示が変わる） |
| ランプ4（第2問 1 音目） | 見た目 | 仕掛け 第2問の進み具合（ランプ4〜8） | （表示が変わる） |
| ランプ5（第2問 2 音目） | 見た目 | 仕掛け 第2問の進み具合（ランプ4〜8） | （表示が変わる） |
| ランプ6（第2問 3 音目） | 見た目 | 仕掛け 第2問の進み具合（ランプ4〜8） | （表示が変わる） |
| ランプ7（第2問 4 音目） | 見た目 | 仕掛け 第2問の進み具合（ランプ4〜8） | （表示が変わる） |
| ランプ8（第2問 5 音目） | 見た目 | 仕掛け 第2問の進み具合（ランプ4〜8） | （表示が変わる） |
| 扉A：ロックされたら閉める | 見た目 | Flag_Shosai_DoorsLocked | （会話・演出だけ） |
| 出口：第2問の正解で開く | 見た目 | Flag_Shosai_DoorOpen | （表示が変わる） |
| シグル：先へ進んだら消す | 見た目 | Flag_Shosai_SigleLeft | （表示が変わる） |
| マレット置き場：取ったら空にする | 見た目 | 持ち物 Item_Mallet | （表示が変わる） |
| 本棚の格子：第1問で開く | 見た目 | Flag_Shosai_Q1Clear | （表示が変わる） |
| 本棚の光とキー：第1問のあと取るまで | 見た目 | Flag_Shosai_Q1Clear、Flag_Shosai_GotKey が OFF | （表示が変わる） |
| 譜面箱の鍵：第1問で開く | 見た目 | Flag_Shosai_Q1Clear | （表示が変わる） |
| 譜面箱の光とメモ：第1問のあと取るまで | 見た目 | Flag_Shosai_Q1Clear、Flag_Shosai_ClueCipher が OFF | （表示が変わる） |
| ヒント：第1問で2回失敗 | 見た目 | 仕掛け 第1問の失敗回数 | （会話・演出だけ） |
| ヒント：第2問で3回失敗 | 見た目 | 仕掛け 第2問の失敗回数 | （会話・演出だけ） |
| ヒント：第2問で5回失敗 | 見た目 | 仕掛け 第2問の失敗回数 | メモ Note_Shosai_Q2Answer |
