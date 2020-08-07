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
