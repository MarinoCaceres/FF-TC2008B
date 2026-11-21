import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.config import load_sharks
from app.schemas import ContextEntry
from app.services import build_agent, run_round
from app.session import Session

PITCH = (
    "Hola Tiburones, soy Alex. Mi startup EcoTrack es un sistema de gestión de energía "
    "para casas que usa IA para reducir el consumo. Tenemos 500 usuarios beta y $50,000 "
    "USD en preórdenes. Vendemos hardware más una suscripción mensual. Busco $300,000 USD "
    "por el 10% de la empresa para mejorar el producto, marketing y crecer el equipo."
)


def test_shark_resolves_pitch_end_to_end():
    sharks = load_sharks()[:1]  # solo un tiburón
    session = Session(sharks=sharks, agents=[build_agent(s) for s in sharks])
    session.context.append(ContextEntry(author="Usuario", kind="usuario", content=PITCH))

    replies = run_round(session)

    assert len(replies) == 1
    reply = replies[0]
    assert reply.shark == sharks[0].name
    assert reply.type in ("pregunta", "calificacion", "decision")
    assert reply.message.strip()
    assert reply.summary.strip() and len(reply.summary) < len(reply.message)
    if reply.type == "calificacion":
        assert reply.score is not None
    if reply.type == "decision":
        assert reply.invests is not None
    # la respuesta quedó en el pool compartido con su versión mínima
    assert session.context[-1].summary == reply.summary
    return reply


if __name__ == "__main__":
    print(test_shark_resolves_pitch_end_to_end().model_dump_json(indent=2))
