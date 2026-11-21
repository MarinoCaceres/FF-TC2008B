import logging
from typing import List

from crewai import LLM, Agent, Crew, Task

from app.config import MODEL_STRING, SharkConfig
from app.schemas import ContextEntry, SharkReply
from app.session import Session

logger = logging.getLogger(__name__)


def build_agent(shark: SharkConfig) -> Agent:
    return Agent(
        role=shark.role,
        goal=shark.goal,
        backstory=shark.backstory + "\nHablas siempre en español de México, con tono natural y profesional.",
        verbose=True,
        allow_delegation=False,
        llm=LLM(model=MODEL_STRING),
    )


def _render_pool(context: List[ContextEntry]) -> str:
    """User text goes in full; shark replies go in as their short summary to save tokens."""
    return "\n".join(
        f"[{e.author}]: {e.summary or e.content}" if e.kind == "tiburon" else f"[{e.author}]: {e.content}"
        for e in context
    )


def _shark_reply(shark: SharkConfig, agent: Agent, session: Session) -> SharkReply:
    task = Task(
        description=f"""Eres {shark.name}, un tiburón inversionista en Shark Tank.

Este es el contexto común de la sesión (el pitch del usuario y lo que ya dijeron
los demás tiburones, resumido):

{_render_pool(session.context)}

Procesa la idea y responde con UNA de estas opciones:
- "pregunta": una pregunta específica y penetrante para el emprendedor.
- "calificacion": una calificación del 1 al 10 (campo score) con tu justificación.
- "decision": tu decisión final (campo invests: true o false) con tu justificación.

Llena también "summary": una frase de máximo 12 palabras, en tercera persona,
que resuma tu postura para los otros tiburones (sin saludos ni relleno).

Sé crítico pero constructivo. Responde ÚNICAMENTE en español de México.""",
        expected_output="Una respuesta estructurada del tiburón (type, message, summary, score, invests)",
        agent=agent,
        output_pydantic=SharkReply,
    )
    result = Crew(agents=[agent], tasks=[task]).kickoff()
    reply = result.pydantic
    if reply is None:
        raise RuntimeError(f"{shark.name} no devolvió una respuesta estructurada")
    reply.shark = shark.name
    return reply


def run_round(session: Session) -> List[SharkReply]:
    """ Sharks answers and pool creation"""
    replies = []
    for shark, agent in zip(session.sharks, session.agents):
        reply = _shark_reply(shark, agent, session)
        session.context.append(
            ContextEntry(
                author=shark.name, kind="tiburon", content=reply.message, summary=reply.summary, reply=reply
            )
        )
        replies.append(reply)
        logger.info("%s respondió (%s)", shark.name, reply.type)
    return replies
