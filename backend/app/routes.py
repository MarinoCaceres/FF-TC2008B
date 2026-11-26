import logging

from fastapi import APIRouter, HTTPException

from app import session as store
from app.config import load_sharks
from app.schemas import (
    RoundOut,
    SessionCreate,
    SessionCreated,
    SessionMessages,
    SharkInfo,
    SharksOut,
    SummaryOutput,
)
from app.services import InvalidRequest, handle_messages, start_session, summarize_session

logger = logging.getLogger(__name__)
router = APIRouter()


@router.get("/", summary="Revisar que la API funciona")
def health():
    return {"status": "La API de Shark Tank está funcionando"}


@router.get("/sharks", response_model=SharksOut, summary="Ver los tiburones disponibles")
def list_sharks():
    return SharksOut(sharks=[SharkInfo(name=s.name, description=s.description) for s in load_sharks()])


@router.post("/session", response_model=SessionCreated, summary="Iniciar una sesión con los tiburones elegidos")
def create_session(request: SessionCreate):
    try:
        session = start_session(request.sharks)
    except InvalidRequest as e:
        raise HTTPException(status_code=422, detail=str(e))
    return SessionCreated(session_id=session.id)


@router.post("/session/{session_id}", response_model=RoundOut, summary="Mandar mensajes a los tiburones")
async def send_messages(session_id: str, request: SessionMessages):
    session = store.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Sesión no encontrada")
    try:
        return await handle_messages(session, request.messages)
    except InvalidRequest as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        logger.exception("Error en la ronda de tiburones")
        raise HTTPException(status_code=500, detail=f"Error en la ronda de tiburones: {e}")


@router.post(
    "/session/{session_id}/summary",
    response_model=SummaryOutput,
    summary="Retroalimentación del coach sobre el desempeño del usuario en la sesión",
)
async def session_summary(session_id: str):
    session = store.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Sesión no encontrada")
    try:
        return await summarize_session(session)
    except InvalidRequest as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        logger.exception("Error al generar el resumen")
        raise HTTPException(status_code=500, detail=f"Error al generar el resumen: {e}")


@router.delete("/session/{session_id}", summary="Terminar la sesión")
def delete_session(session_id: str):
    if not store.remove(session_id):
        raise HTTPException(status_code=404, detail="Sesión no encontrada")
    return {"status": "success", "message": "Sesión eliminada"}
