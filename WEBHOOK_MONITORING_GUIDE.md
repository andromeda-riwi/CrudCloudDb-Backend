# 🔔 Guía: Cómo Ver y Monitorear Webhooks

## 📍 5 Formas de Ver Webhooks

### **1️⃣ Swagger UI (La forma más fácil)**

```
🌐 http://localhost:5063/swagger
```

**Qué ver:**
- Abre la sección verde "Webhooks"
- Verás 8 endpoints disponibles
- Haz clic en cualquiera y presiona "Try it out"
- Verás respuestas en JSON

**Pantalla:**
```
┌─────────────────────────────────────────┐
│ Swagger UI - CCD API                    │
├─────────────────────────────────────────┤
│                                         │
│ 🟢 Webhooks (8 endpoints)               │
│  ├─ GET   /api/webhooks                 │
│  ├─ POST  /api/webhooks                 │
│  ├─ GET   /api/webhooks/{id}            │
│  ├─ PUT   /api/webhooks/{id}            │
│  ├─ PATCH /api/webhooks/{id}/toggle     │
│  ├─ DELETE /api/webhooks/{id}           │
│  ├─ GET   /api/webhooks/{id}/history    │
│  └─ POST  /api/webhooks/{id}/test       │
│                                         │
└─────────────────────────────────────────┘
```

---

### **2️⃣ Consola / Terminal (Logs en tiempo real)**

Cuando ejecutas `dotnet run`:

```bash
dotnet run --project src/CCD.Api
```

**Verás logs automáticos como:**

```
[12:30:45 UTC] info: CCD.Infrastructure.Services.WebhookService
               Evento webhook disparado
               EventType: database.created
               WebhookUrl: https://webhook.site/a1b2c3d4...
               Status: 200 OK
               Duration: 245ms

[12:31:15 UTC] info: CCD.Infrastructure.Services.WebhookService
               Webhook entregado exitosamente
               EventType: account.created
               Attempts: 1
               Timestamp: 2025-11-18T12:31:15Z

[12:32:00 UTC] warn: CCD.Infrastructure.Services.WebhookService
               Webhook falló, reintentando...
               WebhookId: 3fa85f64-5717-4562-b3fc-2c963f66afa6
               Attempt: 2/4
               NextRetry: en 30 segundos
               Error: Connection timeout

[12:33:00 UTC] error: CCD.Infrastructure.Services.WebhookService
               Webhook no entregado
               WebhookId: 3fa85f64-5717-4562-b3fc-2c963f66afa6
               Attempts: 4 (fallidas todas)
               Status: FAILED
```

---

### **3️⃣ Endpoint GET - Historial de Eventos**

```bash
curl -X GET "http://localhost:5063/api/webhooks/{webhookId}/history" \
  -H "Authorization: Bearer {tu_jwt_token}"
```

**Response:**
```json
{
  "webhookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "totalEvents": 12,
  "events": [
    {
      "id": "evt-001",
      "eventType": "database.created",
      "status": "delivered",
      "statusCode": 200,
      "attempts": 1,
      "timestamp": "2025-11-18T12:30:00Z",
      "duration": 245,
      "response": "OK"
    },
    {
      "id": "evt-002",
      "eventType": "account.created",
      "status": "delivered",
      "statusCode": 200,
      "attempts": 1,
      "timestamp": "2025-11-18T12:31:00Z",
      "duration": 189,
      "response": "OK"
    },
    {
      "id": "evt-003",
      "eventType": "database.deleted",
      "status": "failed_after_retries",
      "statusCode": 500,
      "attempts": 4,
      "timestamp": "2025-11-18T12:32:00Z",
      "error": "Connection timeout",
      "lastRetryAt": "2025-11-18T12:33:00Z"
    }
  ]
}
```

---

### **4️⃣ Sitio Externo: Webhook.site (Recomendado para testing)**

```
🌐 https://webhook.site
```

**Pasos:**

1. **Visita webhook.site:**
   ```
   https://webhook.site
   ```

2. **Copia tu URL única:**
   ```
   https://webhook.site/a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6
   ```

3. **Crea webhook en la API:**
   ```bash
   curl -X POST "http://localhost:5063/api/webhooks" \
     -H "Authorization: Bearer {jwt_token}" \
     -H "Content-Type: application/json" \
     -d '{
       "url": "https://webhook.site/a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
       "secret": "mi_secreto_123",
       "events": ["database.created", "account.created"],
       "isActive": true
     }'
   ```

4. **Dispara un evento** (crea una BD, registra usuario, etc.)

5. **Mira en webhook.site en tiempo real:**
   ```
   ┌────────────────────────────────────────┐
   │ Requests (3)                           │
   ├────────────────────────────────────────┤
   │                                        │
   │ POST https://webhook.site/a1b2c...    │
   │ 200 OK - 245ms ago                     │
   │                                        │
   │ Headers:                               │
   │ ├─ Content-Type: application/json      │
   │ ├─ X-Webhook-Signature: t=123...,v1=..│
   │ └─ User-Agent: Andromeda-API/1.0       │
   │                                        │
   │ Body:                                  │
   │ {                                      │
   │   "eventType": "database.created",     │
   │   "timestamp": "2025-11-18T12:30...",  │
   │   "data": {                            │
   │     "databaseId": "guid-123",          │
   │     "name": "user_db_123",             │
   │     "engine": "PostgreSQL"             │
   │   }                                    │
   │ }                                      │
   │                                        │
   └────────────────────────────────────────┘
   ```

---

### **5️⃣ Base de Datos (PgAdmin)**

**Tablas:**
- `Webhooks` - Definiciones de webhooks
- `WebhookEvents` - Histórico de eventos

**Consultas SQL:**

```sql
-- Ver mis webhooks
SELECT id, url, events, is_active, created_at 
FROM "Webhooks" 
WHERE user_id = 'guid-usuario'
ORDER BY created_at DESC;

-- Ver últimos eventos
SELECT id, webhook_id, event_type, status, status_code, created_at 
FROM "WebhookEvents" 
ORDER BY created_at DESC 
LIMIT 20;

-- Ver webhooks con problemas
SELECT 
  w.id, 
  w.url, 
  COUNT(we.id) as total_events,
  SUM(CASE WHEN we.status = 'delivered' THEN 1 ELSE 0 END) as delivered,
  SUM(CASE WHEN we.status = 'failed' THEN 1 ELSE 0 END) as failed
FROM "Webhooks" w
LEFT JOIN "WebhookEvents" we ON w.id = we.webhook_id
GROUP BY w.id, w.url
HAVING SUM(CASE WHEN we.status = 'failed' THEN 1 ELSE 0 END) > 0;
```

---

## 🎯 Resumen Rápido

| Forma | Ubicación | Mejor Para |
|-------|-----------|-----------|
| **Swagger** | `http://localhost:5063/swagger` | Crear, editar, testear webhooks |
| **Consola** | Terminal donde ejecutas `dotnet run` | Ver logs en tiempo real |
| **API History** | `GET /api/webhooks/{id}/history` | Ver historial detallado |
| **Webhook.site** | `https://webhook.site` | Testing sin servidor propio |
| **Base de Datos** | PgAdmin en `localhost:5050` | Auditoría y estadísticas |

---

## 🧪 Ejemplo Completo: Testing de Webhooks

### **Paso 1: Crear Webhook**
```bash
curl -X POST "http://localhost:5063/api/webhooks" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://webhook.site/abc123def456",
    "secret": "supersecret123",
    "events": ["database.created"],
    "isActive": true
  }'
```

### **Paso 2: Ver en Swagger**
- Ve a `http://localhost:5063/swagger`
- Busca "Webhooks"
- Haz GET /api/webhooks
- Verás tu webhook creado

### **Paso 3: Crear una BD (dispara webhook)**
```bash
curl -X POST "http://localhost:5063/api/databases" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{
    "engine": "PostgreSQL",
    "timeZoneId": "UTC"
  }'
```

### **Paso 4: Ver en Consola**
Mira en la terminal donde corre `dotnet run`, verás logs como:
```
info: CCD.Infrastructure.Services.WebhookService
      Webhook enviado: database.created
      Status: 200
      Duration: 234ms
```

### **Paso 5: Ver en Webhook.site**
Abre `https://webhook.site/abc123def456` en navegador y verás el evento recibido en tiempo real.

### **Paso 6: Ver Historial**
```bash
curl -X GET "http://localhost:5063/api/webhooks/{webhookId}/history" \
  -H "Authorization: Bearer eyJhbGci..."
```

---

## 🆘 Troubleshooting

| Problema | Solución |
|----------|----------|
| No veo logs en consola | Verifica que el nivel de log sea "Information" en appsettings.json |
| Webhook no se envía | Verifica que esté activo (`isActive: true`) |
| Error 401 en Swagger | Tu token JWT expiró, genera uno nuevo |
| Status 500 en webhook.site | Revisa los logs de consola para ver el error exacto |
| No veo eventos en historial | Confirma que el evento se haya disparado (ej: crear BD) |

---

**¡Con estas 5 formas siempre sabrás exactamente qué está pasando con tus webhooks!** 🚀

