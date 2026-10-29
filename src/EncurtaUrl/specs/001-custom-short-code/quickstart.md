# Quickstart: validação manual

Pré-requisito: `dotnet run` na pasta do projeto (porta do `@baseUrl` em `EncurtaUrl.Api.http`). Usar os blocos novos do `.http`. Contrato: [contracts/create-short-url.md](contracts/create-short-url.md).

| # | Cenário | Esperado |
|---|---|---|
| 1 | `POST /urls` com `code: "balta-io"` | 201, `code` = `balta-io`, `shortUrl` termina em `/balta-io` |
| 2 | `GET /balta-io` | 302 para a URL original; `GET /urls/balta-io` mostra `clicks` +1 |
| 2b | `GET /BALTA-IO` (caixa diferente) | 302 para a URL original; clicks +1 |
| 3 | Repetir o #1 | 400, `errors.code` informa uso |
| 4 | `code`: `"balta io"`, `"a/b"`, `"olá"` | 400, mensagem de formato |
| 5 | `code` com 2 e 16 caracteres | 400, mensagem de tamanho; 3 e 15 aceitos |
| 6 | `code`: `"urls"` e `"URLS"` | 400, reservado |
| 7 | `url` inválida + `code: "!"` | 400 com `url` e `code` |
| 8 | `code`: `""` / `"   "` / omitido | 201 com código gerado |
| 9 | `code: "BALTA-IO"` após `balta-io` | 400, em uso (ignora caixa) |
| 10 | `code: " abc"` | 400 (sem trim) |
| 11 | Duas requisições simultâneas, mesmo `code` | uma 201, outra 400 |
| 12 | `GET /urls`, `GET /urls/{code}` | mesma estrutura para gerado e customizado |

Build: `dotnet build` sem warnings novos.
