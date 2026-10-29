# Contrato: POST /urls

## Request

```json
{ "url": "https://balta.io", "code": "balta-io" }
```

`code` opcional (ausente, `null`, vazio ou só espaços = gerar automaticamente).

## Respostas

| Status | Quando | Corpo |
|---|---|---|
| 201 | criado; `Location: /urls/{code}` | `ShortUrlResponse` (inalterado) |
| 400 | `url` e/ou `code` inválidos, ou `code` já em uso (ignora caixa; inclui corrida) | `ValidationProblem` com chaves `url` e/ou `code` |
| 500 | falha ao gerar código único (sem `code`) | ProblemDetails (como hoje) |

### 400 exemplo

```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "code": ["O código deve ter entre 3 e 15 caracteres."] } }
```

### 400 exemplo (código em uso)

```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "code": ["O código 'balta-io' já está em uso."] } }
```

## Inalterados

`GET /urls`, `GET /urls/{code}`, `GET /{code}` (302 + clique).
