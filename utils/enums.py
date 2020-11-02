from enum import Enum


class Role(Enum):
  ATTACKER = 1
  DEFENDER = 2


class UnitType(Enum):
  INFANTRY = 1
  ARCHERS = 2
  CAVALRY = 3


class Coll(Enum):
  UTILS = 3


class UnitState(Enum):
  STAND = 1
  MOVE = 2
  FIGHT = 3









