# Nugget - 社内ToDo管理アプリケーション

社内全員への有効期限付きToDoを登録・管理できるアプリケーションです。月末の工数入力対応期限など、全社員に対してやってもらうべきことをToDoリストとして一箇所で管理し、対応漏れを減らして対応率を上げることを目的としています。

## 機能

### Phase 1 (MVP)
- ✅ SAML 2.0認証（Okta/Azure AD/Google Workspace対応）
- ✅ ToDo一覧表示（メイン画面）
- ✅ ToDo作成（管理者用）
- ✅ 完了チェック機能
- ✅ フィルタ機能（未完了/完了/期限切れ）
- ✅ ソート機能（期限順/作成日順）
- ✅ Slack通知（新規ToDo/更新/リマインダー）
- ✅ 期限リマインダー（カスタマイズ可能）

### Phase 2 (予定)
- [ ] SCIM 2.0対応（ユーザー自動プロビジョニング）
- [ ] グループ対応
- [ ] 通知設定カスタマイズUI

### Phase 3 (予定)
- [ ] 統計ダッシュボード
- [ ] モバイル最適化

## 技術スタック

- **Backend**: .NET 10 / ASP.NET Core Web API
- **Frontend**: Blazor WebAssembly
- **Database**: PostgreSQL 16
- **認証**: SAML 2.0 (Sustainsys.Saml2)
- **通知**: Slack (SlackNet)

## ローカル開発

### 必要条件
- .NET 10 SDK
- Docker & Docker Compose
- PostgreSQL (Docker経由)

### セットアップ

1. リポジトリをクローン
```bash
git clone <repository-url>
cd nugget
```

2. 環境変数を設定
```bash
cp docker/.env.example docker/.env
# docker/.env を編集してSlack Bot Tokenなどを設定
```

3. Docker Composeで起動
```bash
cd docker
docker compose up -d
```

4. アクセス
- フロントエンド: http://localhost:5173
- API: http://localhost:5000
- API Health Check: http://localhost:5000/health

### ローカル開発（Docker不使用）

```bash
# PostgreSQLを起動（別途インストール必要）
# データベースを作成

# APIを起動
cd src/Nugget.Api
dotnet run

# フロントエンドを起動（別ターミナル）
cd src/Nugget.Web
dotnet run
```

## 設定

### SAML設定（Okta例）

`appsettings.json` または環境変数:

```json
{
  "Saml": {
    "EntityId": "https://nugget.company.com",
    "ReturnUrl": "https://nugget.company.com",
    "IdpEntityId": "http://www.okta.com/exampleidp",
    "IdpMetadataUrl": "https://your-org.okta.com/app/your-app/sso/saml/metadata",
    "IdpSsoUrl": "https://your-org.okta.com/app/your-app/sso/saml"
  }
}
```

### Slack設定

```json
{
  "Slack": {
    "BotToken": "xoxb-your-bot-token",
    "AppUrl": "https://nugget.company.com"
  }
}
```

## プロジェクト構成

```
nugget/
├── src/
│   ├── Nugget.Api/          # ASP.NET Core Web API
│   ├── Nugget.Web/          # Blazor WebAssembly Frontend
│   ├── Nugget.Core/         # ドメインモデル・インターフェース
│   └── Nugget.Infrastructure/ # データアクセス・外部連携
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── nginx.conf
└── README.md
```

## License

MIT License
