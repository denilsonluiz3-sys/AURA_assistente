"""
AURA — Deep Links Router
Handles luzia:// scheme and https://app.luzia.com URLs
"""
import re
from typing import Optional, Dict, Callable, Any
from urllib.parse import urlparse, parse_qs

class DeepLinkRouter:
    """Routes deep links to appropriate handlers."""
    
    # URL schemes
    CUSTOM_SCHEME = "luzia"
    HTTPS_HOSTS = ["app.luzia.com", "share.luzia.com"]
    
    def __init__(self):
        self.routes: Dict[str, Callable] = {}
        self._register_default_routes()
    
    def _register_default_routes(self):
        """Register default deep link handlers."""
        self.routes["chat"] = self._handle_chat
        self.routes["profile"] = self._handle_profile
        self.routes["documents"] = self._handle_documents
        self.routes["reminders"] = self._handle_reminders
        self.routes["settings"] = self._handle_settings
        self.routes["auth"] = self._handle_auth
        self.routes["share"] = self._handle_share
    
    def parse(self, url: str) -> Optional[Dict[str, Any]]:
        """Parse a deep link URL and return action data."""
        if not url:
            return None
        
        parsed = urlparse(url)
        
        # Handle custom scheme: luzia://chat/123
        if parsed.scheme == self.CUSTOM_SCHEME:
            path = parsed.path.lstrip("/")
            query = parse_qs(parsed.query)
            
            # Extract action and parameters
            parts = path.split("/") if path else []
            action = parts[0] if parts else "home"
            params = parts[1:] if len(parts) > 1 else []
            
            return {
                "scheme": self.CUSTOM_SCHEME,
                "action": action,
                "params": params,
                "query": query,
                "host": parsed.netloc,
            }
        
        # Handle HTTPS URLs: https://app.luzia.com/chat/123
        if parsed.scheme == "https" and parsed.netloc in self.HTTPS_HOSTS:
            path = parsed.path.lstrip("/")
            query = parse_qs(parsed.query)
            
            parts = path.split("/") if path else []
            action = parts[0] if parts else "home"
            params = parts[1:] if len(parts) > 1 else []
            
            return {
                "scheme": "https",
                "action": action,
                "params": params,
                "query": query,
                "host": parsed.netloc,
            }
        
        return None
    
    def route(self, url: str) -> bool:
        """Route a deep link to its handler. Returns True if handled."""
        parsed = self.parse(url)
        if not parsed:
            return False
        
        action = parsed["action"]
        handler = self.routes.get(action)
        
        if handler:
            handler(parsed)
            return True
        
        return False
    
    # --- Handlers ---
    
    def _handle_chat(self, data: Dict):
        """Handle chat deep link."""
        params = data.get("params", [])
        chat_id = params[0] if params else None
        print(f"Opening chat: {chat_id}")
        # Emit event to open chat screen
    
    def _handle_profile(self, data: Dict):
        """Handle profile deep link."""
        params = data.get("params", [])
        user_id = params[0] if params else None
        print(f"Opening profile: {user_id}")
    
    def _handle_documents(self, data: Dict):
        """Handle documents deep link."""
        print("Opening documents")
    
    def _handle_reminders(self, data: Dict):
        """Handle reminders deep link."""
        print("Opening reminders")
    
    def _handle_settings(self, data: Dict):
        """Handle settings deep link."""
        print("Opening settings")
    
    def _handle_auth(self, data: Dict):
        """Handle auth deep link."""
        query = data.get("query", {})
        print(f"Auth flow: {query}")
    
    def _handle_share(self, data: Dict):
        """Handle share deep link."""
        query = data.get("query", {})
        print(f"Share: {query}")

# Example usage
if __name__ == "__main__":
    router = DeepLinkRouter()
    
    # Test URLs
    test_urls = [
        "luzia://chat/12345",
        "luzia://profile/user_678",
        "luzia://documents",
        "https://app.luzia.com/chat/abc",
        "https://share.luzia.com/article/xyz",
    ]
    
    for url in test_urls:
        parsed = router.parse(url)
        print(f"URL: {url}")
        print(f"Parsed: {parsed}")
        print("---")