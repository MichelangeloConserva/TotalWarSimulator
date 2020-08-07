import sys, os  # Dirty trick to allow sibling imports

sys.path.insert(0, os.path.abspath(".."))

import pymunk
import pygame
import random

from pymunk.pygame_util import to_pygame

from .base_classes.bc_person import Person
from utils.colors import BLACK


class Melee_Soldier(Person):

  friction = 0.8
  elasticity = 0.01
  mass = 5

  # BIG VERSION
  # radius = 7
  # dist = 15

  # MEDIUM VERSION
  # radius = 5
  # dist = 3

  # SMALL VERSION
  radius = 2
  dist =   1

  melee_range = 1
  max_speed = 90
  base_speed = 50
  # max_speed   = 300
  # base_speed  = 350

  max_health = 50
  attack = 10
  defense = 3

  # Holder spring setting
  stiffness = 5 * 15 * mass
  damping = 1

  @property
  def is_alive(self):
    return self.health > 0

  def __init__(self, game, pos, col, coll):
    Person.__init__(self, game, pos, col, coll)

    self.add_formation_holder(coll)
    self.attach_to_holder()

    self.health = self.max_health
    self.size = 2 * self.radius + self.dist

  def add_shape(self, coll):
    shape = pymunk.Circle(self.body, self.radius)
    shape.color = self.col
    shape.friction = self.friction
    shape.elasticity = self.elasticity
    shape.mass = self.mass
    shape.collision_type = coll
    return shape

  def add_sensors(self, coll):
    """
  Adding a melee sensor.
  """
    sensor = pymunk.Circle(self.body, self.radius + self.melee_range)
    sensor.sensor = True
    sensor.collision_type = coll
    return sensor

  def update(self, dt):

    ### MELEE FIGHTING ###
    r = [random.random() for _ in range(len(self.enemy_melee_range))]
    for i, enemy in enumerate(self.enemy_melee_range):
      r_ = r[i]
      a = self.attack * r_
      d = enemy.defense * r_
      enemy.health -= max(a - d, 0) * dt

  def draw(self):
    pos = to_pygame(self.body.position, self.game.screen)
    pygame.draw.circle(self.game.screen, self.col, pos, self.radius)

    if self.unit.is_selected:
      pygame.draw.circle(self.game.screen, BLACK, pos, self.radius - 1)
