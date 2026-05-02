# TrashRoyale Relay Server

Минимальный WebSocket relay для дружеских боёв (room codes).

## Локально

```bash
cd relay-server
npm install
node server.js
```

Сервер слушает `:8080`, WebSocket endpoint — `/ws?room=CODE&role=host|guest`.

## Деплой на Render

`render.yaml` уже готов. Через Render dashboard:

1. New → Blueprint → подключить репозиторий
2. Render автоматически прочитает `render.yaml`

Через API (заранее задан `RENDER_API_KEY`):

```bash
curl -X POST https://api.render.com/v1/services \
  -H "Authorization: Bearer $RENDER_API_KEY" \
  -H "Content-Type: application/json" \
  -d @./create-service.json
```

После деплоя пропиши URL в `Assets/Scripts/Net/NetMatchSync.cs` (`NetConfig.RelayUrl`).
