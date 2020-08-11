import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import numpy as np
import pygame

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame
from sklearn.metrics import pairwise_distances
from scipy.optimize import linear_sum_assignment

from b_unit import Unit
from inf_sold import Melee_Soldier
from utils.enums import Role, UnitType
from formations import SquareFormation
from concave_hull import concaveHull
from unit_movement_controller import MovementController

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
  aggressive_charge = True

  FORCE_MULTIPLIER = 10
  dumping_intra = 1
  damping_def = 0.5
  stifness_intra = FORCE_MULTIPLIER * 55 * soldier.mass
  stifness_def = FORCE_MULTIPLIER * 65 * soldier.mass
  LATERAL_NOISE_MULTIPLIER = FORCE_MULTIPLIER
  ATTACKING_MULTIPLIERS = 0.6
  melee_range = 50
  ratio = 4 # This means that we want the number of soldiers in a line be 4 time
            # the number of ranks

  @property
  def formation(self):
    if not hasattr(self, "_formation"):
      self._formation = SquareFormation(self, self.melee_range, self.ratio)
    return self._formation

  def __init__(self, game, pos, angle, col, coll):
    Unit.__init__(self, game, pos, angle, col, coll)
    self.movement_controller = MovementController(self, self.formation)

  def move_at_point(self, final_pos, final_angle=None, n_ranks=None, remove_tu=True):
    self.movement_controller.move_at_point(final_pos, final_angle=None, n_ranks=None, remove_tu=True)

  def update(self, dt):

    n_deads = 0
    for s in self.soldiers:
      s.update(dt)
      if not s.is_alive: n_deads += 1
      
    self.movement_controller.update(n_deads)
      

  def draw(self, DEBUG):
    for s in self.soldiers:
      s.draw()
    
    # DRAW CONVEX HULL
    points,hull = self.formation.get_melee_fighting_hull(in_pygame = True)
    
    for simplex in hull.simplices:
      p1 = points[simplex, 0].tolist()
      p2 = points[simplex, 1].tolist()
      pygame.draw.line(self.game.screen, (0,0,0), (p1[0],p2[0]), (p1[1],p2[1]))        
    