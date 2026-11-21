import logging

from fastapi import APIRouter, HTTPException

from app import session as store
from app.config import load_sharks
from app.schemas import ContextEntry, MessageRequest, PitchRequest, RoundOut, SessionOut
from app.services import build_agent, run_round
from app.session import Session

logger = logging.getLogger(__name__)
router = APIRouter()


def _get_or_404(session_id: str) -> Session:
    session = store.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Sesión no encontrada")
    return session


def _user_turn(session: Session, text: str) -> RoundOut:
    session.context.append(ContextEntry(author="Usuario", kind="usuario", content=text))
    try:
        replies = run_round(session)
    except Exception as e:
        logger.exception("Error en la ronda de tiburones")
        raise HTTPException(status_code=500, detail=f"Error en la ronda de tiburones: {e}")
    return RoundOut(session_id=session.id, replies=replies)


@router.get(
    "/",
    summary="Revisar que la API funciona",
)
def health():
    return {"status": "La API de Shark Tank está funcionando"}


@router.post(
    "/sessions",
    response_model=RoundOut,
    summary="Empezar una sesión con tu pitch",
)
def create_session(request: PitchRequest):
    sharks = load_sharks()
    session = store.add(Session(sharks=sharks, agents=[build_agent(s) for s in sharks]))
    return _user_turn(session, request.pitch)


@router.post(
    "/sessions/{session_id}/messages",
    response_model=RoundOut,
    summary="Responderle a los tiburones",
)
def send_message(session_id: str, request: MessageRequest):
    return _user_turn(_get_or_404(session_id), request.content)


@router.get(
    "/sessions/{session_id}",
    response_model=SessionOut,
    summary="Ver la conversación",
)
def get_session(session_id: str):
    s = _get_or_404(session_id)
    return SessionOut(session_id=s.id, sharks=[x.name for x in s.sharks], context=s.context)


@router.delete(
    "/sessions/{session_id}",
    summary="Terminar la sesión",
)
def delete_session(session_id: str):
    if not store.remove(session_id):
        raise HTTPException(status_code=404, detail="Sesión no encontrada")
    return {"status": "success", "message": "Sesión eliminada"}
