
import asyncio
import requests

from fastapi import FastAPI
from fastapi.responses import PlainTextResponse

import uvicorn
import urllib.parse
from contextlib import asynccontextmanager

from flair_helper import AnalyzeWithFlair

from colorama import Fore, Style, init

init()  # colorama required on Windows, non required on other OS

@asynccontextmanager
async def lifespan(app: FastAPI):
    await asyncio.sleep(0.1)
    try:
        requests.post(f"http://localhost:{csharp_port}/", data="ready")
        print(f"{Fore.GREEN}[SUCCESS] Sent READY to C#{Style.RESET_ALL}")
    except Exception as e:
        print(f"{Fore.YELLOW}[WARNING] Sending READY to C# at startup failed: {e}{Style.RESET_ALL}")

    yield

app = FastAPI(lifespan=lifespan)

@app.get("/status")
def get_status():
    print(f"{Fore.GREEN}[SUCCESS] Received status check, reply READY{Style.RESET_ALL}")
    return PlainTextResponse("ready")

@app.get("/analyze")
def analyze_text(text: str = ""):
    # Decode URL-escaped string
    text = urllib.parse.unquote(text)
    # print(text)

    results = AnalyzeWithFlair(text)

    # Convert to list of dicts that match C# PresidioEntity
    entities = []
    for r in results:
        entities.append({
            "entity_type": r.entity_type,
            # "score": round(r.score, 2),
            "start": r.start,
            "end": r.end
        })
 
    # print(entities)
    return entities



if __name__ == "__main__":
    host = "localhost"
    python_port = 8000
    csharp_port = 8080

    print(f"{Fore.BLUE}[INFO] Starting Python NER Service on {host}:{python_port}{Style.RESET_ALL}")

    uvicorn.run(
        app, 
        host=host, 
        port=python_port,
        # log_level="warning",
        access_log=False
    )
