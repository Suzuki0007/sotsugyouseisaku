# 見た目 A：光漏れ・レンズフレア・画面の縁・字幕・塔の外・床の色（プラン。2026-11-27）

`Docs/打ち合わせ_2026-11-27_実装方針.md` の「3-A 見た目」を作るための調べ物と計画です。

---

## 1. 調べてわかったこと

### 授業で見た 5 枚から読み取れること
| 絵 | 見た目の決め手 | ゲームでの再現 |
|---|---|---|
| ダリ（白い平原と塔） | 地平線が低く、空が画面の半分以上。空は上が青緑で、地平線に向かって淡い黄色。地面は白に近い黄土色で何もない。影は長く、くっきりしている | 塔の外を「空のグラデーション」にする。地面は明るく、コントラストは弱めにする |
| ダリ（オレンジの砂地と黒い糸杉） | 全体をオレンジと青緑の2色で塗り分けている。上が暗く、地平線の近くが明るい | 「上下のグラデーション」（上を落とす）と、暗い所＝青緑・明るい所＝橙の色分け（今の色調の「色分け」でできる） |
| 加工前・加工後（海の女性） | 青緑に寄せ、黒を少し浮かせ、四隅と上を暗くしている | 今のビネットに「上を暗くする」を足せばできる |
| 光漏れ（海と枝） | 右に橙と赤、左上に青緑の帯、左下の角が白く焼けている、縦の筋。全体が少し白っぽい | 「光漏れ」：横の帯・角の焼け・縦の筋の3種類を重ねる |
| レンズフレア（松と太陽） | 太陽のまわりが白く飛ぶ。太陽から画面の中心を通る線の上に、六角形・小さな玉・虹の輪が並ぶ。全体に橙のかぶりがあり、黒が浮く | 「太陽」：画面の中心を通る線の上にゴーストを並べ、太陽が画面に近いほど全体を白っぽくする |

### 評価の高いゲームからわかったこと
- **ICO・ワンダと巨像（上田文人）**：ブルーム（光のにじみ）と色の薄さで、景色を「白っぽく洗い流した」見た目にしている。上田さんは「全体に効かない物は削る（引き算のデザイン）」と言っている。[Aesthetic Explorations](https://aesdes.org/2017/01/25/aesthetic-explorations-fumito-uedas-video-games)・[GameRant](https://gamerant.com/ico-20-year-anniversary-impact-story-gameplay-design/) → **白ベースと儚さに一番近い手本**。光の効果も「足しすぎない」。
- **GRIS**：色のない世界から始まり、進むと色が戻る。白い余白を多く取り、色は少しだけ置く。[Stash](https://www.stashmedia.tv/gris-beautiful-game-nomada-conrad-roset/)・[Power Up Gaming](https://powerup-gaming.com/2019/02/15/interpreting-gris-nomada-studio-interview) → 白ベース＋テーマカラーを少しだけ、という打ち合わせの案と同じ考え方。今ある「色の戻り具合」がそのまま使える。
- **Sable**：平らな色で塗ると奥行きがわからなくなるので、光・霧・線を重ねて奥行きを出している。夢のような、かすんだ砂漠。[Game Developer（GDC 2022）](https://www.gamedeveloper.com/marketing/how-shedworks-refined-the-art-of-sable-in-pursuit-of-readability)・[PC Gamer](https://www.pcgamer.com/sable-is-a-gorgeous-contemplative-exploration-game-made-by-two-friends-who-work-in-a-shed) → 白い床で平らに見える問題は、霧・空・影を重ねて解く（今日作った霧が使える）。
- **Season: A Letter to the Future**：かすんだ、色の薄いパステル調。光の当て方を場所ごとに変えて気分を作っている。[Game Developer](https://gamedeveloper.com/art/love-like-a-sunset-the-inspiration-and-ideology-behind-season-s-hazy-visuals)・[Unreal Engine](https://www.unrealengine.com/tech-blog/creating-the-illustrative-look-of-season-using-unreal-engine)
- **Lost Records: Bloom & Rage**（ライフイズストレンジの後継）：思い出の場面だけ、少しぼけた古いレンズのような見た目にしている。[Yardbarker](https://www.yardbarker.com/video_games/articles/lost_records_bloom_t_nods_nostalgic_narrative_journey_will_make_you_cry/s1_17144_40951079) → **光漏れ・フレアは「回想・思い出」の場面に強く**、ふだんは弱く、という使い分けの手本。
- **Orbitals**：80〜90年代のアニメに見せるため、粒子・ハレーション（光のにじみ）・色ずれ・ぼかしを重ねて「フィルムで撮った」感じにしている。[Unreal Engine](https://www.unrealengine.com/tech-blog/stepping-inside-a-retro-anime-inspired-game-a-look-into-the-rendering-of-orbitals)
- **オクトパストラベラー（HD-2D）**：ぼかし・ブルーム・ビネットを常にかけている。ビネットは本物の加工ではなく、場面によって四隅に影を置く工夫もある。一方で「効果が強すぎる」として切る人もいる。[Wikipedia](https://en.wikipedia.org/wiki/HD-2D)・[Twinfinite](https://twinfinite.net/guides/octopath-traveler-disable-corner-shadows-how/) → **強さは場面ごとに変えられるようにし、常に最大にはしない**。
- **レンズフレアの歴史**：「ゲームで一番使われすぎた効果」と言われる。[GamesRadar](https://gamesradar.com/brief-history-most-over-used-special-effect-videogames-lens-flare/2) → 太陽が見える場面だけに使う。
- **Kentucky Route Zero**：字の選び方が遊ぶ人の受け取り方を大きく変える、と作者が言っている。[PC Gamer](https://pcgamer.com/welcome-to-the-zero-the-making-of-kentucky-route-zeros-open-road)
- **字幕の読みやすさ（Game Accessibility Guidelines など）**：1080p で 46px より小さくしない、縁取りか影をつける、1行は長くしすぎない、2行まで。[Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/if-any-subtitles-captions-are-used-present-them-in-a-clear-easy-to-read-way/)・[80.lv](https://80.lv/articles/10-golden-rules-on-subtitles-for-games)

### Unity で調べたこと
- URP の「Lens Flare (SRP)」は、2D の URP に置くとエラーが出続ける報告がある。[Unity の不具合の一覧](https://issuetracker.unity.com/issues/21809/render-graph-execution-and-argumentexception-errors-are-spammed-when-adding-lens-flare-srp-component-in-a-2d-urp-project) → **Unity のフレアは使わず、絵を重ねる形で自分で作る**（2D で確実に動き、見た目も調整しやすい）。

### このプロジェクトで気をつけること
- **白い画面に白い字幕は読めない。**白ベースにするなら、字幕は「濃い影＋薄い縁取り」が必須。
- 画面は中央の 4:3 に写し、左右は黒い帯（`MapCameraAspect`）。打ち合わせで出た「画面が真四角に見える」は、この 4:3 の端がくっきり切れていることが原因。
- ビネット・粒子・ブルームは、すでに色調のプリセット（`MoodPreset`）にある。新しい効果も**色調のプリセットに足す**と、場面ごとの切り替え・ゆっくり変える・回想・セーブが今の仕組みのまま使える。

---

## 2. 作る物

| # | 作る物 | 中身 | プランナーの使い方 |
|---|---|---|---|
| 1 | **画面の縁** | 4:3 の端を帯の色へぼかして消す（幅・角の丸み）。上下のグラデーション（上を暗く・下を明るく、色も選べる）＝空を落とす | 色調のプリセットの新しい項目「画面の縁」 |
| 2 | **光漏れ** | 横の帯（左右。色・幅・強さ）・角の焼け（白〜橙）・縦の筋。ゆっくり揺れて、ときどき一瞬強く光る（「ピッ」と） | 色調のプリセットの新しい項目「光漏れ」。回想のプリセットは強めにする |
| 3 | **太陽（レンズフレア）** | 太陽の位置を置くと、まわりの白い光・画面の中心を通る線上の六角形と玉・虹の輪・全体のかぶりが出る。太陽が画面の外でも、近ければ少し出る | Hierarchy 右クリック > 演出 > 太陽（レンズフレア）。強さは色調のプリセットで場面ごとに |
| 4 | **映画の字幕** | 中央より少し下に大きな太字（NotoSansJP Bold）。名前なし・背景の箱なし・濃い影と縁取り。1行 16 字くらい・2行まで | 部品「字幕を出す」に「字幕の形：映画／会話」を足す（既定は映画） |
| 5 | **塔の外（空のプリセット）** | 歩ける範囲の外を「空」で埋める。空は**プリセット**（上の色・地平線の色・雲の量と色と速さ・星）で、最初に **黒・青空・暗い空（重い雲）・夜明け・夕焼け・夜（星）** の 6 つを用意する（昔の RPG ツクールの空のような、グラデーションとゆっくり流れる雲）。プリセットは増やせる。イベントで別の空へ**ゆっくり切り替え**られる（暗い空 → 夜明け など） | シーンの演出の設定（`ScenePresentation`）に「外側の空」を足す。部品「空を変える」（秒数つき）。色調・光と同じ層とセーブに乗せる |
| 6 | **白ベースの色調と、床の寒色の比較** | ダリの絵を手本にしたプリセット 2 つ（白い廃墟・橙と青緑）。試し配置マップで床の色を 3 案（今・少し寒色・寒色）撮って比べる | プリセットを選ぶだけ。比べた画像は北川さんに渡す |

**作らない物**：色ずれ（色収差）・画面全体のぼかし。どちらも字幕と小さいドット絵が読みにくくなるため。必要になったら足します。

---

## 3. 段階

前回と同じく、**段階ごとにコミットしてプッシュ**し、終わったら次へ進みます。

| 段階 | やること | 確かめ方 |
|---|---|---|
| 1 ✅ | **画面の縁**・上下のグラデーション（色調のプリセットに項目を足す・窓口で表示する）。11-27 完了：`ScreenLensLayer` | 試し用マップ（`Test_Presentation`）で撮影。会話・1枚絵・シネマの帯と重ならないか |
| 2 ✅ | **光漏れ**（3種類・揺れ・一瞬光る）。11-27 完了：絵は実行中にコードで作る（差し替えの素材が届いたら絵を読む形にする） | 撮影を続けて動きを見る。回想に入る・戻るでゆっくり変わるか |
| 3 ✅ | **映画の字幕**（太字のフォントを作る・字幕の形を足す）。11-27 完了：前からある字幕は帯のまま（新しく足すと映画） | 白い画面・暗い画面の両方で読めるか撮影。今の字幕を使っている場面（OP など）のシナリオ |
| 4 | **太陽（レンズフレア）** | 太陽が画面の中・端・外のときを撮影。重さを測る |
| 5 | **塔の外（空のプリセット 6 つ・部品「空を変える」）**・**白ベースの色調**・**床の寒色の比較** | 試し配置マップで空 6 つを撮影。暗い空 → 夜明けの切り替え・ロードで空が戻るか |
| 6 | まとめ：試し用マップに台座を足す・シナリオ・手順書（`Docs/演出の作り方.md`）・早見表・性能の点検 | シナリオ `presentation_test` に足して合格・重さを前と比べる |

次回の発表に一番効くのは **段階 1〜3**（縁・光漏れ・字幕）。ここまでで、打ち合わせで出た「ビネット・光漏れ・太字の字幕」がそろいます。

### 決めたこと（実装の約束）
- 新しい効果は **画面に重ねる UI の絵**で作る（ポストプロセスのシェーダーを書かない）。4:3 の範囲の中だけに出す。並び順は「1枚絵の上・シネマの帯と字幕の下」（1枚絵＝写真にも光漏れがかかる）。
- 値は色調のプリセットの中に入れ、今の層（既定・範囲・イベント・回想）・ゆっくり変える・セーブの仕組みに乗せる。
- 絵（帯・六角形・虹の輪・雲）はコードで作り、`Assets/Images/` に保存する（素材が届いたら差し替えられる）。
- 毎フレームの処理で確保しない。画質「低」では光漏れの筋とフレアのゴーストを減らす。
