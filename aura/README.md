# AURA — Luzia Integration Framework

Framework de integração do AURA com o app Luzia, contendo:
- API Client (HTTP, autenticação, rate limiting)
- Firebase Authentication
- Deep Links Router
- Push Notifications (FCM)
- Configuration Management

## Estrutura

```
aura/
├── api/
│   └── __init__.py          # API client (HTTP, retry, rate limiting)
├── auth/
│   └── __init__.py          # Firebase Auth
├── deep_links/
│   ├── __init__.py          # Package init
│   └── router.py            # Deep links router (luzia://, https://app.luzia.com)
├── native/
│   ├── __init__.py          # Package init
│   └── notifications.py     # FCM push notifications
└── config/
    ├── __init__.py          # Package init
    └── settings.py          # Configuration management
```

## Uso

### API Client

```python
from aura.api import LuziaAPIClient
from aura.config.settings import AURAConfig

api = LuziaAPIClient(AURAConfig.API_BASE_URL)
api.set_auth_token("your_token")
response = api.get("/chat")
```

### Deep Links

```python
from aura.deep_links.router import DeepLinkRouter

router = DeepLinkRouter()
router.route("luzia://chat/12345")
```

### Push Notifications

```python
from aura.native.notifications import FCMClient

fcm = FCMClient(server_key="your_key")
fcm.subscribe_to_topic("device_token", "general")
```

## Configuração

Edite `aura/config/settings.py` para alterar configurações.

## Status

✅ API Client — Implementado
✅ Firebase Auth — Implementado
✅ Deep Links Router — Implementado
✅ Push Notifications — Implementado
✅ Configuration Management — Implementado