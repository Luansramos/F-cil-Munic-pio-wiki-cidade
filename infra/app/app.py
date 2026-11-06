from http.server import BaseHTTPRequestHandler, HTTPServer
import os

class H(BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(b'{"status":"ok","app":"nexo-placeholder"}')

HTTPServer(("0.0.0.0", int(os.getenv("APP_PORT", "5000"))), H).serve_forever()
