import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from app import session as store  # noqa: E402
from app.schemas import SharkSetup, UserMessage  # noqa: E402
from app.services import handle_messages, start_session  # noqa: E402

PITCH = (
    "Hola Tiburones, soy Alex. Mi startup EcoTrack es un sistema de gestión de energía "
    "para casas que usa IA para reducir el consumo. Tenemos 500 usuarios beta y $50,000 "
    "USD en preórdenes. Vendemos hardware más una suscripción mensual. Busco $300,000 USD "
    "por el 10% de la empresa para mejorar el producto, marketing y crecer el equipo."
)


def test_shark_resolves_pitch_end_to_end():
    session = start_session([SharkSetup(name="Master Shark", modifier1=3, modifier2=3)])
    try:
        responses = handle_messages(session, [UserMessage(recipient="all", content=PITCH)])

        assert len(responses) == 1
        r = responses[0]
        assert r.shark == "Master Shark"
        assert r.type in ("out", "question", "in")
        assert r.content.strip()
        assert -3 <= r.attitude <= 3
        # la respuesta quedó en el pool compartido con su versión mínima
        assert session.context[-1].summary and len(session.context[-1].summary) < len(r.content)
        return r
    finally:
        store.remove(session.id)


if __name__ == "__main__":
    print(test_shark_resolves_pitch_end_to_end().model_dump_json(indent=2))
