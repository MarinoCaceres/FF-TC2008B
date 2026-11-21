import uuid
from dataclasses import dataclass, field
from typing import Dict, List

from crewai import Agent

from app.config import SharkConfig
from app.schemas import ContextEntry


@dataclass
class Session:
    sharks: List[SharkConfig]
    agents: List[Agent]
    id: str = field(default_factory=lambda: uuid.uuid4().hex)
    context: List[ContextEntry] = field(default_factory=list)  # common context pool


_sessions: Dict[str, Session] = {}


def add(session: Session) -> Session:
    _sessions[session.id] = session
    return session


def get(session_id: str) -> Session | None:
    return _sessions.get(session_id)


def remove(session_id: str) -> bool:
    return _sessions.pop(session_id, None) is not None
