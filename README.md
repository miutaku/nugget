# Nugget - 社内ToDo管理アプリケーション

社内全員への有効期限付きToDoを登録・管理できるアプリケーションです。

月末の工数入力対応期限など、全社員に対してやってもらうべきことをToDoリストとして一箇所で管理し、対応漏れを減らして対応率を上げることを目的としています。

## 機能

- **SAML 2.0認証**: Okta, Azure AD, Google WorkspaceなどのIdPと連携
- **ToDo管理**:
  - 管理者による全社・グループ・個人へのToDo作成
  - 期限設定とリマインダー
  - 完了状況のリアルタイム集計
- **進捗管理**:
  - **分析ダッシュボード**: システム全体の利用状況と達成率を可視化
  - **進捗管理リスト**: 作成したタスクの完了状況（誰が未完了か）を一覧確認
- **通知連携**: Slackへの新規ToDo通知、リマインダー通知
- **グループ管理**: SCIM 2.0 または手動によるグループ管理

## 技術スタック

- **Backend**: .NET / ASP.NET Core Web API
- **Frontend**: Blazor WebAssembly
- **Database**: PostgreSQL 16
- **Cache**: Redis (予定)
- **Container**: Docker / Docker Compose

## クイックスタート (Docker)

Docker Composeを使用して、ローカル環境ですぐにアプリケーションを起動できます。

### 1. リポジトリのクローン
```bash
git clone https://github.com/miutaku/nugget.git
cd nugget
```

### 2. 環境変数の設定
`docker` ディレクトリ内の `.env.example` をコピーして `.env` を作成し、必要な値を設定します。

```bash
cd docker
cp .env.example .env
nano .env # またはお好みのエディタで編集
```

### 3. 証明書の配置 (SAML認証用)
SAML IdPから取得した証明書ファイル（`.cert` または `.pem`）を `docker` ディレクトリに配置します。デフォルトのファイル名は `idp_cert.cert` です。
※ ファイル名を変える場合は、`.env` の `SAML_IDP_CERTIFICATE_PATH` も変更してください。

### 4. アプリケーションの起動
```bash
docker compose up -d
```

### 5. アクセス
- **フロントエンド**: http://localhost:5173
- **API**: http://localhost:5000

## 設定 (Environment Variables)

`docker/.env` で設定可能な環境変数の一覧です。

### 認証 (SAML / SCIM)

| 変数名 | 必須 | 説明 | デフォルト値 / 例 |
|--------|------|------|-------------------|
| `SAML_IDP_ENTITY_ID` | ✅ | IdPのEntity ID (Issuer) | `http://www.okta.com/...` |
| `SAML_IDP_SSO_URL` | ✅ | IdPのSSO URL (Login URL) | `https://your-org.okta.com/.../sso/saml` |
| `SAML_IDP_METADATA_URL` | | IdPのメタデータXMLのURL | |
| `SAML_IDP_CERTIFICATE_PATH`| | コンテナ内の証明書パス | `/app/idp_cert.cert` |
| `SAML_ADMIN_EMAILS` | ✅ | 管理者権限を付与するメールアドレス (カンマ区切り) | `admin@example.com,user@example.com` |
| `SCIM_API_TOKEN` | ✅ | SCIM API認証用トークン (任意の文字列) | (ランダムな文字列を生成してください) |

### 通知 (Slack)

| 変数名 | 必須 | 説明 | デフォルト値 / 例 |
|--------|------|------|-------------------|
| `SLACK_BOT_TOKEN` | ✅ | Slack AppのBot User OAuth Token | `xoxb-...` |

### データベース接続 (docker-compose.yml内で定義)
デフォルトでは `docker-compose.yml` 内で `postgres` コンテナへの接続が設定されています。変更が必要な場合は `docker-compose.yml` の `ConnectionStrings__DefaultConnection` を編集してください。

## SCIM プロビジョニング設定 (例: Okta)

Okta からユーザー・グループを自動同期するための設定手順です。

### Okta 側の設定

| 設定項目 | 値 |
|---------|-----|
| **SCIM バージョン** | 2.0 |
| **SCIMコネクターのベースURL** | `https://<your-domain>/api/scim/v2` |
| **ユーザーの一意のIDフィールド** | `userName` |
| **認証モード** | HTTP Header |
| **Authorization** | `Bearer <SCIM_API_TOKENの値>` |

### サポートされているプロビジョニングアクション

| アクション | 対応 |
|----------|------|
| Import New Users | ✅ |
| Import Groups | ✅ |
| 新規ユーザーをプッシュ | ✅ |
| プロファイルの更新をプッシュ | ✅ |
| プッシュグループ | ✅ |

### Enterprise User 属性マッピング

SCIM Enterprise User Schema (`urn:ietf:params:scim:schemas:extension:enterprise:2.0:User`) に対応しています。以下の属性がユーザーに同期され、ToDo の属性ベースターゲティングに利用できます。

| SCIM 属性 | Nugget フィールド | 用途例 |
|----------|-----------------|--------|
| `department` | 部署 | 「営業部」全員にToDo割り当て |
| `division` | 事業部 | 事業部単位での割り当て |
| `title` | 役職 | 役職ベースの割り当て |
| `organization` | 組織 | 組織単位での割り当て |
| `costCenter` | コストセンター | コストセンター単位での割り当て |
| `employeeNumber` | 社員番号 | 個人の識別 |

## ディレクトリ構成

```
nugget/
├── src/
│   ├── Nugget.Api/          # Backend API
│   ├── Nugget.Web/          # Frontend (Blazor WASM)
│   ├── Nugget.Core/         # Domain Entities & Interfaces
│   └── Nugget.Infrastructure/ # EF Core & External Services
├── docker/                  # Docker configuration
│   ├── .env                 # Environment variables (git-ignored, copy from .env.example)
│   ├── docker-compose.yml   # Container configuration
│   ├── Dockerfile           # Multi-stage build definition
│   └── nginx.conf           # Web server configuration
└── tests/                   # Unit Tests
```

## ライセンス

MIT License
