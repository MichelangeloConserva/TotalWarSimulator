import pymunk
import pygame
import numpy as np

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d

from utils.pymunk_utils import limit_velocity
from utils.pymunk_utils import kill_lateral_velocity


class Person:
  def __init__(self, game, pos, col, coll):
    """
  This is the base class that is used for every soldier, i.e. infantry
  cavalry or archers.
  """
    # Linking
    self.target_position = pos  # The location the soldier will go to
    self.col = col
    self.game = game

    # Physics
    self.body = self.add_body(pos)
    self.body.velocity_func = limit_velocity
    self.shape = self.add_shape(coll)
    self.sensor = self.add_sensors(coll)  # Melee sensor
    game.space.add(self.body, self.shape, self.sensor)

    # Variables
    self.enemy_melee_range = set()
    self.enemy_in_range = set()
    self.target_soldier = None  # TODO : To be used when fighting (?)

  def add_body(self, pos):
    body = pymunk.Body()
    body.position = pos
    body.soldier = self
    return body

  def move(
    self, speed, LATERAL_NOISE_MULTIPLIER,
  ):

    kill_lateral_velocity(self.body)
    f = speed * self.mass
    noise = np.random.randn() * self.mass * LATERAL_NOISE_MULTIPLIER
    self.body.apply_force_at_local_point(Vec2d(f, noise), Vec2d(0, 0))

  def detach_from_holder(self):
    if self.spring is None:
      return

    self.game.space.remove(self.spring)
    self.spring = None

  def dies(self):
    self.unit.soldiers.remove(self)
    for c in self.components:
      self.game.space.remove(c)

  # =============================================================================
  # The following functions define the kind of soldier
  # =============================================================================

  def add_shape(self, *args, **kwargs):
    raise NotImplementedError("add_shape")

  def add_sensors(self, *args, **kwargs):
    raise NotImplementedError("add_sensor")

  def update(self, *args, **kwargs):
    raise NotImplementedError("update")

  def draw(self, *args, **kwargs):
    raise NotImplementedError("draw")
