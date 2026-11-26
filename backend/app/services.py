import asyncio
import logging
from typing import Dict, List

from crewai import LLM, Agent, Crew, Task
from crewai.flow.flow import Flow, FlowState, listen, start

from app import session as store
from app.config import MODEL_STRING, SharkConfig, load_sharks
from app.schemas import ContextEntry, RoundOut, SharkOutput, SharkResponse, SharkSetup, SharkStatus, UserMessage
from app.session import ActiveShark, Session

logger = logging.getLogger(__name__)


OUT_REPLY = "Yo ya estoy fuera."


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
            + (f"\n{shark.style}" if shark.style else "")
        ),
        verbose=True,
        allow_delegation=False,
        llm=LLM(model=MODEL_STRING),
    )


class SessionState(FlowState):
    """Estado de la sesión (FlowState ya trae `id`). Los agentes no van aquí: se guardan en el Flow."""

    history: List[ContextEntry] = []  # contexto común
    messages: List[UserMessage] = []  # mensajes de la ronda en curso
    addressed: List[str] = []  # tiburones que responden en esta ronda
    responses: List[SharkResponse] = []  # respuestas de la ronda en curso
    denied: List[str] = []  # tiburones "out" a los que se les habló directo: contestan sin LLM
    statuses: Dict[str, SharkStatus] = {}  # última postura de cada tiburón
    attitudes: Dict[str, int] = {}  # última actitud de cada tiburón


class SharkTankFlow(Flow[SessionState]):
    """Una ronda = ingest (el usuario habla) -> panel (los tiburones responden en paralelo)."""

    def __init__(self, active: List[ActiveShark]):
        super().__init__()
        self.active = active
        self.state.statuses = {s.name: "pending" for s in active}

    @start()
    def ingest(self):
        by_name = {s.name.lower(): s.name for s in self.active}
        for m in self.state.messages:
            key = m.recipient.strip().lower()
            recipient = "all" if key == "all" else by_name[key]
            self.state.history.append(ContextEntry(author="Usuario", recipient=recipient, content=m.content))

    @listen(ingest)
    async def panel(self):
        # Foto del contexto: todos responden a lo mismo sin verse entre ellos;
        # las respuestas de esta ronda entran al historial hasta que todos terminen.
        pool = _render_pool(self.state.history)
        sharks = [s for s in self.active if s.name in self.state.addressed]
        outputs = await asyncio.gather(*(_shark_output(s, pool) for s in sharks))
        results = {s.name: out for s, out in zip(sharks, outputs)}
        for shark in self.active:  # respuestas en el orden de la sesión
            if out := results.get(shark.name):
                self.state.history.append(
                    ContextEntry(author=shark.name, recipient="all", content=out.message, summary=out.summary)
                )
                self.state.statuses[shark.name] = out.type
                self.state.attitudes[shark.name] = out.attitude
                self.state.responses.append(
                    SharkResponse(shark=shark.name, type=out.type, content=out.message, attitude=out.attitude)
                )
                logger.info("%s respondió (%s, actitud %d)", shark.name, out.type, out.attitude)
            elif shark.name in self.state.denied:
                # ya está fuera: rechaza sin LLM y el mensaje no entra al historial
                self.state.responses.append(
                    SharkResponse(
                        shark=shark.name, type="out", content=OUT_REPLY, attitude=self.state.attitudes.get(shark.name, -3)
                    )
                )


def start_session(setups: List[SharkSetup]) -> Session:
    shark_catalog = {s.name.lower(): s for s in load_sharks()}
    active, seen = [], set()
    for setup in setups:
        key = setup.name.lower()
        if key not in shark_catalog:
            raise InvalidRequest(f"El tiburón '{setup.name}' no existe")
        if key in seen:
            raise InvalidRequest(f"El tiburón '{setup.name}' está repetido")
        seen.add(key)
        shark_config = shark_catalog[key]
        active.append(ActiveShark(shark_config, build_agent(shark_config, setup.modifier1, setup.modifier2)))
    return store.add(Session(flow=SharkTankFlow(active)))


def _render_pool(context: List[ContextEntry]) -> str:
    """User text goes in full; shark replies go in as their short summary to save tokens."""
    lines = []
    for e in context:
        if e.author == "Usuario":
            lines.append(f"[Usuario → {e.recipient}]: {e.content}")
        else:
            lines.append(f"[{e.author}]: {e.summary or e.content}")
    return "\n".join(lines)


async def _shark_output(shark: ActiveShark, pool: str) -> SharkOutput:
    style = f"\n{shark.config.style}" if shark.config.style else ""
    task = Task(
        description=f"""Eres {shark.name}, un tiburón inversionista en Shark Tank.

Este es el contexto común de la sesión (lo que dijo el usuario y, resumido, lo que
dijeron los demás tiburones). Todos pueden leerlo:

{pool}

Responde a lo que se te dijo (o a todos, si el mensaje era para "all").
Siempre debes enviar un texto en "message" junto con el tipo de respuesta. Elige UNO:
- type "out": NO inviertes. En "message" da tu retroalimentación: por qué te sales y qué debería mejorar.
- type "question": haces una pregunta específica y penetrante al emprendedor.
- type "in": haces una oferta. En "message" indica el monto, el porcentaje de la empresa que pides y por qué.

Además llena:
- attitude: entero de -3 (lo detestas) a 3 (te encanta) sobre el pitch.
- summary: frase de máximo 12 palabras, en tercera persona, que resuma tu postura
  para los otros tiburones (sin saludos ni relleno).

Respeta siempre tu personalidad y estilo de respuesta (incluida su longitud).{style} Responde ÚNICAMENTE en español de México.""",
        expected_output="Respuesta estructurada del tiburón (type, message, summary, attitude)",
        agent=shark.agent,
        output_pydantic=SharkOutput,
    )
    output = (await Crew(agents=[shark.agent], tasks=[task]).kickoff_async()).pydantic
    if output is None:
        raise RuntimeError(f"{shark.name} no devolvió una respuesta estructurada")
    return output


async def handle_messages(session: Session, messages: List[UserMessage]) -> RoundOut:
    """Run one round: the user's messages join the pool; addressed sharks reply in parallel.

    A shark that is already "out" is never called: if addressed directly it just repeats that it is out,
    and a message sent only to it is not added to the shared pool.
    """
    state = session.flow.state
    by_name = {s.name.lower(): s.name for s in session.flow.active}
    addressed, denied, passed = set(), set(), []
    for m in messages:
        key = m.recipient.strip().lower()
        if key == "all":
            addressed.update(n for n in by_name.values() if state.statuses[n] != "out")
            passed.append(m)
        elif key in by_name:
            name = by_name[key]
            if state.statuses[name] == "out":
                denied.add(name)
            else:
                addressed.add(name)
                passed.append(m)
        else:
            raise InvalidRequest(f"El destinatario '{m.recipient}' no está en la sesión")

    async with session.lock:
        state.messages, state.addressed, state.denied, state.responses = passed, sorted(addressed), sorted(denied), []
        await session.flow.kickoff_async()
        return RoundOut(responses=list(state.responses), statuses=dict(state.statuses))
