from typing import List, Literal, Optional

from pydantic import BaseModel, Field


class PitchRequest(BaseModel):
    pitch: str = Field(min_length=1, description="Descripción completa de la idea y el pitch, en texto libre")


class MessageRequest(BaseModel):
    content: str = Field(min_length=1, description="Respuesta o comentario del usuario a los tiburones")


class SharkReply(BaseModel):
    """Shark output"""
    shark: str = ""  # lo llena el servicio, no el LLM
    type: Literal["pregunta", "calificacion", "decision"]
    message: str = Field(description="Respuesta completa para el usuario, en español de México")
    summary: str = Field(
        description="Resumen en tercera persona, máx. 12 palabras, solo para los demás tiburones. "
        "Ej: 'No le convence el impacto ambiental'"
    )
    score: Optional[int] = Field(default=None, ge=1, le=10, description="Calificación 1-10 (si aplica)")
    invests: Optional[bool] = Field(default=None, description="True/False solo si type es 'decision'")


class ContextEntry(BaseModel):
    author: str  # "Usuario" o el nombre del tiburón
    kind: Literal["usuario", "tiburon"]
    content: str  # texto completo (lo que ve el usuario)
    summary: Optional[str] = None  # versión mínima (lo que ven los demás tiburones)
    reply: Optional[SharkReply] = None


class SessionOut(BaseModel):
    session_id: str
    sharks: List[str]
    context: List[ContextEntry]


class RoundOut(BaseModel):
    session_id: str
    replies: List[SharkReply]
