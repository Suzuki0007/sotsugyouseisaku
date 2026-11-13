# 環境プロファイル（CT）

> 上流の detect-env.py（Python が要る）の代わりに手で書いた。scan.ps1 が読み、適用条件タグが**明確に合わない**ルールだけを外す。
> 値が変わったら（Unity の版・ビルドの方式・出す機種）ここを直す。確かめた日：2026-11-13。

| キー | 値 | 根拠 |
|---|---|---|
| unity_version | 6000.5.4f1 | ProjectSettings/ProjectVersion.txt |
| render_pipeline | urp | Packages/manifest.json（com.unity.render-pipelines.universal 17.6.0、2D Renderer） |
| scripting_backend | mono | ProjectSettings.asset の scriptingBackend が空＝Standalone の既定（Mono） |
| platforms | windows | PC（Standalone）向けの卒業制作。iOS・Android には出さない |
| addressables | false | manifest に com.unity.addressables がない |
| vat | false | 2D。VAT は使わない |
