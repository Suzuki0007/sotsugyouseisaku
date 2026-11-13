# Project Auditor の指摘で、この作品では外すもの

> `auditor.ps1` がこの表の ID の指摘を「ignored」として数えるだけにする（一覧に出さない・差分に入れない）。
> 書籍の注意どおり、Project Auditor の Ignore はレポートに残るだけで設定ファイルに出ないので、ここで管理する。
> 外す理由を必ず書く。理由がなくなったら行を消す。

| ID | 何の指摘か | 外す理由 |
|---|---|---|
| PAA3000 | Resources フォルダにあるアセット | 一言・アイテム・メモ・UI の設定は「Resources に置くだけで使える」設計（プランナー向け。rules/gimmick-party.md など）。量も小さい |
| PAA3001 | Resources フォルダのアセットが参照しているアセット | 同上（フォント・TMP のシェーダーなど） |
| PAA4011 | 音の元ファイルが MP3 など非可逆 | 音質の話で性能ではない。素材の形式はサウンド担当が決める |
| PAS0016 | Fixed Timestep が既定値（0.02） | 推奨は 30fps の場合の話。この作品は 60fps |
| PAS0023 | Rendering Path が Forward | 2D の作品には Forward が合う（指摘の文にもそう書いてある） |
| PAS0007 | Prebake Collision Meshes が無効 | 3D のメッシュのコライダーの話。2D では使わない |
| PAS0008 | Optimize Mesh Data が無効 | 3D のメッシュの話。2D のスプライトにはほぼ効かない |
| PAS1007 | Quality の Mipmap Streaming が無効 | 高解像度のミップマップ付きテクスチャがない（2D・スプライトはミップマップなし） |
| PAS0019 | Texture Quality が Full Res でない | 当たっているのは品質レベル「Very Low」（テクスチャ半分）。PC のビルドの既定は「Ultra」（Full Res）なので完成品には効かない。ただし**エディタの今の品質レベルが Very Low**（code-map の問題点）。品質レベルの整理は PAS0018 で見る |
| PAS0020 | Async Upload Time Slice が既定値 | ロードが長いときに試す設定。今は問題になっていない |
| PAS0021 | Async Upload Buffer Size が既定値 | 同上 |
| UDR0005 | RuntimeInitializeOnLoadMethod が 2 つ以上ある | このプロジェクトの決まった書き方（static のリセット＝SubsystemRegistration と、自動で作る＝AfterSceneLoad を分ける。rules/coding-style.md）。害はない |
