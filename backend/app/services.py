import logging
from typing import List

from crewai import LLM, Agent, Crew, Task

from app import session as store
from app.config import MODEL_STRING, SharkConfig, load_sharks
from app.schemas import ContextEntry, SharkOutput, SharkResponse, SharkSetup, UserMessage
from app.session import ActiveShark, Session

logger = logging.getLogger(__name__)


class InvalidRequest(ValueError):
    """The client asked for something that doesn't exist (unknown shark, etc.)."""


def build_agent(shark: SharkConfig, modifier1: int, modifier2: int) -> Agent:
    return Agent(
        role=shark.role,
        goal=shark.goal,
        backstory=(
            f"{shark.backstory}\n"
            "Tu personalidad en esta sesión (escala del 1 al 5):\n"
            f"- Escepticismo = {modifier1}: qué tanto dudas de lo que te dicen (cifras, afirmaciones, "
            "supuestos). Con 4-5 cuestionas y pides pruebas de cada dato; con 1-2 "
            "les das el beneficio de la duda.\n"
            f"- Presión = {modifier2}: el tono de tus preguntas. Con 4-5 eres directo y duro, "
            "señalas respuestas vagas o evasivas y las repites hasta que te contesten; con 1-2 "
            "eres amable y no insistes.\n"
            "Hablas siempre en español de México, con tono natural y profesional."
        ),
        verbose=True,
        allow_delegation=False,
        llm=LLM(model=MODEL_STRING),
    )


def start_session(setups: List[SharkSetup]) -> Session:
    catalog = {s.name.lower(): s for s in load_sharks()}
    active, seen = [], set()
    for setup in setups:
        key = setup.name.lower()
        if key not in catalog:
            raise InvalidRequest(f"El tiburón '{setup.name}' no existe")
        if key in seen:
            raise InvalidRequest(f"El tiburón '{setup.name}' está repetido")
        seen.add(key)
        cfg = catalog[key]
        active.append(ActiveShark(cfg, build_agent(cfg, setup.modifier1, setup.modifier2), setup.modifier1, setup.modifier2))
    return store.add(Session(sharks=active))


def _render_pool(context: List[ContextEntry]) -> str:
    """User text goes in full; shark replies go in as their short summary to save tokens."""
    lines = []
    for e in context:
        if e.author == "Usuario":
            lines.append(f"[Usuario → {e.recipient}]: {e.content}")
        else:
            lines.append(f"[{e.author}]: {e.summary or e.content}")
    return "\n".join(lines)


def _shark_output(shark: ActiveShark, session: Session) -> SharkOutput:
    task = Task(
        description=f"""Eres {shark.name}, un tiburón inversionista en Shark Tank.

Este es el contexto común de la sesión (lo que dijo el usuario y, resumido, lo que
dijeron los demás tiburones). Todos pueden leerlo:

{_render_pool(session.context)}

Responde a lo que se te dijo (o a todos, si el mensaje era para "all"). Elige UNO:
- type "question": una pregunta específica y penetrante para el emprendedor.
- type "in": decides invertir (explica tu oferta y por qué).
- type "out": decides NO invertir (explica por qué).

Además llena:
- attitude: entero de -3 (lo detestas) a 3 (te encanta) sobre el pitch.
- summary: frase de máximo 12 palabras, en tercera persona, que resuma tu postura
  para los otros tiburones (sin saludos ni relleno).

Sé crítico pero constructivo. Responde ÚNICAMENTE en español de México.""",
        expected_output="Respuesta estructurada del tiburón (type, message, summary, attitude)",
        agent=shark.agent,
        output_pydantic=SharkOutput,
    )
    output = Crew(agents=[shark.agent], tasks=[task]).kickoff().pydantic
    if output is None:
        raise RuntimeError(f"{shark.name} no devolvió una respuesta estructurada")
    return output


def handle_messages(session: Session, messages: List[UserMessage]) -> List[SharkResponse]:
    """Add the user's messages to the pool (all sharks can read them); only addressed sharks reply."""
    by_name = {s.name.lower(): s for s in session.sharks}
    addressed = set()
    for m in messages:
        recipient = m.recipient.strip()
        if recipient.lower() == "all":
            addressed.update(by_name)
        elif recipient.lower() in by_name:
            addressed.add(recipient.lower())
        else:
            raise InvalidRequest(f"El destinatario '{m.recipient}' no está en la sesión")

    for m in messages:
        recipient = "all" if m.recipient.strip().lower() == "all" else by_name[m.recipient.strip().lower()].name
        session.context.append(ContextEntry(author="Usuario", recipient=recipient, content=m.content))

    responses = []
    for shark in session.sharks:  # session order; each reply is visible to the next shark
        if shark.name.lower() not in addressed:
            continue
        out = _shark_output(shark, session)
        session.context.append(
            ContextEntry(author=shark.name, recipient="all", content=out.message, summary=out.summary)
        )
        responses.append(SharkResponse(shark=shark.name, type=out.type, content=out.message, attitude=out.attitude))
        logger.info("%s respondió (%s, actitud %d)", shark.name, out.type, out.attitude)
    return responses
