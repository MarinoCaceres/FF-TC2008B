import logging
import os
from pathlib import Path
from typing import List

import yaml
from dotenv import load_dotenv
from pydantic import BaseModel

load_dotenv()
logging.basicConfig(level=logging.INFO)

API_TYPE = os.getenv("API_TYPE", "azure").lower()
PORT = int(os.getenv("BACKEND_PORT", 8000))
SHARKS_FILE = Path(os.getenv("SHARKS_FILE", Path(__file__).parent.parent / "config" / "sharks.yml"))


def _require(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise RuntimeError(f"Falta la variable de entorno: {name}")
    return value


def _build_model_string() -> str:
    if API_TYPE == "openai":
        _require("OPENAI_API_KEY")
        return os.getenv("OPENAI_MODEL", "gpt-4")
    if API_TYPE == "azure":
        os.environ["AZURE_API_KEY"] = _require("AZURE_API_KEY")
        os.environ["AZURE_API_BASE"] = _require("AZURE_ENDPOINT")
        os.environ["AZURE_API_VERSION"] = os.getenv("AZURE_API_VERSION", "2024-02-01")
        return f"azure/{os.getenv('AZURE_DEPLOYMENT', 'gpt-4o-mini')}"
    raise ValueError("API_TYPE debe ser 'openai' o 'azure'")


MODEL_STRING = _build_model_string()


class SharkConfig(BaseModel):
    name: str
    description: str  # texto corto para mostrar en la UI
    role: str
    goal: str
    backstory: str


def load_sharks() -> List[SharkConfig]:
    data = yaml.safe_load(SHARKS_FILE.read_text(encoding="utf-8"))
    sharks = [SharkConfig(**s) for s in data["sharks"]]
    if not sharks:
        raise RuntimeError(f"{SHARKS_FILE} no define ningún tiburón")
    return sharks
