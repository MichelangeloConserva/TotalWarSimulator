import sys, os  # Dirty trick to allow sibling imports
sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from b_unit import Unit
from inf_sold import Melee_Soldier
from utils.enums import Role, UnitType
from formations import SquareFormation
from unit_controller import UnitController


class Melee_Unit(Unit):

  soldier = Melee_Soldier
  role = UnitType.INFANTRY
  start_n = 100
  start_ranks = 5
  dist = 5

  FORCE_MULTIPLIER = 10
  dumping_intra = 1
  damping_def = 0.5
  stifness_intra = FORCE_MULTIPLIER * 55 * soldier.mass
  stifness_def = FORCE_MULTIPLIER * 65 * soldier.mass
  LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
  ATTACKING_MULTIPLIERS = 0.6
  melee_range = soldier.radius * 2 + soldier.dist
  ratio = 4  # This means that we want the number of soldiers in a line be 4 time
  # the number of ranks

  @property
  def formation(self):
    if not hasattr(self, "_formation"):
      self._formation = SquareFormation(self, self.melee_range, self.ratio)
    return self._formation

  def __init__(self, game, pos, angle, col, coll):
    Unit.__init__(self, game, pos, angle, col, coll)
    self.controller = UnitController(self, self.formation)

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):
    self.controller.move_at_point(
      final_pos, final_angle=None, n_ranks=None, remove_tu=True
    )

  def update_info(self):
    self.formation.formation_info_update()


  def update(self, dt):
    n_deads = 0
    for s in self.soldiers:
      s.update(dt)
      if not s.is_alive:
        n_deads += 1
    self.controller.update(n_deads)

  def draw(self, DEBUG):
    for s in self.soldiers:
      s.draw()

    # DRAW CONVEX HULL
    inf_simplices, fight_simplices = self.formation.get_hulls(for_draw=True)    
    for p1, p2 in inf_simplices:
      pygame.draw.line(
        self.game.screen, (0, 0, 0), (p1[0], p2[0]), (p1[1], p2[1])
      )
    for p1, p2 in fight_simplices:
      pygame.draw.line(
        self.game.screen, (0, 0, 0), (p1[0], p2[0]), (p1[1], p2[1])
      )
