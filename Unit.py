import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame
from scipy.spatial import ConvexHull, convex_hull_plot_2d

from utils.pygame_utils import draw_text
from utils.enums import Role, UnitType, UnitState
from Formation import Formation
from Soldier import Soldier

class Unit:

  soldier = Soldier
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
  
  @property
  def soldier_size_dist(self): return self.soldier.radius * 2, self.soldier.dist
  @property
  def n(self):                 return len(self.soldiers)
  @property
  def pos(self): return sum(self.get_soldiers_pos()) / len(self.soldiers)
  @property
  def formation(self):
    if not hasattr(self, "_formation"):
      self._formation = Formation(self, self.melee_range, self.ratio)
    return self._formation

  def get_soldiers_pos(self, vec2d=True):
    typ = Vec2d if vec2d else list
    return [typ(s.body.position) for s in self.soldiers]

  def __init__(self, game, pos, angle, col, coll):

    self.col = col
    self.coll = coll
    self.game = game
    self._n = self.start_n
    self.n_ranks = self.start_ranks
    self.angle = angle
    self.final_pos = pos

    self.is_selected = False
    self.is_moving = False
    self.order = None
    self.before_order = None
    self.units_fighting_against = set()
    self.state = UnitState.STAND

    # Adding the soldiers physically
    self.add_soldiers()
    
    self.order, r_ind = self.formation.get_formation(
      pos, angle, self.n_ranks, self.n, *self.soldier_size_dist
    )
    self.formation.execute_formation(self.order, r_ind, set_physically=True)

  def add_soldiers(self):
    soldiers = []
    for _ in range(self.start_n):
      s = self.soldier(self.game, Vec2d(), self.col, self.coll)
      s.unit = self
      soldiers.append(s)
    self.soldiers = soldiers

  def update(self, dt):
    for s in self.soldiers:  s.update(dt)

  def draw(self, DEBUG):
    for s in self.soldiers: s.draw(DEBUG)

    if DEBUG:
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

    