import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import numpy as np

from pymunk.vec2d import Vec2d
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from .base_classes.bc_unit import Unit
from .infantry_soldier import Melee_Soldier
from .formations import SquareFormation
from utils.enums import Role, UnitType


class Melee_Unit(Unit):

  soldier = Melee_Soldier
  role = UnitType.INFANTRY
  start_n = 100
  start_ranks = 5
  dist = 5
  start_width = (start_n // start_ranks) * soldier.radius * 2 + (
    start_n // start_ranks - 1
  ) * soldier.dist
  rest_length_intra = soldier.radius * 2 + soldier.dist
  aggressive_charge = False

  FORCE_MULTIPLIER = 10
  dumping_intra = 1
  damping_def = 0.5
  stifness_intra = FORCE_MULTIPLIER * 55 * soldier.mass
  stifness_def = FORCE_MULTIPLIER * 65 * soldier.mass
  LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
  ATTACKING_MULTIPLIERS = 0.6
  TIME_TO_DEF = 1  # seconds

  @property
  def formation(self):
    if not hasattr(self, "_formation"):
      self._formation = SquareFormation(self)
    return self._formation

  # TODO : investigate what works better for fighting
  @property
  def is_fighting(self):
    for s in self.soldiers:
      if len(s.enemy_melee_range) > 0:
        self._is_fighting = True
        # if self.target_unit is None:
        #     self._target_unit = list(s.enemy_melee_range)[0].unit
        # self.target_unit = list(s.enemy_melee_range)[0].unit
        return True
    # if self._is_fighting and self.order is None:
    #     size, dist = self.soldier_size_dist
    #     pos = np.array(list(self.pos))
    #     self.order, r_ind = get_formation(pos, self.angle, self.n_ranks, self.n, size, dist)
    #     self.execute_formation(self.order, r_ind, set_physically = False)
    self._is_fighting = False
    return False

  def __init__(self, game, pos, angle, col, coll):
    Unit.__init__(self, game, pos, angle, col, coll)

  def attack(self, changed):

    if not changed and self.is_fighting:
      # Our frontline attack the nearest enemy border soldiers
      for s in self.ranks[0]:
        if s.target_soldier is None:
          continue
        s.target_position = s.target_soldier.body.position

      if self.aggressive_charge:
        ### AGGRESSIVE CHARGE ###
        # The rest of the soldiers follows
        for i in range(1, len(self.ranks)):
          for s in self.ranks[i]:
            s.target_position = s.front_nn.body.position
            s.target_soldier = s.front_nn
      else:
        ### DEFENSIVE CHARGE ###
        # The rest of the soldiers doesnt follow
        for i in list(range(1, len(self.ranks)))[::-1]:
          for s in self.ranks[i]:
            if s.target_soldier is None:
              continue

            ts = s.front_nn
            for _ in range(2):
              if ts.front_nn is None:
                break
              ts = ts.front_nn

            s.target_position = ts.body.position
            # s.target_position = s.body.position + s.body.world_to_local(Vec2d(1,0)))
            s.target_soldier = None
      return

    enemy_border_units = self.target_unit.soldiers
    enemy_pos = np.array([list(s.body.position) for s in enemy_border_units])
    frontline_pos = np.array([list(s.body.position) for s in self.ranks[0]])
    pd = pairwise_distances(frontline_pos, enemy_pos)
    row_ind, col_ind = linear_sum_assignment(pd)

    # Our frontline attack the nearest enemy border soldiers
    for ri, ci in zip(row_ind, col_ind):
      s_our = self.ranks[0][ri]
      s_enemy = enemy_border_units[ci]

      s_our.target_position = s_enemy.body.position
      s_our.target_soldier = s_enemy

    # The rest of the soldiers follows
    for i in range(1, len(self.ranks)):
      for j, s in enumerate(self.ranks[i]):
        s.target_position = s.front_nn.body.position
        s.target_soldier = s.front_nn
