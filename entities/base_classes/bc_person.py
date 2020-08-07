import pymunk
import pygame
import numpy as np

from pymunk.pygame_util import to_pygame
from pymunk.vec2d import Vec2d

from utils.pymunk_utils import limit_velocity
from utils.pymunk_utils import kill_lateral_velocity


class Person:
  @property
  def nns(self):
    return [self.front_nn, self.right_nn, self.bot_nn, self.left_nn]

  @property
  def is_at_border(self):
    for nn in self.nns:
      if nn is None:
        return True
    return False

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
    self.sensor = self.add_sensors(coll)  # Melee senso
    game.space.add(self.body, self.shape, self.sensor)

    # Variables
    self.enemy_melee_range = set()
    self.enemy_in_range = set()
    self.target_soldier = None  # TODO : To be used when fighting (?)

    self.reset_nn()

  def add_body(self, pos):
    body = pymunk.Body()
    body.position = pos
    body.soldier = self
    return body

  def reset_nn(self):
    """
  When in formation every soldier has neighbours. It is very important
  to link them.
  """
    self.front_nn = None
    self.bot_nn = None
    self.left_nn = None
    self.right_nn = None

  def add_formation_holder(self, coll):
    """
  Each individual has an holder attached. An holder is another body that
  is used by the formation.
  The individual is attached to the holder through a DampedSpring so that
  each soldier has constraint movement.
  """
    body = pymunk.Body()
    body.position = self.body.position
    shape = pymunk.Circle(body, 1)
    shape.density = 0.01
    shape.friction = 0.5
    shape.elasticity = 1
    shape.collision_type = 3  # Utility identifier
    self.holder = body
    self.game.space.add(body, shape)

  def attach_to_holder(self):
    """
  When formation changes the holder can be changed

  Returns
  -------
  None.

  """
    self.holder.position = self.body.position
    joint = pymunk.constraint.DampedSpring(
      self.holder, self.body, Vec2d(), Vec2d(), 0, self.stiffness, self.damping
    )
    joint.collide_bodies = False
    self.spring = joint
    self.game.space.add(joint)

  def move(
    self,
    speed,
    is_fighting,
    FORCE_MULTIPLIER,
    ATTACKING_MULTIPLIERS,
    LATERAL_NOISE_MULTIPLIER,
  ):
    # We change the velocity directly when intra_springs are not present
    # applying force without intra_springs is a mess
    if not self.unit.order is None:
      dv = Vec2d(speed, 0.0)
      self.body.velocity = self.body.rotation_vector.cpvrotate(dv)
      self.body.angular_velocity = 0.0
    else:
      kill_lateral_velocity(self.body)
      f = speed * self.mass * FORCE_MULTIPLIER
      if is_fighting:
        f = f * ATTACKING_MULTIPLIERS

      noise = np.random.randn() * self.mass * LATERAL_NOISE_MULTIPLIER
      self.body.apply_force_at_local_point(Vec2d(f, noise), Vec2d(0, 0))

  def detach_from_holder(self):
    self.spring = None
    self.gamespace.remove(self.spring)

  def dies(self):
    self.game.space.remove(self.body, self.shape, self.sensor, self.spring)

  def draw(self):
    pos = to_pygame(self.body.position, self.game.screen)
    pygame.draw.circle(self.game.screen, self.col, pos, self.radius)

# =============================================================================
# The following function are define the kind of soldier
# =============================================================================

  def add_shape(self, *args, **kwargs):
    raise NotImplementedError("add_shape")

  def add_sensors(self, *args, **kwargs):
    raise NotImplementedError("add_sensor")

  def update(self, *args, **kwargs):
    raise NotImplementedError("update")
