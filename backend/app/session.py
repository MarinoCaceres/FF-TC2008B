import asyncio
from dataclasses import dataclass, field
from typing import TYPE_CHECKING, Dict

from crewai import Agent

from app.config import SharkConfig

if TYPE_CHECKING:  # el Flow vive en services; solo se importa para tipos
    from app.services import SharkTankFlow


@dataclass
class ActiveShark:
    config: SharkConfig
    agent: Agent

    @property
    def name(self) -> str:
        return self.config.name


@dataclass
class Session:
    flow: "SharkTankFlow"
    lock: asyncio.Lock = field(default_factory=asyncio.Lock)  # una ronda a la vez por sesión

    @property
    def id(self) -> str:
        return self.flow.state.id


_sessions: Dict[str, Session] = {}


def add(session: Session) -> Session:
    _sessions[session.id] = session
    return session


def get(session_id: str) -> Session | None:
    return _sessions.get(session_id)


def remove(session_id: str) -> bool:
    return _sessions.pop(session_id, None) is not None
