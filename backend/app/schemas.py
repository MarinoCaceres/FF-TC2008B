from typing import Dict, List, Literal

from pydantic import BaseModel, Field


# ---- GET /sharks
class SharkInfo(BaseModel):
    name: str
    description: str


class SharksOut(BaseModel):
    sharks: List[SharkInfo]


# ---- POST /session
class SharkSetup(BaseModel):
    name: str
    modifier1: int = Field(ge=1, le=5)  # escepticismo (1-5): duda de cifras, afirmaciones y supuestos
    modifier2: int = Field(ge=1, le=5)  # presión (1-5): tono de las preguntas, señala respuestas vagas o evasivas


class SessionCreate(BaseModel):
    sharks: List[SharkSetup] = Field(min_length=1)


class SessionCreated(BaseModel):
    session_id: str


# ---- POST /session/{id}
class UserMessage(BaseModel):
    recipient: str = "all"  # "all" o el nombre de un tiburón
    content: str = Field(min_length=1)


class SessionMessages(BaseModel):
    messages: List[UserMessage] = Field(min_length=1)


class SharkOutput(BaseModel):
    """What the LLM must produce (structured output). `summary` is internal, for the context pool."""
    type: Literal["out", "question", "in"]  # out = rechaza con feedback, question = pregunta, in = hace una oferta
    message: str = Field(
        min_length=1,
        description="Texto para el usuario, siempre presente: retroalimentación si type=out, "
        "la pregunta si type=question, la oferta (monto y porcentaje) si type=in. Español de México",
    )
    summary: str = Field(
        description="Resumen en tercera persona, máx. 12 palabras, solo para los demás tiburones. "
        "Ej: 'No le convence el impacto ambiental'"
    )
    attitude: int = Field(ge=-3, le=3, description="Actitud hacia el pitch: -3 lo detesta, 0 neutral, 3 le encanta")
    hint: str | None = Field(
        default=None,
        description="Solo si type=question: pista corta (máx. 2 oraciones) que ayuda al usuario a contestar "
        "y dice qué espera oír el tiburón. Sin pista (null) en los demás tipos",
    )


class SharkResponse(BaseModel):
    shark: str
    type: Literal["out", "question", "in"]
    content: str
    attitude: int
    hint: str | None = None  # solo en preguntas: guía breve para que el usuario sepa qué responder


SharkStatus = Literal["pending", "question", "in", "out"]  # pending = aún no responde; out es definitivo


class RoundOut(BaseModel):
    responses: List[SharkResponse]
    statuses: Dict[str, SharkStatus]  # estado actual de cada tiburón de la sesión


# ---- shared context pool
class ContextEntry(BaseModel):
    author: str  # "Usuario" o nombre del tiburón
    recipient: str  # "all" o nombre del tiburón (solo usuario)
    content: str  # texto completo
    summary: str | None = None  # versión mínima que leen los demás tiburones


# ---- POST /session/{id}/summary
class Criterion(BaseModel):
    score: int = Field(ge=1, le=10, description="Calificación de 1 (muy mal) a 10 (excelente)")
    comment: str = Field(min_length=1, description="Por qué esa calificación, con ejemplos concretos del chat")


class SummaryOutput(BaseModel):
    """What the coach LLM must produce (structured output) and what the endpoint returns."""
    coherence: Criterion = Field(description="Coherencia: ideas ordenadas, cifras consistentes, sin contradicciones")
    formality: Criterion = Field(description="Formalidad: lenguaje y tono apropiados para presentar a inversionistas")
    approach: Criterion = Field(
        description="Forma de abordar a los tiburones: manejo de preguntas, objeciones y presión; seguridad"
    )
    pitch_quality: Criterion = Field(description="Calidad del pitch: propuesta de valor, números, mercado, petición")
    overall: str = Field(min_length=1, description="Retroalimentación general del desempeño, en un párrafo")
    strengths: List[str] = Field(min_length=1, description="Lo que hizo bien")
    improvements: List[str] = Field(min_length=1, description="Consejos concretos y accionables para mejorar")
