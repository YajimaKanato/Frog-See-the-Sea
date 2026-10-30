# Frog See the Sea / カエル、海を知る

Unity 6000.6.3f1。井戸のカエルが足場を登り、外で海を見る2Dジャンプゲームです。

## 起動とシーン

**Assets/Scenes/Title.unity** を開いてPlayします。

- **Title**：タイトル画面。「Start」で同じシーン内のルール選択UIへ切り替えます。Stages / Endlessとステージ番号を選び、「Climb」でInGameシーンへ移ります。
- **InGame**：ゲーム進行・2D表示・ゲーム中のHUD。一時停止、再挑戦、クリア画面を担当します。「Title」でタイトルシーンへ戻ります。
- Build SettingsはTitleを0番、InGameを1番として登録しています。InGameを直接Playした場合はステージ1から開始します。
- 旧Wellシーン、3Dの描画設定・仮素材・軌道ガイドは削除しました。

シーン生成メニュー：**Frog See the Sea → Create or open 2D scenes**。既存のTitle / InGameシーンは上書きしません。

## 操作

HUD以外でマウス左ボタン／タッチを押し、跳びたい方向の反対へドラッグして離します。距離で威力、角度で方向が変わります。

- 真下へ引くと上、右下へ引くと左上へジャンプ。
- 空中で再ジャンプや方向転換はできません。
- 苔の柱へ横から触れると張り付きます。2.2秒以内にドラッグを離して、壁の反対側へジャンプします。
- 左右は連続したループです。カエルの水平表示位置は固定し、足場と背景が横スクロールします。カメラは高さを追従します。
- 軌道線、方向矢印、ジャンプ力ゲージは表示しません。
- R：再挑戦。Esc：一時停止／ドラッグ中はキャンセル。

3ステージ（42 / 54 / 66m）とエンドレスを収録しています。最高高度をローカル保存します。対戦は未実装です。UIは仮の英語表記です。

## 構成

| 層 | クラス | 責務 |
| --- | --- | --- |
| シーン間設定 | GameSession | 選択したルールとステージ番号だけを保持し、シーン遷移 |
| タイトル | TitleController | タイトル内のホーム／ルール選択UI切り替え |
| インゲーム | FrogSeaGame | Model・Input・Viewの接続、実行・一時停止・再挑戦 |
| Model | FrogModel | 数値計算による2D移動、着地、壁張り付き、壁ジャンプ、クリア判定 |
| Model | StageModel | 足場データ、左右ループ、ステージ／エンドレス生成 |
| 設定 | JumpSettings | ScriptableObjectによるジャンプ感・入力感度の調整 |
| Input | DragJumpInput | Input Systemによる押下・ドラッグ・離上、短時間ドラッグの保持 |
| Core | EventBus | 型付きイベントと購読解除 |
| View | WellView | SpriteRendererの足場・背景・海。画面付近の足場描画を再利用 |
| View | FrogView / PlatformArtwork | 2Dのカエル・足場とジャンプ／着地時の伸縮 |
| View | WellCameraView | 正投影2Dカメラの高さ追従 |
| UI | GameHudView | 配置済みのuGUIとゲーム中のHUDの表示更新 |
| 演出 | FeedbackView | EventBus購読による効果音 |

3Dメッシュや3D物理は使用しません。衝突判定は既存の数値Modelで処理し、SpriteRendererに表示します。ModelはGameObject・入力・カメラに依存しません。

Assets/FrogSea/Resources/FrogJumpSettings.assetで調整できます。演出追加にはJumped / Landed / WallAttached / Fell / GoalReached / RunStartedを購読します。購読時に返るIDisposableを破棄し、シーン遷移時に購読解除します。

## 検証

Unityメニューから実行します。

- **Validate gameplay models**：ジャンプ・着地・左右の継ぎ目・壁ジャンプ・足場の到達可能性。Play中は短時間ドラッグの入力も検証します。
- **Validate 2D scene structure**：Title / InGameのビルド順、軌道スクリプトの除去。Play中は正投影カメラ、3Dメッシュ／軌道線の不在、シーンごとの責務を検証します。
- **Run 2D flow smoke test**：停止中に実行。タイトル→ルール選択→ステージ2→ジャンプと着地→一時停止／再開→タイトル→エンドレス→再挑戦→タイトルを実際のシーンで通します。

結果はLogs/FrogSea-validation.txt、Logs/FrogSea-2D-structure.txt、Logs/FrogSea2D-smoke.txtに出力します。フローテストは終了後にPlayを停止します。プレイヤービルドは未検証です。


## タイトルシーンの編集

タイトルは実行時生成をやめ、カメラ・背景・UI・EventSystemをTitle.unityに保存しています。Play前からGameビューで確認できます。

- **TitleScreen.prefab**（Assets/FrogSea/Prefabs/Title）：Canvas、タイトルホーム、ルール選択画面、TitleController、TitleViewをまとめたプレハブ。文字・位置・サイズは通常のuGUIオブジェクトとして編集します。
- **TitleBackdrop.prefab**（同フォルダー）：レンガ、足場、カエルをまとめた背景。
- **MenuButton.prefab / PrimaryMenuButton.prefab**（Prefabs/UI）：共通ボタンとその強調色のPrefab Variant。タイトル内ではNested Prefabとして配置しています。
- **WellBrick.prefab**（Prefabs/Environment）：繰り返し配置するレンガ。
- **FrogArtwork.prefab**（Prefabs/Characters）：カエルの見た目。
- **Art/Frog.png / Square.png**：保存済みのスプライト。タイトルではSpriteやTextureもPlay時に生成しません。
- **Settings/UI**：EventSystemの保存済み入力アクション参照。

シーンのTitleScreenを選択すると、TitleControllerのView参照を確認できます。TitleViewにパネル・表示テキスト・前後ボタンの参照と、ルール説明文があります。各ButtonのOn Clickには呼び出し先を永続設定しているため、Inspectorから確認・変更できます。TitleModelは選択状態、TitleControllerは操作とシーン遷移、TitleViewは表示更新を担当します。

ルール選択のレイアウトを編集するときは、TitleScreenのCanvas内にあるRule selectionを有効化し、Title homeを無効化して編集できます。Play開始時はホーム表示に戻ります。共通変更はPrefab Mode、シーン固有の調整はPrefab InstanceのOverrideで行えます。

Author or open title sceneは初期配置用のEditorツールです。作成済みのタイトルやプレハブを毎回再構築しません。インゲームも下記の事前配置方式へ移行済みです。

Validate authored titleで配置・参照・永続ボタン接続・保存済みアート／入力を検証できます。Run 2D flow smoke testでは、Play前後でタイトルの全オブジェクト数（非表示パネルを含む）が変わらないことも検証します。



## インゲームシーンとルートの編集

InGame.unityも事前配置方式です。カメラ・カエル・背景壁・床・海・HUD・効果音をInGameRig.prefab内に保存し、Play／再挑戦時はModelとイベント購読だけを作り直します。GameObject、スプライト、AudioClipの実行時生成はありません。

### 主なプレハブ

- Prefabs/Platforms/Platform.prefab：通常足場。Widthで表示と着地判定の幅を変更します。
- Prefabs/Platforms/MossPlatform.prefab：通常足場のVariant。苔の柱への張り付きと壁ジャンプに対応します。
- Prefabs/Platforms/GoalPlatform.prefab：ゴール用の色を設定したVariant。
- Prefabs/Environment/WellWall.prefab：タイル表示の井戸壁。Art/WellBricks.pngを使用します。
- Prefabs/Environment/MossWall.prefab：苔付き足場に組み込む壁の見た目。苔付き足場のStickyが張り付き判定を担当します。
- Prefabs/Environment/WellFloor.prefab、SeaEnding.prefab：床と海の演出。
- Prefabs/Characters/FrogPlayer.prefab：FrogArtworkを組み込んだプレイヤー。
- Prefabs/InGame/FollowCamera.prefab、InGameRig.prefab：カメラと接続済みゲーム全体。
- Prefabs/UI/InGameHUD.prefab：HUD、一時停止、クリア画面。ボタンの呼び出し先は保存済みです。
- Prefabs/Stages/Stage01～03.prefab：編集可能なステージの足場配置。

### 全周に分岐する配置

井戸の1周は32mで、シーンではX=-16～16の展開図として編集します。8列の足場を1周に等間隔で配置し、次の段を半列分ずらしています。左右どちらへ跳んでも次の段へ進め、方向を途中で変えたり、周回しながら登ったりできます。最上段にも8か所のゴール足場があります。

InGameRig内のStage01を展開し、足場Prefab Instanceを移動・複製・追加・削除してください。Play時にStageLayout配下のPlatformDefinitionを読み直し、その座標とWidth / Stickyから判定データを作ります。手作業で配列を登録し直す必要はありません。ゴールの高さはStageLayoutのGoal Heightで設定します。Stage02 / Stage03は初期状態では非表示なので、Prefab Modeで編集するか、シーンで編集したいステージだけを有効にしてください。共通の見た目は足場・壁のPrefab Modeで変更します。

左右のつながりは表示だけの複製ではなく、判定も1周32mで接続しています。カエルの画面内X位置は固定し、足場のArtworkだけを水平移動するため、編集用の足場本体の座標をプレイ中に書き換えません。

エンドレスも同じ全周の分岐配置を使用します。足場データは上方へ追加しますが、表示はシーンに配置済みの128スロットを使い回し、プレハブをプレイ中に追加生成しません。

### 検証

Validate gameplay modelsでは、各足場に左右2方向以上の上昇経路があること、継ぎ目を越える接続があること、各ステージの8方向のゴールが開始地点から到達可能なことを実際のジャンプ計算で検証します。Validate authored ingameは配置・Prefab・UIの接続を検証します。Run 2D flow smoke testでは、インゲームのPlay／再挑戦／エンドレスでも全オブジェクト数が増えないことを確認します。

初期配置ツールはEditor専用です。Author or open ingame sceneは、作成済みのシーンやプレハブを再生成せずに開きます。共通UIや仮スプライトの生成ヘルパーもEditorフォルダーへ移し、プレイヤー側コードから分離しています。


