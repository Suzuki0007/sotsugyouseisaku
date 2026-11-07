# アニメーションの使い方（プランナー向け）

コードを書かずに、**画像を入れる → SpriteAnimData を作る → SpriteAnimator を付ける** の3ステップで、キャラクターやオブジェクトを動かせます。
（エフェクトと同じ操作感です。エフェクトは `Assets/Script/Effect/README_エフェクトの使い方.md` を見てください）

---

## 1. 画像を入れる

- キャラクターは `Assets/Images/Player/`・`Assets/Images/Enemy/` など、オブジェクトは用途ごとのフォルダに入れてください。
- 入れた画像を選び、Inspector の **Texture Type** を **Sprite (2D and UI)** にします。
- 次のどちらの形でもOKです。
  - **スプライトシート**：1枚にコマが並んだ画像。**Sprite Mode: Multiple** にして **Sprite Editor** で切り分けます。**1枚 = 1つの向き** として扱います。
  - **連番ファイル**：`Walk_Down_00.png, Walk_Down_01.png …` のように1コマずつ分かれた画像。

### 名前の付け方（ここが大事）

画像の名前から **動き** と **向き** を自動で判断します。おすすめは **`キャラ名_動き_向き`** です。

```
Noah_Walk_Down.png      (スプライトシート)
Noah_Walk_Up.png
Noah_Walk_Left.png       ← 右だけでもOK（左は右を反転して使う）
Noah_Idle_Down.png
Slime_Dead_00.png, Slime_Dead_01.png …  (連番ファイル・向きなし)
Chest_Open_00.png …
```

| 種類 | 使える単語 |
|---|---|
| 向き：下 | `front` `down` `正面` `前` `下` |
| 向き：上 | `back` `behind` `up` `背面` `後ろ` `上` |
| 向き：左 | `left` `左` |
| 向き：右 | `right` `side` `右` `横` |
| 動き | `Idle`(idle/wait/stand/待機)　`Walk`(walk/move/歩き)　`Run`(run/dash)　`Damage`(damage/hurt)　`Dead`(dead/death/die/死亡)　`Attack`(attack/shot/攻撃)　`Open`(open/開く)　`Close`(close/閉じる) |

- 区切りは `_` `-` スペースのどれでもOK。大文字・小文字も問いません。
- 単語の後ろの数字は無視します（`front2` → `front`）。
- **名前に動きが無いとき**：向きがあって2コマ以上 → `Walk`、それ以外 → `Idle` とみなします。
  - 例：今ある `animation_noah_front2.png`（4コマ）→ Walk の下向き、`Noah-front.png`（1コマ）→ Idle の下向き

## 2. SpriteAnimData を作る

1キャラクター（1オブジェクト）分の画像を **まとめて選んで**、**右クリック → Create → Animation → SpriteAnimData (選択した画像から作成)**。

`Anim_Noah` のような名前で、画像と同じフォルダにできます。Console に「どの画像をどの動き・向きにしたか」が出るので確認してください。

**あとから動きを足すとき**は、作った SpriteAnimData の Inspector 上の枠に画像を **ドラッグ&ドロップ** します。同じ名前の動きがあれば画像だけ入れ替わります（速さやイベントは残ります）。

### Inspector で調整する

```
┌ [ここに画像をドラッグ&ドロップ] ─────────┐
├ 動き ──────────────────────────┤
│ [Idle] [Walk] [Dead]   ← 押して切り替え    │
│ ＋追加 複製 削除 ▲ ▼                      │
│ 最初に流す動き  Idle                        │
├ 設定 ──────────────────────────┤
│ 名前        Walk                            │
│ 向き        上下左右                        │
│ 下向き / 上向き / 左向き / 右向き の画像     │
│ 速さ        8   ← 1秒に何コマ               │
│ 再生のしかた ループ / 1回 / 1回→次の動きへ  │
│ ▶ コマごとの長さ                            │
├ コマごとのイベント (音・エフェクト) ─────┤
├ プレビュー ───────────────────────┤
│ [↓下][↑上][←左][→右]                      │
│ ┌──────────────┐               │
│ │   (プレビュー)  │ ← ここで動く      │
│ └──────────────┘               │
└────────────────────────────┘
```

| 項目 | 説明 |
|---|---|
| 向き | **向きなし**（置物・エフェクト風）／**上下左右**／**左右だけ**。左右は片方だけ入れれば、もう片方は反転して使います |
| 速さ | 1秒に何コマ進めるか。大きいほど速い |
| 再生のしかた | **ループ**：繰り返す（Idle・Walk・燃える松明）／**1回**：最後のコマで止まる（Dead・Open・Damage）／**1回→次の動きへ**：終わったら別の動きへ（Open → Opened など） |
| コマごとの長さ | `2` にするとそのコマだけ2倍長く表示。溜めや決めポーズに |
| コマごとのイベント | 「◯コマ目で SE を鳴らす」「◯コマ目でエフェクトを出す」。足音・攻撃の発射・着地の土煙など |

設定が足りないと黄色の警告が出ます。画像ごとに **Pixels Per Unit / Filter Mode** がバラバラだと警告が出るので、ボタンでそろえてください。

## 3. SpriteAnimator を付ける

動かしたいオブジェクト（SpriteRenderer が付いているもの）を選び、**Add Component → Animation → Sprite Animator** を追加して、**アニメーションデータ** に作った SpriteAnimData を入れます。

- これだけで「最初に流す動き」がループ再生されます（松明・NPC・背景の置物など）。
- データを入れるとシーン上の見た目が1コマ目になります。
- **Playモード中**は Inspector の「▶ 動きの名前」ボタンで試し再生、「下向き」などのボタンで向きを確認できます。

> ⚠ **Animator コンポーネントと一緒に付けないでください。** Animator が画像を上書きします（警告と「Animator を外す」ボタンが出ます）。

### キャラクターの場合：CharacterAnimLink も付ける

**Add Component → Animation → Character Anim Link** を追加すると、コードを書かずに次のように切り替わります。

| 状況 | 流れる動き |
|---|---|
| 止まっている | `Idle`（向きは最後に動いた方向） |
| 動いている | `Walk`（無ければ Idle のまま） |
| ダメージを受けた（Health） | `Damage` を1回流して元に戻る（無ければ何もしない） |
| 死亡した（Health） | 下の「死亡アニメ」を参照 |

- 操作キャラ・追従している仲間・敵のどれでも使えます（実際に動いたかどうかで判断します）。
- 動きの名前は Inspector で変えられます（例：歩きを `Run` にしたい場合）。

### 死亡アニメ

敵（`EnemyDead`）とノア（`NoahDead`）には **「死亡アニメの名前」**（初期値 `Dead`）欄があります。

- SpriteAnimData に `Dead` の動きがあれば、**流し終わってから** 敵が消える／ゲームオーバー画面へ移ります。
  - アニメ中は移動・攻撃・当たり判定が止まります。
  - 敵の「死亡エフェクト」は、アニメが終わって消える瞬間に出ます。
- `Dead` が無ければ、今まで通りすぐに消える／移ります。
- `Dead` は「1回」にしてください。

### 宝箱

`Treasure` の **「Open Animator」** に宝箱の SpriteAnimator を入れると、開けたときに `Open` の動きを流します（空なら今まで通り画像の差し替え）。セーブから戻ったときは、開き終わった見た目で表示されます。

### イベントで流す

EventZone のイベントで流したいときは、**AnimEventAction**（Add Component → Animation → Anim Event Action）を付けて EventSequence の actions に登録します。セーブから戻ったときは、流し終わった見た目で表示されます。

---

## 4. ノア・シグルを新しい仕組みに置き換える

**Tools → Animation → サンプルを作成して割り当てる (ノア・シグル)** を実行すると、次のようになります。

- `Assets/Data/Animation/Sample/` に `Anim_Sample_Noah`（Idle + Walk 4方向）と `Anim_Sample_Sigle`（Idle 4方向）ができます。
- `Player_Noah` / `Player_Sigle` の Prefab に SpriteAnimator・CharacterAnimLink が付き、古い `NoahAnimation` / `SigleAnimation` と Animator が外れます。

本番の絵ができたら、その SpriteAnimData に画像をドラッグするだけで差し替わります。

---

## よくある質問

**Q. 動かない／画像が変わらない**
- Animator コンポーネントが付いていませんか？（SpriteAnimator の Inspector に警告が出ます）
- SpriteAnimData の「最初に流す動き」に画像が入っているか確認してください。

**Q. 向きが変わらない**
- その動きの「向き」が「向きなし」になっていませんか？

**Q. 歩きと待機がチラチラ切り替わる**
- CharacterAnimLink の **Stop Delay** を少し大きく（0.15〜0.2）してください。

**Q. 名前から正しく振り分けられなかった**
- Console のログで振り分け結果を確認し、Inspector で画像を正しい欄に入れ直してください。次からは「名前の付け方」の単語を使ってください。

---

## プログラマー向け

```csharp
SpriteAnimator anim = GetComponent<SpriteAnimator>();

anim.Play("Attack");             // 動きを流す (同じ動きを流し中なら何もしない)
anim.Play("Attack", true);       // 最初から流し直す
anim.SetDirection(moveDir);      // 向き (Vector2。斜めは左右優先)
anim.SetFacing(SpriteAnimFacing.Left);
anim.JumpToEnd("Open");          // 流し終わった見た目にする (セーブ復元用。音・エフェクトは出ない)
yield return anim.WaitForCycle();// 1周流し終わるまで待つ

anim.OnClipFinished += (a, clip) => { ... };   // 「1回」の動きが終わった
anim.OnMessage      += (a, msg)  => { ... };   // コマごとのイベントの「合図」

GetComponent<CharacterAnimLink>().PlayAction("Attack"); // 1回流して Idle/Walk に戻る
```

- 連番のコマ計算は `SpriteFrameTiming` に共通化してあり、エフェクト（`EffectInstance`）も同じ処理を使っています。
- 仮置きの Animator 方式（`SpriteAnimation` / `kari_player_test` / `Player.controller`）はそのまま残してあります。
