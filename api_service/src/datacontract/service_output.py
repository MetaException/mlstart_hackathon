from pydantic import BaseModel
from typing import List, Tuple, Dict


class ServiceOutput(BaseModel):
    objectid: int
    classname: str
    xtl: int
    xbr: int
    ytl: int
    ybr: int
