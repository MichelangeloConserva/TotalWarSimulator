import numpy as np
import pygame
import pymunk

from pymunk.vec2d import Vec2d
from pymunk.pygame_util import to_pygame, from_pygame
from scipy.spatial import ConvexHull, convex_hull_plot_2d

from utils.pygame_utils import draw_text

class Unit:
  @property
  def soldier_size_dist(self):
    return self.soldier.radius * 2, self.soldier.dist

  @property
  def pos(self):
    return sum(self.get_soldiers_pos()) / len(self.soldiers)

  @property
  def n(self):
    return len(self.soldiers)

  @property
  def border_units(self):
    return [s for s in self.soldiers if s.is_at_border]

  @property
  def target_unit(self):
    return self._target_unit

  @target_unit.setter
  def target_unit(self, tu):
    self._target_unit = tu
    if tu is None:
      return
    self.move_at_point(tu.pos, remove_tu=False)  # Facing the enemy unit

  @property
  def is_moving(self):
    return self._is_moving

  @is_moving.setter
  def is_moving(self, v):
    """
  We set a timer from the moment the unit starts moving as this may be useful.
  """
    if not v:
      self.start_mov_time = pygame.time.get_ticks()
    self._is_moving = v

  def calc_width(self, upr):
    return upr * self.soldier.radius * 2 + (upr - 1) * self.soldier.dist

  def get_soldiers_pos(self, vec2d = True):
    typ = Vec2d if vec2d else list
    return [typ(s.body.position) for s in self.soldiers]
  
  def __init__(self, game, pos, angle, col, coll):

    self.col = col
    self.coll = coll
    self.game = game
    self._n = self.start_n
    self.n_ranks = self.start_ranks
    self.angle = angle    

    self.is_selected = False
    self._is_moving = False
    self._is_fighting = False
    self._target_unit = None
    self.order = None
    self.before_order = None

    # Adding the soldiers physically
    self.add_soldiers()

    # Instantiate them in the space
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
    